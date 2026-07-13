import { create } from 'zustand';
import { getHouseholdDashboard } from '@/api/endpoints/dashboard';
import type { HouseholdDashboard } from '@/types/dashboard';

interface DashboardState {
  dashboard: HouseholdDashboard | null;
  isLoading: boolean;
  error: string | null;
  fetchDashboard: (householdId: string) => Promise<void>;
}

export const useDashboardStore = create<DashboardState>((set) => ({
  dashboard: null,
  isLoading: false,
  error: null,

  fetchDashboard: async (householdId) => {
    set({ isLoading: true, error: null });

    const result = await getHouseholdDashboard(householdId);

    if (!result.ok) {
      set({ isLoading: false, error: result.error.message });
      return;
    }

    set({ isLoading: false, dashboard: result.data });
  },
}));
