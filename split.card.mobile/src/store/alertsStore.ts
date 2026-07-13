import { create } from 'zustand';
import { getHouseholdAlerts } from '@/api/endpoints/alerts';
import type { HouseholdAlerts } from '@/types/alerts';

interface AlertsState {
  alerts: HouseholdAlerts | null;
  isLoading: boolean;
  error: string | null;
  fetchAlerts: (householdId: string) => Promise<void>;
}

export const useAlertsStore = create<AlertsState>((set) => ({
  alerts: null,
  isLoading: false,
  error: null,

  fetchAlerts: async (householdId) => {
    set({ isLoading: true, error: null });

    const result = await getHouseholdAlerts(householdId);

    if (!result.ok) {
      set({ isLoading: false, error: result.error.message });
      return;
    }

    set({ isLoading: false, alerts: result.data });
  },
}));
