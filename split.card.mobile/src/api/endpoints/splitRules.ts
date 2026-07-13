import { apiRequest } from '@/api/client';
import type { SplitRule } from '@/types/splitRule';
import type { PersonShare } from '@/types/personShare';

export function getSplitRules(householdId: string) {
  return apiRequest<SplitRule[]>(`/api/households/${householdId}/split-rules`);
}

export function createSplitRule(payload: Omit<SplitRule, 'id'>) {
  return apiRequest<SplitRule>('/api/split-rules', {
    method: 'POST',
    body: payload,
  });
}

// Backend devuelve 204 (sin body) si ningún SplitRule matchea `merchant` — apiRequest
// mapea eso a { ok: true, data: undefined }. Tratar `result.data` como opcional siempre.
export function suggestSplitForMerchant(householdId: string, merchant: string) {
  const query = new URLSearchParams({ merchant }).toString();
  return apiRequest<PersonShare[]>(`/api/households/${householdId}/split-rules/suggest?${query}`);
}
