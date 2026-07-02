import { create } from 'zustand';
import { getCards } from '@/api/endpoints/cards';
import type { Card } from '@/types/card';

interface CardsState {
  cards: Card[];
  isLoading: boolean;
  error: string | null;
  fetchCards: () => Promise<void>;
}

export const useCardsStore = create<CardsState>((set) => ({
  cards: [],
  isLoading: false,
  error: null,

  fetchCards: async () => {
    set({ isLoading: true, error: null });

    const result = await getCards();

    if (!result.ok) {
      set({ isLoading: false, error: result.error.message });
      return;
    }

    set({ isLoading: false, cards: result.data });
  },
}));
