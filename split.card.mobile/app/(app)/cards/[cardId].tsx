import { useLocalSearchParams } from 'expo-router';
import { View, Text, StyleSheet } from 'react-native';

export default function CardDetailScreen() {
  const { cardId } = useLocalSearchParams<{ cardId: string }>();

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Detalle de tarjeta</Text>
      <Text style={styles.subtitle}>ID: {cardId}</Text>
      <Text style={styles.note}>
        Listado de transacciones del periodo activo: pendiente de que el backend exponga
        GET /api/cards/{'{cardId}'}/transactions (ver Application/Controllers, siguiente fase).
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16 },
  title: { fontSize: 22, fontWeight: '700' },
  subtitle: { fontSize: 14, color: '#666', marginTop: 4 },
  note: { marginTop: 16, fontSize: 13, color: '#888' },
});
