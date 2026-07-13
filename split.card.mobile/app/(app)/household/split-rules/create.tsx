import { useEffect, useMemo, useState } from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Button, Chip, HelperText, Surface, Text, TextInput } from 'react-native-paper';
import { router } from 'expo-router';
import { useAuth } from '@/hooks/useAuth';
import { useHouseholdStore } from '@/store/householdStore';
import { useSplitRulesStore } from '@/store/splitRulesStore';
import { theme, spacing, radius } from '@/constants/theme';

export default function CreateSplitRuleScreen() {
  const { user } = useAuth();
  const { members, fetchMembers } = useHouseholdStore();
  const { isSubmitting, createRule } = useSplitRulesStore();

  const [descriptionPattern, setDescriptionPattern] = useState('');
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);
  const [percentages, setPercentages] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (user && members.length === 0) {
      fetchMembers(user.householdId);
    }
  }, [user, members.length, fetchMembers]);

  // Mismo patrón de split equitativo automático que transactions/new.tsx — duplicado a
  // propósito por ahora (dos usos, no justifica extraer un hook compartido todavía).
  useEffect(() => {
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

  if (!user) {
    return null;
  }

  function toggleMember(memberId: string) {
    setSelectedMemberIds((prev) =>
      prev.includes(memberId) ? prev.filter((id) => id !== memberId) : [...prev, memberId],
    );
  }

  async function handleSubmit() {
    if (!user) {
      return;
    }

    setFormError(null);

    if (!descriptionPattern) {
      setFormError('Completá el patrón de comercio (ej. "Netflix").');
      return;
    }

    if (!isSplitValid) {
      setFormError('Los porcentajes deben sumar exactamente 100.');
      return;
    }

    const success = await createRule({
      householdId: user.householdId,
      descriptionPattern,
      defaultSplit: selectedMemberIds.map((id) => ({ personId: id, percentage: Number(percentages[id]) })),
    });

    if (!success) {
      setFormError(useSplitRulesStore.getState().error ?? 'No se pudo crear la regla.');
      return;
    }

    router.back();
  }

  return (
    <ScrollView style={styles.flex} contentContainerStyle={styles.scrollContent}>
      <Surface style={styles.card} elevation={0}>
        <TextInput
          mode="outlined"
          label="Comercio (ej. Netflix)"
          value={descriptionPattern}
          onChangeText={setDescriptionPattern}
          style={styles.input}
        />

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
                    onChangeText={(value) => setPercentages((prev) => ({ ...prev, [id]: value }))}
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
          {isSubmitting ? 'Guardando...' : 'Guardar regla'}
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
