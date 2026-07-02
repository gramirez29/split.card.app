import type { Currency } from './enums';

export interface InstallmentPlan {
  id: string;
  totalInstallments: number;
  installmentAmount: number;
  currency: Currency;
  firstChargeDate: string; // ISO date: yyyy-MM-dd
}
