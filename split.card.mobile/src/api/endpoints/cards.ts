import { apiRequest } from '@/api/client';
import type { Card } from '@/types/card';
import type { CardType } from '@/types/enums';

export interface CreateCardRequest {
  householdId: string;
  name: string;
  bank: string;
  type: CardType;
  cutoffDay: number | null;
  paymentDueDay: number | null;
}

// No hay GET /api/cards ni GET /api/cards/{cardId} en el backend — solo
// GET /api/households/{householdId}/cards (ver RequestSamples.md de split.card.service).
export function getCardsForHousehold(householdId: string) {
  return apiRequest<Card[]>(`/api/households/${householdId}/cards`);
}

export function createCard(payload: CreateCardRequest) {
  return apiRequest<Card>('/api/cards', {
    method: 'POST',
    body: payload,
  });
}
