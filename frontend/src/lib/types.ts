export type TransactionStatus = "Pending" | "Processing" | "Completed" | "Failed";

export interface TransactionSearchItem {
  transactionId: string;
  externalTransactionId: string;
  correlationId: string;
  playerExternalId: string;
  playerUsername: string;
  gameExternalId: string;
  gameName: string;
  amount: number;
  currency: string;
  status: TransactionStatus;
  type: string;
  createdAtUtc: string;
  processedAtUtc?: string | null;
  failureReason?: string | null;
}

export interface TransactionAcceptedResponse {
  transactionId: string;
  externalTransactionId: string;
  correlationId: string;
  status: TransactionStatus;
  createdAtUtc: string;
}

export interface PlayerProfileResponse {
  playerId: string;
  externalPlayerId: string;
  username: string;
  country: string;
  currency: string;
  totalTransactions: number;
  lifetimeVolume: number;
  lastActivityUtc: string;
}

export interface TransactionActivity {
  transactionId: string;
  externalTransactionId: string;
  playerUsername: string;
  gameName: string;
  amount: number;
  currency: string;
  status: TransactionStatus;
  occurredAtUtc: string;
}

export interface ServiceHealth {
  service: string;
  status: string;
  detail: string;
}

export interface DashboardOverviewResponse {
  totalTransactions24h: number;
  settledAmount24h: number;
  failedTransactions24h: number;
  activePlayers24h: number;
  queueDepth: number;
  activeConnections: number;
  recentActivity: TransactionActivity[];
  serviceHealth: ServiceHealth[];
}

export interface AuditLogDto {
  auditLogId: string;
  action: string;
  actor: string;
  entityType: string;
  entityId: string;
  detailsJson: string;
  createdAtUtc: string;
}

export interface TransactionLifecycleEvent {
  transactionId: string;
  externalTransactionId: string;
  correlationId: string;
  status: TransactionStatus;
  stage: string;
  message: string;
  amount: number;
  currency: string;
  playerUsername: string;
  gameName: string;
  occurredAtUtc: string;
}

export interface TransactionSearchFilters {
  player?: string;
  transactionId?: string;
  game?: string;
  status?: string;
}
