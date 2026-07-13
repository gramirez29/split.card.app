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

// Distinto shape de Transaction a propósito — lo que devuelve
// GET /api/cards/{cardId}/transactions?date=... (backend: PeriodTransactionResponse).
// `amount` sigue siendo el total original de la compra; `periodAmount` es lo que
// corresponde pagar en ESE periodo específico — son el mismo valor para una compra sin
// cuotas, pero distintos para una compra en cuotas. Cualquier suma/total mostrado en
// pantalla tiene que usar `periodAmount`, nunca `amount` — usar `amount` acá fue
// exactamente el bug que se corrigió (mostraba el total de la compra en vez de la cuota).
export interface PeriodTransaction {
  id: string;
  cardId: string;
  merchant: string;
  purchaseDate: string;
  amount: number;
  periodAmount: number;
  currency: Currency;
  installmentPlanId: string | null;
  installmentNumber: number | null;
  totalInstallments: number | null;
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
