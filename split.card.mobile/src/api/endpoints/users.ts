import { apiRequest } from '@/api/client';
import type { User } from '@/types/user';
import type { UserRole } from '@/types/enums';

export interface InviteUserRequest {
  householdId: string;
  name: string;
  email: string;
  password: string;
  role: UserRole;
}

export function inviteUser(payload: InviteUserRequest) {
  return apiRequest<User>('/api/users/invite', {
    method: 'POST',
    body: payload,
  });
}
