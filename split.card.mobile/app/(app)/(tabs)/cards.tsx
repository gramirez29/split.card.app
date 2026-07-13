import { useEffect } from 'react';
import { View, StyleSheet, FlatList, Pressable } from 'react-native';
import { ActivityIndicator, FAB, Icon, Text } from 'react-native-paper';
import { router } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useAuth } from '@/hooks/useAuth';
import { useCardsStore } from '@/store/cardsStore';
import { theme, spacing, radius } from '@/constants/theme';
import { APP_BAR_HEIGHT } from '@/components/TopAppBar';

export default function CardsScreen() {
  const insets = useSafeAreaInsets();
  const { user } = useAuth();
  const { cards, isLoading, error, fetchCards } = useCardsStore();

  useEffect(() => {
    if (user) {
      fetchCards(user.householdId);
    }
  }, [user, fetchCards]);

  return (
    <View style={[styles.container, { paddingTop: insets.top + APP_BAR_HEIGHT + spacing.md }]}>
      <Text variant="headlineSmall" style={styles.title}>
        Tarjetas
      </Text>

      {isLoading ? (
        <ActivityIndicator style={styles.loader} />
      ) : error ? (
        <Text style={styles.error}>{error}</Text>
      ) : (
        <FlatList
          data={cards}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          renderItem={({ item }) => (
            <Pressable style={styles.row} onPress={() => router.push(`/(app)/cards/${item.id}`)}>
              <View style={styles.rowIcon}>
                <Icon
                  source={item.type === 'Credit' ? 'credit-card-outline' : 'cash'}
                  size={22}
                  color={theme.colors.primary}
                />
              </View>
              <View style={styles.rowBody}>
                <Text variant="titleSmall" style={styles.rowTitle}>
                  {item.name}
                </Text>
                <Text variant="bodySmall" style={styles.rowSubtitle}>
                  {item.bank} · {item.type === 'Credit' ? `Corte día ${item.cutoffDay}` : 'Débito'}
                </Text>
              </View>
              <Icon source="chevron-right" size={20} color={theme.colors.onSurfaceVariant} />
            </Pressable>
          )}
          ListEmptyComponent={<Text style={styles.emptyText}>No hay tarjetas registradas todavía.</Text>}
        />
      )}

      {user?.role === 'Owner' ? (
        <FAB
          icon="plus"
          style={styles.fab}
          color={theme.colors.onPrimary}
          onPress={() => router.push('/(app)/cards/add')}
        />
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: theme.colors.background, padding: spacing.md },
  title: { color: theme.colors.onBackground, fontWeight: '700', marginBottom: spacing.md },
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
  rowIcon: {
    width: 40,
    height: 40,
    borderRadius: radius.sm,
    backgroundColor: theme.colors.primaryContainer,
    alignItems: 'center',
    justifyContent: 'center',
  },
  rowBody: { flex: 1 },
  rowTitle: { color: theme.colors.onSurface },
  rowSubtitle: { color: theme.colors.onSurfaceVariant, marginTop: 2 },
  emptyText: { color: theme.colors.onSurfaceVariant, textAlign: 'center', marginTop: spacing.lg },
  fab: {
    position: 'absolute',
    right: spacing.md,
    bottom: 96,
    backgroundColor: theme.colors.primary,
  },
});
