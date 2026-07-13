import { useEffect, useMemo, useRef, useState } from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Button, Chip, HelperText, SegmentedButtons, Surface, Switch, Text, TextInput } from 'react-native-paper';
import { router } from 'expo-router';
import { Dropdown } from '@/components/Dropdown';
import { createTransaction } from '@/api/endpoints/transactions';
import { suggestSplitForMerchant } from '@/api/endpoints/splitRules';
import { useCardsStore } from '@/store/cardsStore';
import { useHouseholdStore } from '@/store/householdStore';
import { useAuth } from '@/hooks/useAuth';
import { theme, spacing, radius } from '@/constants/theme';
import { formatCurrency } from '@/utils/currency';
import type { Currency } from '@/types/enums';

// Opciones fijas de cuotas que ofrecen los bancos en Costa Rica — pedido explícito del
// usuario en vez de un TextInput libre (evita valores raros tipo "7 cuotas").
const INSTALLMENT_OPTIONS = [2, 3, 4, 6, 12, 18, 24];

export default function NewTransactionScreen() {
  const { user } = useAuth();
  const { cards, fetchCards } = useCardsStore();
  const { members, fetchMembers } = useHouseholdStore();

  const [cardId, setCardId] = useState('');
  const [merchant, setMerchant] = useState('');
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState<Currency>('CRC');
  const [isInstallments, setIsInstallments] = useState(false);
  const [totalInstallments, setTotalInstallments] = useState('');
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);
  const [percentages, setPercentages] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [suggestionApplied, setSuggestionApplied] = useState(false);

  // Cuando se aplica una sugerencia de SplitRule, el próximo cambio de selectedMemberIds
  // NO debe disparar el recálculo de split equitativo — si no, pisaría los porcentajes
  // sugeridos apenas se aplican (ambos setState corren en el mismo render, pero el efecto
  // de abajo igual se dispara por el cambio de referencia del array).
  const skipNextAutoSplitRef = useRef(false);

  useEffect(() => {
    if (user && cards.length === 0) {
      fetchCards(user.householdId);
    }
  }, [user, cards.length, fetchCards]);

  useEffect(() => {
    if (user && members.length === 0) {
      fetchMembers(user.householdId);
    }
  }, [user, members.length, fetchMembers]);

  useEffect(() => {
    if (!cardId && cards[0]) {
      setCardId(cards[0].id);
    }
  }, [cardId, cards]);

  // Por default, quien registra la compra queda seleccionado 100% para sí mismo.
  useEffect(() => {
    if (user && selectedMemberIds.length === 0) {
      setSelectedMemberIds([user.id]);
    }
  }, [user, selectedMemberIds.length]);

  useEffect(() => {
    if (skipNextAutoSplitRef.current) {
      skipNextAutoSplitRef.current = false;
      return;
    }

    if (selectedMemberIds.length === 0) {
      setPercentages({});
      return;
    }

    const base = Math.floor(100 / selectedMemberIds.length);
    const remainder = 100 - base * selectedMemberIds.length;

    const next: Record<string, string> = {};
    selectedMemberIds.forEach((id, index) => {
      next[id] = String(index === 0 ? base + remainder : base);
    });

    setPercentages(next);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedMemberIds]);

  const percentageSum = useMemo(
    () => selectedMemberIds.reduce((sum, id) => sum + (Number(percentages[id]) || 0), 0),
    [selectedMemberIds, percentages],
  );

  const isSplitValid = selectedMemberIds.length > 0 && percentageSum === 100;

  const numericAmount = Number(amount);
  const numericTotalInstallments = Number(totalInstallments);

  // El backend guarda esto como InstallmentPlan.InstallmentAmount y lo usa tal cual para
  // cada periodo (GetPeriodAmount) — si acá se manda el monto total en vez de dividirlo,
  // cada cuota le sale al usuario por el total completo. Bug real que pasó exactamente así.
  // Redondeado a 2 decimales para no arrastrar fracciones flotantes (ej. 10000/3).
  const installmentAmountPreview =
    isInstallments && numericAmount > 0 && numericTotalInstallments > 0
      ? Math.round((numericAmount / numericTotalInstallments) * 100) / 100
      : null;

  if (!user) {
    return null;
  }

  function toggleMember(memberId: string) {
    setSuggestionApplied(false);
    setSelectedMemberIds((prev) =>
      prev.includes(memberId) ? prev.filter((id) => id !== memberId) : [...prev, memberId],
    );
  }

  function setMemberPercentage(memberId: string, value: string) {
    setPercentages((prev) => ({ ...prev, [memberId]: value }));
  }

  async function handleMerchantBlur() {
    if (!merchant.trim() || !user) {
      return;
    }

    const result = await suggestSplitForMerchant(user.householdId, merchant.trim());

    if (!result.ok || !result.data || result.data.length === 0) {
      return;
    }

    skipNextAutoSplitRef.current = true;

    const nextPercentages: Record<string, string> = {};
    const nextSelectedIds = result.data.map((share) => {
      nextPercentages[share.personId] = String(share.percentage);
      return share.personId;
    });

    setSelectedMemberIds(nextSelectedIds);
    setPercentages(nextPercentages);
    setSuggestionApplied(true);
  }

  async function handleSubmit() {
    if (!user) {
      return;
    }

    setFormError(null);

    if (!cardId || !merchant || !numericAmount || numericAmount <= 0) {
      setFormError('Completá tarjeta, comercio y un monto válido.');
      return;
    }

    if (isInstallments && (!numericTotalInstallments || numericTotalInstallments <= 0)) {
      setFormError('Completá una cantidad de cuotas válida.');
      return;
    }

    if (!isSplitValid) {
      setFormError('Los porcentajes del split deben sumar exactamente 100.');
      return;
    }

    const split = selectedMemberIds.map((id) => ({
      personId: id,
      percentage: Number(percentages[id]),
    }));

    setIsSubmitting(true);

    const result = await createTransaction({
      cardId,
      merchant,
      purchaseDate: new Date().toISOString().slice(0, 10),
      amount: numericAmount,
      currency,
      installments: isInstallments
        ? {
            totalInstallments: numericTotalInstallments,
            // installmentAmountPreview ya está calculado y validado arriba (>0) cuando
            // isInstallments es true y llegamos hasta acá.
            installmentAmount: installmentAmountPreview as number,
          }
        : null,
      split,
    });

    setIsSubmitting(false);

    if (!result.ok) {
      setFormError(result.error.message);
      return;
    }

    router.back();
  }

  return (
    <ScrollView style={styles.flex} contentContainerStyle={styles.scrollContent}>
      <Surface style={styles.card} elevation={0}>
        <TextInput
          mode="outlined"
          label="Comercio"
          value={merchant}
          onChangeText={(value) => {
            setMerchant(value);
            setSuggestionApplied(false);
          }}
          onBlur={handleMerchantBlur}
          style={styles.input}
        />

        {suggestionApplied ? (
          <HelperText type="info" visible style={styles.suggestionNote}>
            Split sugerido aplicado a partir de una regla guardada.
          </HelperText>
        ) : null}

        <Dropdown
          label="Tarjeta"
          value={cardId}
          onSelect={setCardId}
          options={cards.map((card) => ({ label: card.name, value: card.id }))}
          style={styles.input}
        />

        <TextInput
          mode="outlined"
          label="Monto"
          keyboardType="decimal-pad"
          value={amount}
          onChangeText={setAmount}
          style={styles.input}
        />

        <SegmentedButtons
          value={currency}
          onValueChange={(value) => setCurrency(value as Currency)}
          buttons={[
            { value: 'CRC', label: 'Colones' },
            { value: 'USD', label: 'Dólares' },
          ]}
          style={styles.segmented}
        />

        <View style={styles.switchRow}>
          <Text variant="bodyMedium" style={styles.switchLabel}>
            ¿Pago en cuotas?
          </Text>
          <Switch value={isInstallments} onValueChange={setIsInstallments} />
        </View>

        {isInstallments ? (
          <>
            <Dropdown
              label="Cantidad de cuotas"
              value={totalInstallments}
              onSelect={setTotalInstallments}
              options={INSTALLMENT_OPTIONS.map((n) => ({ label: `${n} cuotas`, value: String(n) }))}
              style={styles.input}
            />

            {installmentAmountPreview !== null ? (
              <HelperText type="info" visible style={styles.installmentPreview}>
                {numericTotalInstallments} cuotas de {formatCurrency(installmentAmountPreview, currency)} cada una
              </HelperText>
            ) : null}
          </>
        ) : null}

        <Text variant="labelLarge" style={styles.label}>
          ¿Entre quiénes se divide?
        </Text>
        <View style={styles.chipRow}>
          {members.map((member) => (
            <Chip
              key={member.id}
              selected={selectedMemberIds.includes(member.id)}
              onPress={() => toggleMember(member.id)}
              style={styles.chip}
              showSelectedCheck
            >
              {member.name}
            </Chip>
          ))}
        </View>

        {selectedMemberIds.length > 1
          ? selectedMemberIds.map((id) => {
              const member = members.find((m) => m.id === id);

              return (
                <View key={id} style={styles.percentageRow}>
                  <Text variant="bodyMedium" style={styles.percentageLabel}>
                    {member?.name ?? id}
                  </Text>
                  <TextInput
                    mode="outlined"
                    dense
                    keyboardType="number-pad"
                    value={percentages[id] ?? ''}
                    onChangeText={(value) => setMemberPercentage(id, value)}
                    style={styles.percentageInput}
                    right={<TextInput.Affix text="%" />}
                  />
                </View>
              );
            })
          : null}

        {selectedMemberIds.length > 1 ? (
          <HelperText type={isSplitValid ? 'info' : 'error'} visible>
            Suma actual: {percentageSum}% {isSplitValid ? '' : '(debe ser 100%)'}
          </HelperText>
        ) : null}

        <HelperText type="error" visible={formError !== null}>
          {formError ?? ''}
        </HelperText>

        <Button
          mode="contained"
          onPress={handleSubmit}
          loading={isSubmitting}
          disabled={isSubmitting || !isSplitValid}
          style={styles.submitButton}
          contentStyle={styles.submitButtonContent}
        >
          {isSubmitting ? 'Guardando...' : 'Guardar'}
        </Button>
      </Surface>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1, backgroundColor: theme.colors.background },
  scrollContent: { padding: spacing.md, paddingBottom: spacing.xl },
  card: { borderRadius: radius.lg, padding: spacing.md, backgroundColor: theme.colors.surface },
  input: { marginBottom: spacing.md },
  suggestionNote: { marginTop: -spacing.sm },
  installmentPreview: { marginTop: -spacing.sm, marginBottom: spacing.sm },
  label: { color: theme.colors.onSurfaceVariant, marginBottom: spacing.sm },
  segmented: { marginBottom: spacing.md },
  switchRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.md,
  },
  switchLabel: { color: theme.colors.onSurface },
  chipRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm, marginBottom: spacing.sm },
  chip: { marginBottom: spacing.xs },
  percentageRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: spacing.sm,
    gap: spacing.md,
  },
  percentageLabel: { color: theme.colors.onSurface, flex: 1 },
  percentageInput: { width: 100 },
  submitButton: { borderRadius: radius.pill, marginTop: spacing.sm },
  submitButtonContent: { paddingVertical: 6 },
});
