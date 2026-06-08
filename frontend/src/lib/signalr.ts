import * as signalR from "@microsoft/signalr";
import type { TransactionLifecycleEvent } from "./types";

const signalRUrl = import.meta.env.VITE_SIGNALR_URL ?? "http://localhost:5053";

export async function connectTransactionHub(
  onTransactionUpdated: (event: TransactionLifecycleEvent) => void,
) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${signalRUrl}/hubs/transactions`)
    .withAutomaticReconnect()
    .build();

  connection.on("transaction-updated", onTransactionUpdated);
  await connection.start();

  return connection;
}
