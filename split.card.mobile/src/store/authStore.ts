import { create } from 'zustand';
import { login as loginRequest, logout as logoutRequest, getCurrentUser } from '@/api/endpoints/auth';
import type { User } from '@/types/user';

interface AuthState {
  user: User | null;
  isAuthenticating: boolean;
  error: string | null;
  login: (email: string, password: string) => Promise<boolean>;
  logout: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticating: false,
  error: null,

  login: async (email, password) => {
    set({ isAuthenticating: true, error: null });

    const loginResult = await loginRequest({ email, password });

    if (!loginResult.ok) {
      set({ isAuthenticating: false, error: loginResult.error.message });
      return false;
    }

    const userResult = await getCurrentUser();

    if (!userResult.ok) {
      set({ isAuthenticating: false, error: userResult.error.message });
      return false;
    }

    set({ isAuthenticating: false, user: userResult.data });
    return true;
  },

  logout: async () => {
    await logoutRequest();
    set({ user: null });
  },
}));
