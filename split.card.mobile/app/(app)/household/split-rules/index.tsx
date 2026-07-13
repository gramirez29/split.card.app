import { useEffect } from 'react';
import { View, StyleSheet, FlatList } from 'react-native';
import { ActivityIndicator, FAB, Text } from 'react-native-paper';
import { router } from 'expo-router';
import { useAuth } from '@/hooks/useAuth';
import { useHouseholdStore } from '@/store/householdStore';
import { useSplitRulesStore } from '@/store/splitRulesStore';
import { theme, spacing, radius } from '@/constants/theme';

export default function SplitRulesScreen() {
  const { user } = useAuth();
  const { members, fetchMembers } = useHouseholdStore();
  const { rules, isLoading, error, fetchRules } = useSplitRulesStore();

  useEffect(() => {
    if (user) {
      fetchRules(user.householdId);
    }
  }, [user, fetchRules]);

  useEffect(() => {
    if (user && members.length === 0) {
      fetchMembers(user.householdId);
    }
  }, [user, members.length, fetchMembers]);

  function nameFor(personId: string): string {
    return members.find((member) => member.id === personId)?.name ?? personId;
  }

  return (
    <View style={styles.container}>
      <Text variant="headlineSmall" style={styles.title}>
        Reglas de split
      </Text>
      <Text variant="bodySmall" style={styles.subtitle}>
        Cuando el comercio de una compra nueva matchea una de estas reglas, se sugiere este
        split automáticamente.
      </Text>

      {isLoading ? (
        <ActivityIndicator style={styles.loader} />
      ) : error ? (
        <Text style={styles.error}>{error}</Text>
      ) : (
        <FlatList
          data={rules}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          renderItem={({ item }) => (
            <View style={styles.row}>
              <Text variant="titleSmall" style={styles.rowTitle}>
                {item.descriptionPattern}
              </Text>
              <Text variant="bodySmall" style={styles.rowSubtitle}>
                {item.defaultSplit.map((share) => `${nameFor(share.personId)}: ${share.percentage}%`).join(' · ')}
              </Text>
            </View>
          )}
          ListEmptyComponent={<Text style={styles.emptyText}>No hay reglas todavía.</Text>}
        />
      )}

      {user?.role === 'Owner' ? (
        <FAB
          icon="plus"
          style={styles.fab}
          color={theme.colors.onPrimary}
          onPress={() => router.push('/(app)/household/split-rules/create')}
        />
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.colors.background, padding: spacing.md, paddingTop: spacing.xl },
  title: { color: theme.colors.onBackground, fontWeight: '700' },
  subtitle: { color: theme.colors.onSurfaceVariant, marginTop: spacing.xs, marginBottom: spacing.md },
  loader: { marginTop: spacing.lg },
  error: { color: theme.colors.error },
  listContent: { paddingBottom: 100, gap: spacing.sm },
  row: {
    backgroundColor: theme.colors.surface,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  rowTitle: { color: theme.colors.onSurface },
  rowSubtitle: { color: theme.colors.onSurfaceVariant, marginTop: 4 },
  emptyText: { color: theme.colors.onSurfaceVariant, textAlign: 'center', marginTop: spacing.lg },
  fab: {
    position: 'absolute',
    right: spacing.md,
    bottom: 24,
    backgroundColor: theme.colors.primary,
  },
});
