import { useCallback, useEffect, useState } from 'react';
import { View, StyleSheet, FlatList, Alert, Pressable } from 'react-native';
import { ActivityIndicator, Button, Icon, Text } from 'react-native-paper';
import * as Sharing from 'expo-sharing';
import { useLocalSearchParams } from 'expo-router';
import { getTransactionsForCard, getVisibleTransactions } from '@/api/endpoints/transactions';
import { downloadStatementPdf } from '@/api/endpoints/reconciliation';
import { useAuth } from '@/hooks/useAuth';
import { useCardsStore } from '@/store/cardsStore';
import { theme, spacing, radius } from '@/constants/theme';
import { formatCurrency } from '@/utils/currency';
import { formatDisplayDate, toIsoDate } from '@/utils/date';
import type { PeriodTransaction } from '@/types/transaction';
import type { Currency } from '@/types/enums';

const MONTH_LABELS = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];

export default function CardDetailScreen() {
  const { cardId } = useLocalSearchParams<{ cardId: string }>();
  const { user } = useAuth();
  const { cards, fetchCards } = useCardsStore();

  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const [transactions, setTransactions] = useState<PeriodTransaction[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDownloadingPdf, setIsDownloadingPdf] = useState(false);

  const card = cards.find((item) => item.id === cardId);

  useEffect(() => {
    if (user && cards.length === 0) {
      fetchCards(user.householdId);
    }
  }, [user, cards.length, fetchCards]);

  const loadTransactions = useCallback(async () => {
    if (!user || !card) {
      return;
    }

    setIsLoading(true);
    setError(null);

    // Débito nunca tiene StatementPeriod (el backend lo salta a propósito) — el endpoint
    // por periodo devuelve 404 siempre para estas tarjetas. Único fallback real: traer
    // todas las transacciones visibles del household y filtrar por cardId acá. Débito
    // tampoco puede tener cuotas (el backend las rechaza al registrar), así que
    // periodAmount == amount siempre y installmentNumber/totalInstallments son null.
    if (card.type === 'Debit') {
      const result = await getVisibleTransactions(user.householdId);

      if (!result.ok) {
        setError(result.error.message);
        setIsLoading(false);
        return;
      }

      const normalized: PeriodTransaction[] = result.data
        .filter((transaction) => transaction.cardId === cardId)
        .map((transaction) => ({
          ...transaction,
          periodAmount: transaction.amount,
          installmentNumber: null,
          totalInstallments: null,
        }));

      setTransactions(normalized);
      setIsLoading(false);
      return;
    }

    const result = await getTransactionsForCard(cardId, toIsoDate(selectedDate));

    if (!result.ok) {
      if (result.error.status === 404) {
        // 404 esperado: todavía no hay compras registradas en ese periodo (el Card ya
        // se validó al cargarlo desde cardsStore, así que no es un cardId inválido).
        setTransactions([]);
        setIsLoading(false);
        return;
      }

      setError(result.error.message);
      setIsLoading(false);
      return;
    }

    setTransactions(result.data);
    setIsLoading(false);
  }, [user, card, cardId, selectedDate]);

  useEffect(() => {
    loadTransactions();
  }, [loadTransactions]);

  // periodAmount, no amount — para una compra en cuotas amount es el total original de
  // la compra completa, no lo que corresponde a este periodo. Usar amount acá fue
  // exactamente el bug que se corrigió en el backend; no reintroducirlo acá.
  const totalsByCurrency = transactions.reduce<Record<string, number>>((totals, transaction) => {
    totals[transaction.currency] = (totals[transaction.currency] ?? 0) + transaction.periodAmount;
    return totals;
  }, {});

  function goToPreviousPeriod() {
    setSelectedDate((current) => {
      const next = new Date(current);
      next.setMonth(next.getMonth() - 1);
      return next;
    });
  }

  function goToNextPeriod() {
    setSelectedDate((current) => {
      const next = new Date(current);
      next.setMonth(next.getMonth() + 1);
      return next;
    });
  }

  async function handleSharePdf() {
    setIsDownloadingPdf(true);

    const result = await downloadStatementPdf(cardId, toIsoDate(selectedDate));

    setIsDownloadingPdf(false);

    if (!result.ok) {
      Alert.alert('No se pudo descargar el PDF', result.error.message);
      return;
    }

    const canShare = await Sharing.isAvailableAsync();

    if (!canShare) {
      Alert.alert('PDF descargado', `Guardado en: ${result.data}`);
      return;
    }

    await Sharing.shareAsync(result.data, { mimeType: 'application/pdf' });
  }

  if (!card || isLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator />
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.center}>
        <Text style={styles.error}>{error}</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text variant="titleMedium" style={styles.cardName}>
        {card.name}
      </Text>
      <Text variant="bodySmall" style={styles.cardBank}>
        {card.bank} · {card.type === 'Credit' ? 'Crédito' : 'Débito'}
      </Text>

      {card.type === 'Credit' ? (
        <View style={styles.periodNav}>
          <Pressable onPress={goToPreviousPeriod} style={styles.periodNavButton}>
            <Icon source="chevron-left" size={22} color={theme.colors.onSurface} />
          </Pressable>
          <Text variant="titleSmall" style={styles.periodLabel}>
            {MONTH_LABELS[selectedDate.getMonth()]} {selectedDate.getFullYear()}
          </Text>
          <Pressable onPress={goToNextPeriod} style={styles.periodNavButton}>
            <Icon source="chevron-right" size={22} color={theme.colors.onSurface} />
          </Pressable>
        </View>
      ) : null}

      <View style={styles.totalsRow}>
        {Object.entries(totalsByCurrency).map(([currency, total]) => (
          <View key={currency} style={styles.totalChip}>
            <Text variant="labelSmall" style={styles.totalLabel}>
              Total {currency}
            </Text>
            <Text variant="titleMedium" style={styles.totalValue}>
              {formatCurrency(total, currency as Currency)}
            </Text>
          </View>
        ))}
      </View>

      {card.type === 'Credit' ? (
        <Button
          mode="outlined"
          icon="file-pdf-box"
          onPress={handleSharePdf}
          loading={isDownloadingPdf}
          disabled={isDownloadingPdf}
          style={styles.pdfButton}
        >
          Compartir PDF de conciliación
        </Button>
      ) : null}

      <FlatList
        data={transactions}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        renderItem={({ item }) => (
          <View style={styles.row}>
            <View style={styles.rowBody}>
              <Text variant="titleSmall" style={styles.rowTitle}>
                {item.merchant}
              </Text>
              <Text variant="bodySmall" style={styles.rowSubtitle}>
                {formatDisplayDate(item.purchaseDate)}
                {item.installmentNumber !== null && item.totalInstallments !== null
                  ? ` · Cuota ${item.installmentNumber}/${item.totalInstallments}`
                  : ''}
              </Text>
            </View>
            <Text variant="titleSmall" style={styles.rowAmount}>
              {formatCurrency(item.periodAmount, item.currency)}
            </Text>
          </View>
        )}
        ListEmptyComponent={
          <Text style={styles.emptyText}>
            {card.type === 'Credit'
              ? 'No hay transacciones en este periodo.'
              : 'No hay transacciones en esta tarjeta todavía.'}
          </Text>
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.colors.background, padding: spacing.md },
  center: { flex: 1, backgroundColor: theme.colors.background, justifyContent: 'center', alignItems: 'center' },
  error: { color: theme.colors.error },
  cardName: { color: theme.colors.onSurface },
  cardBank: { color: theme.colors.onSurfaceVariant, marginTop: 2, marginBottom: spacing.md },
  periodNav: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
    marginBottom: spacing.md,
  },
  periodNavButton: {
    width: 36,
    height: 36,
    borderRadius: radius.pill,
    backgroundColor: theme.colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
  },
  periodLabel: { color: theme.colors.onSurface, minWidth: 140, textAlign: 'center' },
  totalsRow: { flexDirection: 'row', gap: spacing.sm, marginBottom: spacing.md },
  totalChip: {
    backgroundColor: theme.colors.surface,
    borderRadius: radius.md,
    padding: spacing.md,
    flex: 1,
  },
  totalLabel: { color: theme.colors.onSurfaceVariant },
  totalValue: { color: theme.colors.onSurface, marginTop: 2 },
  pdfButton: { borderRadius: radius.pill, marginBottom: spacing.md },
  listContent: { paddingBottom: spacing.xl, gap: spacing.sm },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: theme.colors.surface,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  rowBody: { flex: 1 },
  rowTitle: { color: theme.colors.onSurface },
  rowSubtitle: { color: theme.colors.onSurfaceVariant, marginTop: 2 },
  rowAmount: { color: theme.colors.onSurface },
  emptyText: { color: theme.colors.onSurfaceVariant, textAlign: 'center', marginTop: spacing.lg },
});
