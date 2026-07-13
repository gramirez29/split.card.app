import { create } from 'zustand';
import { createSplitRule, getSplitRules } from '@/api/endpoints/splitRules';
import type { SplitRule } from '@/types/splitRule';
import type { PersonShare } from '@/types/personShare';

interface CreateSplitRulePayload {
  householdId: string;
  descriptionPattern: string;
  defaultSplit: PersonShare[];
}

interface SplitRulesState {
  rules: SplitRule[];
  isLoading: boolean;
  isSubmitting: boolean;
  error: string | null;
  fetchRules: (householdId: string) => Promise<void>;
  createRule: (payload: CreateSplitRulePayload) => Promise<boolean>;
}

export const useSplitRulesStore = create<SplitRulesState>((set) => ({
  rules: [],
  isLoading: false,
  isSubmitting: false,
  error: null,

  fetchRules: async (householdId) => {
    set({ isLoading: true, error: null });

    const result = await getSplitRules(householdId);

    if (!result.ok) {
      set({ isLoading: false, error: result.error.message });
      return;
    }

    set({ isLoading: false, rules: result.data });
  },

  createRule: async (payload) => {
    set({ isSubmitting: true, error: null });

    const result = await createSplitRule(payload);

    if (!result.ok) {
      set({ isSubmitting: false, error: result.error.message });
      return false;
    }

    set((state) => ({ isSubmitting: false, rules: [...state.rules, result.data] }));
    return true;
  },
}));
