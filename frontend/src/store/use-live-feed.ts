import { create } from "zustand";
import type { TransactionLifecycleEvent } from "@/lib/types";

interface NotificationItem {
  id: string;
  title: string;
  body: string;
  createdAtUtc: string;
}

interface LiveFeedState {
  events: TransactionLifecycleEvent[];
  notifications: NotificationItem[];
  pushEvent: (event: TransactionLifecycleEvent) => void;
}

export const useLiveFeedStore = create<LiveFeedState>((set) => ({
  events: [],
  notifications: [],
  pushEvent: (event) =>
    set((state) => ({
      events: [event, ...state.events].slice(0, 12),
      notifications: [
        {
          id: `${event.transactionId}-${event.stage}`,
          title: `${event.externalTransactionId} ${event.stage}`,
          body: `${event.playerUsername} | ${event.gameName} | ${event.message}`,
          createdAtUtc: event.occurredAtUtc,
        },
        ...state.notifications,
      ].slice(0, 8),
    })),
}));
