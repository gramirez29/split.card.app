import { apiDownloadFile } from '@/api/client';

export function downloadStatementPdf(cardId: string, date: string) {
  return apiDownloadFile(
    `/api/cards/${cardId}/statement-pdf?date=${date}`,
    `statement-${cardId}-${date}.pdf`,
  );
}
