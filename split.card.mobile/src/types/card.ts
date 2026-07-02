import type { CardType } from './enums';

export interface Card {
  id: string;
  householdId: string;
  name: string;
  bank: string;
  type: CardType;
  cutoffDay: number | null;
  paymentDueDay: number | null;
  ownerUserId: string;
}
