import { apiRequest } from '@/api/client';
import type { CreateTransactionRequest, PeriodTransaction, Transaction } from '@/types/transaction';

// El backend resuelve el StatementPeriod internamente a partir de un solo `date`
// (yyyy-MM-dd), no un rango startDate/endDate — ver GET /api/cards/{cardId}/transactions
// en RequestSamples.md. Solo funciona para tarjetas Credit: el backend nunca crea un
// StatementPeriod para Debit (RegisterTransactionCommandHandler lo salta a propósito),
// así que esta llamada devuelve 404 siempre que cardId sea una tarjeta Debit. Para Debit,
// usar getVisibleTransactions + filtrar por cardId en el cliente (ver Card Detail).
//
// Devuelve PeriodTransaction, no Transaction — el backend expone `periodAmount` acá
// específicamente porque una compra en cuotas debe mostrar el monto de la cuota, no el
// total de la compra (bug real que ya se corrigió una vez, no reintroducirlo usando
// `amount` en vez de `periodAmount` en la UI).
export function getTransactionsForCard(cardId: string, date: string) {
  return apiRequest<PeriodTransaction[]>(`/api/cards/${cardId}/transactions?date=${date}`);
}

// Todas las transacciones visibles del household (Owner ve todo, el resto solo donde
// aparece en split) — sin filtro por tarjeta ni por periodo. Es el único endpoint que
// funciona para listar transacciones de una tarjeta Debit, filtrando por cardId acá.
// Devuelve Transaction (no PeriodTransaction) — no está resuelto por periodo, así que no
// tiene sentido mostrar un `periodAmount` acá; para Debit no hay concepto de cuotas de
// todas formas (el backend las rechaza al registrar).
export function getVisibleTransactions(householdId: string) {
  return apiRequest<Transaction[]>(`/api/households/${householdId}/transactions`);
}

export function createTransaction(payload: CreateTransactionRequest) {
  return apiRequest<Transaction>('/api/transactions', {
    method: 'POST',
    body: payload,
  });
}
