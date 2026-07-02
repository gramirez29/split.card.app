import { create } from 'zustand';
import type { CreateTransactionRequest } from '@/types/transaction';

interface QueuedTransaction {
  localId: string;
  payload: CreateTransactionRequest;
  createdAt: string;
}

interface OfflineQueueState {
  pendingTransactions: QueuedTransaction[];
  enqueue: (payload: CreateTransactionRequest) => void;
  dequeue: (localId: string) => void;
}

// Cola en memoria únicamente: se pierde si la app se cierra sin conexión.
// Persistencia real (expo-sqlite vs AsyncStorage) queda pendiente de decisión
// — ver README de la app mobile.
export const useOfflineQueueStore = create<OfflineQueueState>((set) => ({
  pendingTransactions: [],

  enqueue: (payload) =>
    set((state) => ({
      pendingTransactions: [
        ...state.pendingTransactions,
        {
          localId: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
          payload,
          createdAt: new Date().toISOString(),
        },
      ],
    })),

  dequeue: (localId) =>
    set((state) => ({
      pendingTransactions: state.pendingTransactions.filter((t) => t.localId !== localId),
    })),
}));
