import { apiRequest } from '@/api/client';
import type { CreateTransactionRequest, Transaction } from '@/types/transaction';

export function getTransactionsForCard(cardId: string, startDate: string, endDate: string) {
  const query = new URLSearchParams({ startDate, endDate }).toString();
  return apiRequest<Transaction[]>(`/api/cards/${cardId}/transactions?${query}`);
}

export function createTransaction(payload: CreateTransactionRequest) {
  return apiRequest<Transaction>('/api/transactions', {
    method: 'POST',
    body: payload,
  });
}
