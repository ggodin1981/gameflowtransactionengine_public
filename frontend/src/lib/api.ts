import type {
  AuditLogDto,
  DashboardOverviewResponse,
  PlayerProfileResponse,
  TransactionAcceptedResponse,
  TransactionSearchFilters,
  TransactionSearchItem,
} from "./types";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5051";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    ...init,
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export function fetchOverview() {
  return request<DashboardOverviewResponse>("/api/dashboard/overview");
}

export function searchTransactions(filters: TransactionSearchFilters) {
  const params = new URLSearchParams();

  Object.entries(filters).forEach(([key, value]) => {
    if (value) {
      params.set(key, value);
    }
  });

  return request<TransactionSearchItem[]>(`/api/transactions?${params.toString()}`);
}

export function fetchPlayer(externalPlayerId: string) {
  return request<PlayerProfileResponse>(`/api/players/${externalPlayerId}`);
}

export function fetchAuditLogs() {
  return request<AuditLogDto[]>("/api/audit-logs");
}

export function createTransaction(payload: {
  playerExternalId: string;
  playerUsername: string;
  country: string;
  currency: string;
  gameExternalId: string;
  gameName: string;
  provider: string;
  amount: number;
  type: number;
}) {
  return request<TransactionAcceptedResponse>("/api/transactions", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}
