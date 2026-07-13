import { apiRequest } from '@/api/client';
import type { HouseholdDashboard } from '@/types/dashboard';

export function getHouseholdDashboard(householdId: string) {
  return apiRequest<HouseholdDashboard>(`/api/households/${householdId}/dashboard`);
}
