import { useEffect, useMemo, useState } from 'react';
import { View, StyleSheet, FlatList, Pressable, ScrollView } from 'react-native';
import { ActivityIndicator, Button, Icon, Surface, Text, TextInput } from 'react-native-paper';
import { router } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '@/hooks/useAuth';
import { useAlertsStore } from '@/store/alertsStore';
import { useCardsStore } from '@/store/cardsStore';
import { useDashboardStore } from '@/store/dashboardStore';
import { theme, spacing, radius, alertColors } from '@/constants/theme';
import { formatCurrency } from '@/utils/currency';
import { APP_BAR_HEIGHT } from '@/components/TopAppBar';
import type { CardAlert } from '@/types/alerts';

function describeDaysUntil(daysUntil: number): string {
  if (daysUntil === 0) {
    return 'hoy';
  }

  if (daysUntil === 1) {
    return 'mañana';
  }

  return `en ${daysUntil} días`;
}

function describeCardAlert(alert: CardAlert): string {
  const when = describeDaysUntil(alert.daysUntil);
  return alert.alertType === 'CutoffSoon' ? `${alert.cardName}: corte ${when}` : `${alert.cardName}: pago ${when}`;
}

export default function HomeScreen() {
  const insets = useSafeAreaInsets();
  const { user } = useAuth();
  const { cards, fetchCards } = useCardsStore();
  const { dashboard, isLoading, error, fetchDashboard } = useDashboardStore();
  const { alerts, fetchAlerts } = useAlertsStore();
  const [exchangeRateInput, setExchangeRateInput] = useState('');

  useEffect(() => {
    if (user) {
      fetchCards(user.householdId);
      fetchDashboard(user.householdId);
      fetchAlerts(user.householdId);
    }
  }, [user, fetchCards, fetchDashboard, fetchAlerts]);

  const exchangeRate = Number(exchangeRateInput.replace(',', '.'));
  const hasValidRate = exchangeRateInput.trim() !== '' && exchangeRate > 0;

  const combinedCrc = useMemo(() => {
    if (!dashboard || !hasValidRate) {
      return null;
    }

    return dashboard.grandTotalCrc + dashboard.grandTotalUsd * exchangeRate;
  }, [dashboard, hasValidRate, exchangeRate]);

  const cardTotalsByCardId = useMemo(() => {
    const map: Record<string, { totalCrc: number; totalUsd: number }> = {};
    dashboard?.byCard.forEach((cardTotal) => {
      map[cardTotal.cardId] = { totalCrc: cardTotal.totalCrc, totalUsd: cardTotal.totalUsd };
    });
    return map;
  }, [dashboard]);

  const hasAlerts = Boolean(alerts && (alerts.cardAlerts.length > 0 || alerts.installmentAlerts.length > 0));

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={[styles.scrollContent, { paddingTop: insets.top + APP_BAR_HEIGHT + spacing.md }]}
    >
      <Surface style={styles.banner} elevation={0}>
        <Text variant="titleMedium" style={styles.bannerTitle}>
          Registrar una compra
        </Text>
        <Text variant="bodySmall" style={styles.bannerSubtitle}>
          Anotala al salir del comercio, antes de que se te olvide.
        </Text>
        <Button
          mode="contained"
          style={styles.bannerButton}
          onPress={() => router.push('/(app)/transactions/new')}
        >
          Nueva compra
        </Button>
      </Surface>

      {hasAlerts && alerts ? (
        <View style={styles.alertsSection}>
          {alerts.cardAlerts.map((alert, index) => (
            <View key={`${alert.cardId}-${alert.alertType}-${index}`} style={styles.alertRow}>
              <Icon source="alert-circle-outline" size={24} color={alertColors.warning} />
              <Text variant="bodyLarge" style={styles.alertText}>
                {describeCardAlert(alert)}
              </Text>
            </View>
          ))}

          {alerts.installmentAlerts.map((alert) => (
            <View key={alert.transactionId} style={styles.alertRow}>
              <Icon source="calendar-clock-outline" size={24} color={alertColors.warning} />
              <Text variant="bodyLarge" style={styles.alertText}>
                {alert.merchant} — Cuota {alert.installmentNumber}/{alert.totalInstallments} —{' '}
                {formatCurrency(alert.periodAmount, alert.currency)} — vence {describeDaysUntil(alert.daysUntilPaymentDue)}
              </Text>
            </View>
          ))}
        </View>
      ) : null}

      <Text variant="titleMedium" style={styles.sectionTitle}>
        Total del periodo actual
      </Text>

      {isLoading ? (
        <ActivityIndicator style={styles.loader} />
      ) : error ? (
        <Text style={styles.error}>{error}</Text>
      ) : dashboard ? (
        <>
          <View style={styles.grandTotalsRow}>
            <View style={styles.grandTotalChip}>
              <Text variant="labelSmall" style={styles.grandTotalLabel}>
                Colones
              </Text>
              <Text variant="headlineSmall" style={styles.grandTotalValue}>
                {formatCurrency(dashboard.grandTotalCrc, 'CRC')}
              </Text>
            </View>
            <View style={styles.grandTotalChip}>
              <Text variant="labelSmall" style={styles.grandTotalLabel}>
                Dólares
              </Text>
              <Text variant="headlineSmall" style={styles.grandTotalValue}>
                {formatCurrency(dashboard.grandTotalUsd, 'USD')}
              </Text>
            </View>
          </View>

          <View style={styles.exchangeRateRow}>
            <TextInput
              mode="outlined"
              dense
              label="Tipo de cambio (₡ por $)"
              keyboardType="decimal-pad"
              value={exchangeRateInput}
              onChangeText={setExchangeRateInput}
              style={styles.exchangeRateInput}
            />
            {combinedCrc !== null ? (
              <View style={styles.combinedTotalBox}>
                <Text variant="labelSmall" style={styles.combinedTotalLabel}>
                  Combinado (referencial)
                </Text>
                <Text variant="titleMedium" style={styles.combinedTotalValue}>
                  {formatCurrency(combinedCrc, 'CRC')}
                </Text>
              </View>
            ) : null}
          </View>

          {dashboard.byPerson.length > 0 ? (
            <View style={styles.personList}>
              {dashboard.byPerson.map((person) => (
                <View key={person.personId} style={styles.personRow}>
                  <Text variant="titleSmall" style={styles.personName}>
                    {person.personName}
                  </Text>
                  <Text variant="bodyMedium" style={styles.personAmounts}>
                    {formatCurrency(person.totalCrc, 'CRC')}
                    {person.totalUsd > 0 ? ` · ${formatCurrency(person.totalUsd, 'USD')}` : ''}
                  </Text>
                </View>
              ))}
            </View>
          ) : null}
        </>
      ) : null}

      <Text variant="titleMedium" style={styles.sectionTitle}>
        Tus tarjetas
      </Text>

      <FlatList
        data={cards}
        keyExtractor={(item) => item.id}
        numColumns={2}
        scrollEnabled={false}
        columnWrapperStyle={styles.gridRow}
        contentContainerStyle={styles.gridContent}
        renderItem={({ item }) => {
          const cardTotal = cardTotalsByCardId[item.id];

          return (
            <Pressable style={styles.cardTile} onPress={() => router.push(`/(app)/cards/${item.id}`)}>
              <Icon
                source={item.type === 'Credit' ? 'credit-card-outline' : 'cash'}
                size={22}
                color={theme.colors.primary}
              />
              <Text variant="titleSmall" style={styles.cardTileTitle} numberOfLines={1}>
                {item.name}
              </Text>
              {item.type === 'Credit' && cardTotal ? (
                <Text variant="bodySmall" style={styles.cardTileSubtitle} numberOfLines={1}>
                  {formatCurrency(cardTotal.totalCrc, 'CRC')}
                  {cardTotal.totalUsd > 0 ? ` · ${formatCurrency(cardTotal.totalUsd, 'USD')}` : ''}
                </Text>
              ) : (
                <Text variant="bodySmall" style={styles.cardTileSubtitle} numberOfLines={1}>
                  {item.bank}
                </Text>
              )}
            </Pressable>
          );
        }}
        ListEmptyComponent={<Text style={styles.emptyText}>No hay tarjetas registradas todavía.</Text>}
      />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.colors.background },
  scrollContent: { padding: spacing.md, paddingBottom: 120 },
  banner: {
    borderRadius: radius.lg,
    padding: spacing.md,
    backgroundColor: theme.colors.primaryContainer,
    marginBottom: spacing.lg,
  },
  bannerTitle: { color: theme.colors.onPrimaryContainer, fontWeight: '700' },
  bannerSubtitle: { color: theme.colors.onPrimaryContainer, opacity: 0.8, marginTop: 4, marginBottom: spacing.md },
  bannerButton: { alignSelf: 'flex-start', borderRadius: radius.pill },
  alertsSection: {
    gap: spacing.sm,
    marginBottom: spacing.lg,
  },
  alertRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    backgroundColor: alertColors.warningContainer,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  alertText: { color: alertColors.onWarningContainer, flex: 1, fontWeight: '500' },
  sectionTitle: { color: theme.colors.onBackground, fontWeight: '600', marginBottom: spacing.sm, marginTop: spacing.md },
  loader: { marginTop: spacing.md },
  error: { color: theme.colors.error },
  grandTotalsRow: { flexDirection: 'row', gap: spacing.sm },
  grandTotalChip: {
    flex: 1,
    backgroundColor: theme.colors.surface,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  grandTotalLabel: { color: theme.colors.onSurfaceVariant },
  grandTotalValue: { color: theme.colors.onSurface, marginTop: 2 },
  exchangeRateRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, marginTop: spacing.sm },
  exchangeRateInput: { flex: 1 },
  combinedTotalBox: {
    backgroundColor: theme.colors.secondaryContainer,
    borderRadius: radius.md,
    padding: spacing.sm,
    alignItems: 'flex-end',
  },
  combinedTotalLabel: { color: theme.colors.onSecondaryContainer },
  combinedTotalValue: { color: theme.colors.onSecondaryContainer },
  personList: { marginTop: spacing.md, gap: spacing.sm },
  personRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: theme.colors.surface,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  personName: { color: theme.colors.onSurface },
  personAmounts: { color: theme.colors.onSurfaceVariant },
  gridContent: { paddingBottom: spacing.sm },
  gridRow: { gap: spacing.md, marginBottom: spacing.md },
  cardTile: {
    flex: 1,
    backgroundColor: theme.colors.surface,
    borderRadius: radius.md,
    padding: spacing.md,
    minHeight: 100,
    justifyContent: 'space-between',
  },
  cardTileTitle: { color: theme.colors.onSurface, marginTop: spacing.sm },
  cardTileSubtitle: { color: theme.colors.onSurfaceVariant },
  emptyText: { color: theme.colors.onSurfaceVariant, textAlign: 'center', marginTop: spacing.lg },
});
