import { apiRequest } from '@/api/client';
import type { StatementPeriod } from '@/types/statementPeriod';

export function getStatementPeriod(cardId: string, date: string) {
  return apiRequest<StatementPeriod>(`/api/cards/${cardId}/statement-period?date=${date}`);
}
