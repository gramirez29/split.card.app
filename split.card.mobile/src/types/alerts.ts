import type { Currency } from './enums';

export interface CardAlert {
  cardId: string;
  cardName: string;
  alertType: 'CutoffSoon' | 'PaymentDueSoon';
  date: string;
  daysUntil: number;
}

export interface InstallmentAlert {
  cardId: string;
  cardName: string;
  transactionId: string;
  merchant: string;
  periodAmount: number;
  currency: Currency;
  installmentNumber: number;
  totalInstallments: number;
  paymentDueDate: string;
  daysUntilPaymentDue: number;
}

export interface HouseholdAlerts {
  cardAlerts: CardAlert[];
  installmentAlerts: InstallmentAlert[];
}
