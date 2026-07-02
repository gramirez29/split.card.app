import type { StatementStatus } from './enums';

export interface StatementPeriod {
  id: string;
  cardId: string;
  startDate: string;
  endDate: string;
  paymentDueDate: string;
  status: StatementStatus;
}
