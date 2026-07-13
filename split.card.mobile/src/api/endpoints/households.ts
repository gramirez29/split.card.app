import { apiRequest } from '@/api/client';
import type { User } from '@/types/user';

export function getHouseholdMembers(householdId: string) {
  return apiRequest<User[]>(`/api/households/${householdId}/members`);
}

export interface RegisterHouseholdRequest {
  householdName: string;
  ownerName: string;
  ownerEmail: string;
  ownerPassword: string;
}

export interface RegisterHouseholdResponse {
  householdId: string;
  ownerUserId: string;
}

export function registerHousehold(payload: RegisterHouseholdRequest) {
  return apiRequest<RegisterHouseholdResponse>('/api/households', {
    method: 'POST',
    body: payload,
    requiresAuth: false,
  });
}
