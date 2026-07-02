import { useEffect } from 'react';
import { View, Text, FlatList, Pressable, StyleSheet, ActivityIndicator } from 'react-native';
import { router } from 'expo-router';
import { useCardsStore } from '@/store/cardsStore';

export default function DashboardScreen() {
  const { cards, isLoading, error, fetchCards } = useCardsStore();

  useEffect(() => {
    fetchCards();
  }, [fetchCards]);

  if (isLoading) {
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
      <Text style={styles.title}>Tus tarjetas</Text>

      <FlatList
        data={cards}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <Pressable
            style={styles.cardRow}
            onPress={() => router.push(`/(app)/cards/${item.id}`)}
          >
            <Text style={styles.cardName}>{item.name}</Text>
            <Text style={styles.cardBank}>{item.bank}</Text>
          </Pressable>
        )}
        ListEmptyComponent={<Text>No hay tarjetas registradas todavía.</Text>}
      />

      <Pressable style={styles.fab} onPress={() => router.push('/(app)/transactions/new')}>
        <Text style={styles.fabText}>+ Nueva compra</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16 },
  center: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  title: { fontSize: 22, fontWeight: '700', marginBottom: 16 },
  cardRow: { paddingVertical: 12, borderBottomWidth: 1, borderBottomColor: '#eee' },
  cardName: { fontSize: 16, fontWeight: '600' },
  cardBank: { fontSize: 13, color: '#666' },
  error: { color: '#c0392b' },
  fab: { backgroundColor: '#111', borderRadius: 8, padding: 14, alignItems: 'center', marginTop: 16 },
  fabText: { color: '#fff', fontSize: 16, fontWeight: '600' },
});
