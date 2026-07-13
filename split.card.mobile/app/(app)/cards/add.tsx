import { useState } from 'react';
import { StyleSheet, ScrollView } from 'react-native';
import { Button, HelperText, SegmentedButtons, Surface, Text, TextInput } from 'react-native-paper';
import { router } from 'expo-router';
import { Dropdown } from '@/components/Dropdown';
import { useAuth } from '@/hooks/useAuth';
import { useCardsStore } from '@/store/cardsStore';
import { theme, spacing, radius } from '@/constants/theme';
import type { CardType } from '@/types/enums';

// 1-31, para elegir el día de corte/pago sin tener que digitarlo (y sin poder meter un
// número inválido como 35).
const DAY_OPTIONS = Array.from({ length: 31 }, (_, index) => index + 1);
const DAY_DROPDOWN_OPTIONS = DAY_OPTIONS.map((day) => ({ label: String(day), value: String(day) }));

export default function AddCardScreen() {
  const { user } = useAuth();
  const { isSubmitting, createCard } = useCardsStore();

  const [name, setName] = useState('');
  const [bank, setBank] = useState('');
  const [type, setType] = useState<CardType>('Credit');
  const [cutoffDay, setCutoffDay] = useState('');
  const [paymentDueDay, setPaymentDueDay] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  if (!user) {
    return null;
  }

  async function handleSubmit() {
    if (!user) {
      return;
    }

    setFormError(null);

    if (!name || !bank) {
      setFormError('Completá el nombre y el banco.');
      return;
    }

    if (type === 'Credit' && (!cutoffDay || !paymentDueDay)) {
      setFormError('Las tarjetas de crédito necesitan día de corte y día de pago.');
      return;
    }

    const success = await createCard({
      householdId: user.householdId,
      name,
      bank,
      type,
      cutoffDay: type === 'Credit' ? Number(cutoffDay) : null,
      paymentDueDay: type === 'Credit' ? Number(paymentDueDay) : null,
    });

    if (!success) {
      setFormError(useCardsStore.getState().error ?? 'No se pudo crear la tarjeta.');
      return;
    }

    router.back();
  }

  return (
    <ScrollView style={styles.flex} contentContainerStyle={styles.scrollContent}>
      <Surface style={styles.card} elevation={0}>
        <TextInput mode="outlined" label="Nombre" value={name} onChangeText={setName} style={styles.input} />

        <TextInput mode="outlined" label="Banco" value={bank} onChangeText={setBank} style={styles.input} />

        <Text variant="labelLarge" style={styles.label}>
          Tipo
        </Text>
        <SegmentedButtons
          value={type}
          onValueChange={(value) => setType(value as CardType)}
          buttons={[
            { value: 'Credit', label: 'Crédito' },
            { value: 'Debit', label: 'Débito' },
          ]}
          style={styles.segmented}
        />

        {type === 'Credit' ? (
          <>
            <Dropdown
              label="Día de corte"
              value={cutoffDay}
              onSelect={setCutoffDay}
              options={DAY_DROPDOWN_OPTIONS}
              style={styles.input}
            />
            <Dropdown
              label="Día de pago"
              value={paymentDueDay}
              onSelect={setPaymentDueDay}
              options={DAY_DROPDOWN_OPTIONS}
              style={styles.input}
            />
          </>
        ) : null}

        <HelperText type="error" visible={formError !== null}>
          {formError ?? ''}
        </HelperText>

        <Button
          mode="contained"
          onPress={handleSubmit}
          loading={isSubmitting}
          disabled={isSubmitting}
          style={styles.submitButton}
          contentStyle={styles.submitButtonContent}
        >
          {isSubmitting ? 'Guardando...' : 'Guardar tarjeta'}
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
  label: { color: theme.colors.onSurfaceVariant, marginBottom: spacing.sm },
  segmented: { marginBottom: spacing.md },
  submitButton: { borderRadius: radius.pill, marginTop: spacing.sm },
  submitButtonContent: { paddingVertical: 6 },
});
