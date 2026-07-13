import { useEffect } from 'react';
import { View, StyleSheet, FlatList, Pressable } from 'react-native';
import { ActivityIndicator, FAB, Icon, Text } from 'react-native-paper';
import { router } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '@/hooks/useAuth';
import { useHouseholdStore } from '@/store/householdStore';
import { theme, spacing, radius } from '@/constants/theme';
import { APP_BAR_HEIGHT } from '@/components/TopAppBar';

export default function HouseholdScreen() {
  const insets = useSafeAreaInsets();
  const { user } = useAuth();
  const { members, isLoading, error, fetchMembers } = useHouseholdStore();

  useEffect(() => {
    if (user) {
      fetchMembers(user.householdId);
    }
  }, [user, fetchMembers]);

  return (
    <View style={[styles.container, { paddingTop: insets.top + APP_BAR_HEIGHT + spacing.md }]}>
      <Text variant="headlineSmall" style={styles.title}>
        Household
      </Text>

      <Pressable style={styles.linkRow} onPress={() => router.push('/(app)/household/split-rules')}>
        <Icon source="table-split-cell" size={20} color={theme.colors.primary} />
        <Text variant="titleSmall" style={styles.linkText}>
          Reglas de split
        </Text>
        <Icon source="chevron-right" size={20} color={theme.colors.onSurfaceVariant} />
      </Pressable>

      <Text variant="titleMedium" style={styles.sectionTitle}>
        Miembros
      </Text>

      {isLoading ? (
        <ActivityIndicator style={styles.loader} />
      ) : error ? (
        <Text style={styles.error}>{error}</Text>
      ) : (
        <FlatList
          data={members}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          renderItem={({ item }) => (
            <View style={styles.row}>
              <View style={styles.avatar}>
                <Text style={styles.avatarText}>{item.name.charAt(0).toUpperCase()}</Text>
              </View>
              <View style={styles.rowBody}>
                <Text variant="titleSmall" style={styles.rowTitle}>
                  {item.name}
                </Text>
                <Text variant="bodySmall" style={styles.rowSubtitle}>
                  {item.email}
                </Text>
              </View>
              <View style={styles.roleBadge}>
                <Text variant="labelSmall" style={styles.roleBadgeText}>
                  {item.role}
                </Text>
              </View>
            </View>
          )}
          ListEmptyComponent={<Text style={styles.emptyText}>No hay miembros todavía.</Text>}
        />
      )}

      {user?.role === 'Owner' ? (
        <FAB
          icon="account-plus"
          style={styles.fab}
          color={theme.colors.onPrimary}
          onPress={() => router.push('/(app)/household/invite')}
        />
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.colors.background, padding: spacing.md },
  title: { color: theme.colors.onBackground, fontWeight: '700', marginBottom: spacing.md },
  linkRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    backgroundColor: theme.colors.surface,
    borderRadius: radius.md,
    padding: spacing.md,
    marginBottom: spacing.lg,
  },
  linkText: { color: theme.colors.onSurface, flex: 1 },
  sectionTitle: { color: theme.colors.onBackground, fontWeight: '600', marginBottom: spacing.sm },
  loader: { marginTop: spacing.lg },
  error: { color: theme.colors.error },
  listContent: { paddingBottom: 100, gap: spacing.sm },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: theme.colors.surface,
    borderRadius: radius.md,
    padding: spacing.md,
    gap: spacing.md,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: radius.pill,
    backgroundColor: theme.colors.secondaryContainer,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarText: { color: theme.colors.onSecondaryContainer, fontWeight: '700' },
  rowBody: { flex: 1 },
  rowTitle: { color: theme.colors.onSurface },
  rowSubtitle: { color: theme.colors.onSurfaceVariant, marginTop: 2 },
  roleBadge: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 4,
    borderRadius: radius.pill,
    backgroundColor: theme.colors.primaryContainer,
  },
  roleBadgeText: { color: theme.colors.onPrimaryContainer },
  emptyText: { color: theme.colors.onSurfaceVariant, textAlign: 'center', marginTop: spacing.lg },
  fab: {
    position: 'absolute',
    right: spacing.md,
    bottom: 96,
    backgroundColor: theme.colors.primary,
  },
});
