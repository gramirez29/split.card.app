import { create } from 'zustand';
import { getHouseholdMembers } from '@/api/endpoints/households';
import { inviteUser } from '@/api/endpoints/users';
import type { InviteUserRequest } from '@/api/endpoints/users';
import type { User } from '@/types/user';

interface HouseholdState {
  members: User[];
  isLoading: boolean;
  isSubmitting: boolean;
  error: string | null;
  fetchMembers: (householdId: string) => Promise<void>;
  inviteMember: (payload: InviteUserRequest) => Promise<boolean>;
}

export const useHouseholdStore = create<HouseholdState>((set) => ({
  members: [],
  isLoading: false,
  isSubmitting: false,
  error: null,

  fetchMembers: async (householdId) => {
    set({ isLoading: true, error: null });

    const result = await getHouseholdMembers(householdId);

    if (!result.ok) {
      set({ isLoading: false, error: result.error.message });
      return;
    }

    set({ isLoading: false, members: result.data });
  },

  inviteMember: async (payload) => {
    set({ isSubmitting: true, error: null });

    const result = await inviteUser(payload);

    if (!result.ok) {
      set({ isSubmitting: false, error: result.error.message });
      return false;
    }

    set((state) => ({ isSubmitting: false, members: [...state.members, result.data] }));
    return true;
  },
}));
