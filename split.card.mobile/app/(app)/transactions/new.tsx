import { useEffect, useState } from 'react';
import { View, Text, TextInput, Pressable, StyleSheet, Switch } from 'react-native';
import { router } from 'expo-router';
import { createTransaction } from '@/api/endpoints/transactions';
import { useCardsStore } from '@/store/cardsStore';
import { useAuth } from '@/hooks/useAuth';
import type { Currency } from '@/types/enums';
import type { PersonShare } from '@/types/personShare';

export default function NewTransactionScreen() {
  const { user } = useAuth();
  const { cards, fetchCards } = useCardsStore();
  const [cardId, setCardId] = useState(cards[0]?.id ?? '');

  useEffect(() => {
    if (cards.length === 0) {
      fetchCards();
    }
  }, [cards.length, fetchCards]);

  useEffect(() => {
    if (!cardId && cards[0]) {
      setCardId(cards[0].id);
    }
  }, [cardId, cards]);

  const [merchant, setMerchant] = useState('');
  const [amount, setAmount] = useState('');
  const [currency, setCurrency] = useState<Currency>('CRC');
  const [isInstallments, setIsInstallments] = useState(false);
  const [totalInstallments, setTotalInstallments] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  if (!user) {
    return null;
  }

  async function handleSubmit() {
    if (!user) {
      return;
    }

    setFormError(null);

    const numericAmount = Number(amount);

    if (!cardId || !merchant || !numericAmount || numericAmount <= 0) {
      setFormError('Completá tarjeta, comercio y un monto válido.');
      return;
    }

    // Split simplificado: 100% a quien registra la transacción. El picker de split
    // entre miembros del household (esposa/hija) queda pendiente de que el backend
    // exponga GET /api/households/{id}/members.
    const split: PersonShare[] = [{ personId: user.id, percentage: 100 }];

    setIsSubmitting(true);

    const result = await createTransaction({
      cardId,
      merchant,
      purchaseDate: new Date().toISOString().slice(0, 10),
      amount: numericAmount,
      currency,
      installments: isInstallments
        ? {
            totalInstallments: Number(totalInstallments),
            installmentAmount: numericAmount,
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
    <View style={styles.container}>
      <Text style={styles.title}>Nueva compra</Text>

      <TextInput
        style={styles.input}
        placeholder="Comercio"
        value={merchant}
        onChangeText={setMerchant}
      />

      <Text style={styles.label}>Tarjeta</Text>
      <View style={styles.cardPicker}>
        {cards.map((card) => (
          <Pressable
            key={card.id}
            style={[styles.cardOption, cardId === card.id && styles.cardOptionActive]}
            onPress={() => setCardId(card.id)}
          >
            <Text style={cardId === card.id ? styles.cardOptionTextActive : styles.cardOptionText}>
              {card.name}
            </Text>
          </Pressable>
        ))}
      </View>

      <TextInput
        style={styles.input}
        placeholder="Monto"
        keyboardType="decimal-pad"
        value={amount}
        onChangeText={setAmount}
      />

      <View style={styles.currencyRow}>
        <Pressable
          style={[styles.currencyOption, currency === 'CRC' && styles.currencyOptionActive]}
          onPress={() => setCurrency('CRC')}
        >
          <Text style={currency === 'CRC' ? styles.currencyTextActive : styles.currencyText}>CRC</Text>
        </Pressable>
        <Pressable
          style={[styles.currencyOption, currency === 'USD' && styles.currencyOptionActive]}
          onPress={() => setCurrency('USD')}
        >
          <Text style={currency === 'USD' ? styles.currencyTextActive : styles.currencyText}>USD</Text>
        </Pressable>
      </View>

      <View style={styles.row}>
        <Text>Cuotas</Text>
        <Switch value={isInstallments} onValueChange={setIsInstallments} />
      </View>

      {isInstallments ? (
        <TextInput
          style={styles.input}
          placeholder="Cantidad de meses"
          keyboardType="number-pad"
          value={totalInstallments}
          onChangeText={setTotalInstallments}
        />
      ) : null}

      {formError ? <Text style={styles.error}>{formError}</Text> : null}

      <Pressable style={styles.button} onPress={handleSubmit} disabled={isSubmitting}>
        <Text style={styles.buttonText}>{isSubmitting ? 'Guardando...' : 'Guardar'}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 16, gap: 12 },
  title: { fontSize: 22, fontWeight: '700', marginBottom: 8 },
  input: { borderWidth: 1, borderColor: '#ccc', borderRadius: 8, padding: 12, fontSize: 16 },
  label: { fontSize: 13, color: '#666', marginTop: 4 },
  cardPicker: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  cardOption: { borderWidth: 1, borderColor: '#ccc', borderRadius: 8, paddingVertical: 8, paddingHorizontal: 12 },
  cardOptionActive: { backgroundColor: '#111', borderColor: '#111' },
  cardOptionText: { color: '#111' },
  cardOptionTextActive: { color: '#fff', fontWeight: '600' },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', paddingVertical: 8 },
  currencyRow: { flexDirection: 'row', gap: 8 },
  currencyOption: { flex: 1, borderWidth: 1, borderColor: '#ccc', borderRadius: 8, padding: 10, alignItems: 'center' },
  currencyOptionActive: { backgroundColor: '#111', borderColor: '#111' },
  currencyText: { color: '#111' },
  currencyTextActive: { color: '#fff', fontWeight: '600' },
  button: { backgroundColor: '#111', borderRadius: 8, padding: 14, alignItems: 'center', marginTop: 8 },
  buttonText: { color: '#fff', fontSize: 16, fontWeight: '600' },
  error: { color: '#c0392b' },
});
