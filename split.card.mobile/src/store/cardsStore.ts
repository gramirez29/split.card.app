import { create } from 'zustand';
import { createCard as createCardRequest, getCardsForHousehold } from '@/api/endpoints/cards';
import type { CreateCardRequest } from '@/api/endpoints/cards';
import type { Card } from '@/types/card';

interface CardsState {
  cards: Card[];
  isLoading: boolean;
  isSubmitting: boolean;
  error: string | null;
  fetchCards: (householdId: string) => Promise<void>;
  createCard: (payload: CreateCardRequest) => Promise<boolean>;
}

export const useCardsStore = create<CardsState>((set) => ({
  cards: [],
  isLoading: false,
  isSubmitting: false,
  error: null,

  fetchCards: async (householdId) => {
    set({ isLoading: true, error: null });

    const result = await getCardsForHousehold(householdId);

    if (!result.ok) {
      set({ isLoading: false, error: result.error.message });
      return;
    }

    set({ isLoading: false, cards: result.data });
  },

  createCard: async (payload) => {
    set({ isSubmitting: true, error: null });

    const result = await createCardRequest(payload);

    if (!result.ok) {
      set({ isSubmitting: false, error: result.error.message });
      return false;
    }

    set((state) => ({ isSubmitting: false, cards: [...state.cards, result.data] }));
    return true;
  },
}));
