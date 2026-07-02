import { apiRequest } from '@/api/client';
import type { Card } from '@/types/card';

export function getCards() {
  return apiRequest<Card[]>('/api/cards');
}

export function getCard(cardId: string) {
  return apiRequest<Card>(`/api/cards/${cardId}`);
}
