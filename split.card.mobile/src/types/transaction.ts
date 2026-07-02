import type { Currency } from './enums';
import type { PersonShare } from './personShare';

export interface Transaction {
  id: string;
  cardId: string;
  merchant: string;
  purchaseDate: string; // ISO date: yyyy-MM-dd
  amount: number;
  currency: Currency;
  installmentPlanId: string | null;
  createdByUserId: string;
  split: PersonShare[];
}

export interface CreateTransactionRequest {
  cardId: string;
  merchant: string;
  purchaseDate: string;
  amount: number;
  currency: Currency;
  installments: {
    totalInstallments: number;
    installmentAmount: number;
  } | null;
  split: PersonShare[];
}
