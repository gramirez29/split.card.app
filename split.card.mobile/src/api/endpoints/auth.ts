import { apiRequest, setAuthToken, clearAuthToken } from '@/api/client';
import type { User } from '@/types/user';

interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  token: string;
}

export async function login(credentials: LoginRequest) {
  const result = await apiRequest<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: credentials,
    requiresAuth: false,
  });

  if (result.ok) {
    await setAuthToken(result.data.token);
  }

  return result;
}

export async function logout(): Promise<void> {
  await clearAuthToken();
}

export function getCurrentUser() {
  return apiRequest<User>('/api/auth/me');
}
