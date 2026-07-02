import { useAuthStore } from '@/store/authStore';

export function useAuth() {
  const user = useAuthStore((state) => state.user);
  const isAuthenticating = useAuthStore((state) => state.isAuthenticating);
  const login = useAuthStore((state) => state.login);
  const logout = useAuthStore((state) => state.logout);

  return {
    user,
    isAuthenticated: user !== null,
    isAuthenticating,
    login,
    logout,
  };
}
