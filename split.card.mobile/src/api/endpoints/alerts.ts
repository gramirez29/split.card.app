import { apiRequest } from '@/api/client';
import type { HouseholdAlerts } from '@/types/alerts';

export function getHouseholdAlerts(householdId: string) {
  return apiRequest<HouseholdAlerts>(`/api/households/${householdId}/alerts`);
}
