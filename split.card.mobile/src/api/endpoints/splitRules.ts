import { apiRequest } from '@/api/client';
import type { SplitRule } from '@/types/splitRule';

export function getSplitRules(householdId: string) {
  return apiRequest<SplitRule[]>(`/api/households/${householdId}/split-rules`);
}

export function createSplitRule(payload: Omit<SplitRule, 'id'>) {
  return apiRequest<SplitRule>('/api/split-rules', {
    method: 'POST',
    body: payload,
  });
}
