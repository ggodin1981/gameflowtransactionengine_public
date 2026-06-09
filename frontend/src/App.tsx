import { useEffect, useRef, useState, startTransition, useDeferredValue } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  Bell,
  CircleAlert,
  Coins,
  Database,
  Search,
  ShieldCheck,
  UserRoundSearch,
  Wifi,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { fetchAuditLogs, fetchOverview, fetchPlayer, searchTransactions } from "@/lib/api";
import { queryClient } from "@/lib/query-client";
import { connectTransactionHub } from "@/lib/signalr";
import type { TransactionLifecycleEvent, TransactionSearchFilters, TransactionSearchItem } from "@/lib/types";
import { useLiveFeedStore } from "@/store/use-live-feed";

const numberFormatter = new Intl.NumberFormat("en-US");
const currencyFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "EUR",
  maximumFractionDigits: 2,
});

function getStatusVariant(status: string): "neutral" | "success" | "warning" | "danger" | "info" {
  switch (status.toLowerCase()) {
    case "completed":
    case "healthy":
      return "success";
    case "processing":
      return "info";
    case "failed":
      return "danger";
    case "pending":
    case "unknown":
      return "warning";
    default:
      return "neutral";
  }
}

function mergeTransactions(
  liveEvents: TransactionLifecycleEvent[],
  apiTransactions: TransactionSearchItem[] | undefined,
): TransactionSearchItem[] {
  const liveMapped = liveEvents.map((event) => ({
    transactionId: event.transactionId,
    externalTransactionId: event.externalTransactionId,
    correlationId: event.correlationId,
    playerExternalId: "live-stream",
    playerUsername: event.playerUsername,
    gameExternalId: "live-stream",
    gameName: event.gameName,
    amount: event.amount,
    currency: event.currency,
    status: event.status,
    type: "Live",
    createdAtUtc: event.occurredAtUtc,
    processedAtUtc: event.occurredAtUtc,
    failureReason: event.status === "Failed" ? event.message : null,
  }));

  const combined = [...liveMapped, ...(apiTransactions ?? [])];
  const deduplicated = new Map(combined.map((item) => [item.externalTransactionId, item]));
  return Array.from(deduplicated.values()).slice(0, 10);
}

export default function App() {
  const [searchDraft, setSearchDraft] = useState<TransactionSearchFilters>({});
  const [submittedFilters, setSubmittedFilters] = useState<TransactionSearchFilters>({});
  const [playerLookupDraft, setPlayerLookupDraft] = useState("PLY-10001");
  const [selectedPlayerId, setSelectedPlayerId] = useState("PLY-10001");

  const deferredFilters = useDeferredValue(submittedFilters);
  const liveEvents = useLiveFeedStore((state) => state.events);
  const notifications = useLiveFeedStore((state) => state.notifications);
  const pushEvent = useLiveFeedStore((state) => state.pushEvent);

  const overviewQuery = useQuery({
    queryKey: ["overview"],
    queryFn: fetchOverview,
  });

  const transactionsQuery = useQuery({
    queryKey: ["transactions", deferredFilters],
    queryFn: () => searchTransactions(deferredFilters),
  });

  const playerQuery = useQuery({
    queryKey: ["player", selectedPlayerId],
    queryFn: () => fetchPlayer(selectedPlayerId),
    enabled: selectedPlayerId.length > 0,
  });

  const auditLogsQuery = useQuery({
    queryKey: ["audit-logs"],
    queryFn: fetchAuditLogs,
  });

  const handleTransactionUpdateRef = useRef<(event: TransactionLifecycleEvent) => void>(() => undefined);
  handleTransactionUpdateRef.current = (event: TransactionLifecycleEvent) => {
    pushEvent(event);
    queryClient.invalidateQueries({ queryKey: ["overview"] });
    queryClient.invalidateQueries({ queryKey: ["transactions"] });
    queryClient.invalidateQueries({ queryKey: ["audit-logs"] });
  };

  useEffect(() => {
    let active = true;
    let connectionPromise: Promise<{ stop: () => Promise<void> } | null>;

    connectionPromise = connectTransactionHub((event) => handleTransactionUpdateRef.current(event))
      .then((connection) => {
        if (!active) {
          void connection.stop();
          return null;
        }

        return connection;
      })
      .catch(() => null);

    return () => {
      active = false;
      void connectionPromise.then((connection) => connection?.stop());
    };
    // `handleTransactionUpdate` is an Effect Event, so it always sees fresh state without
    // requiring the hub connection to be recreated on re-render.
  }, []);

  const monitorItems = mergeTransactions(liveEvents, transactionsQuery.data);

  const statCards = [
    {
      label: "Transactions / 24h",
      value: numberFormatter.format(overviewQuery.data?.totalTransactions24h ?? 0),
      icon: Activity,
    },
    {
      label: "Settled Amount",
      value: currencyFormatter.format(overviewQuery.data?.settledAmount24h ?? 0),
      icon: Coins,
    },
    {
      label: "Failed Requests",
      value: numberFormatter.format(overviewQuery.data?.failedTransactions24h ?? 0),
      icon: CircleAlert,
    },
    {
      label: "Active Players",
      value: numberFormatter.format(overviewQuery.data?.activePlayers24h ?? 0),
      icon: UserRoundSearch,
    },
  ];

  return (
    <main className="mx-auto flex min-h-screen w-full max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <section className="grid gap-6 lg:grid-cols-[1.4fr_0.8fr]">
        <Card className="overflow-hidden border-cyan-400/15 bg-gradient-to-br from-slatepanel via-slatepanel to-midnight p-8">
          <div className="flex flex-wrap items-center gap-3">
            <Badge variant="info">GameFlow Transaction Engine</Badge>
            <Badge>React 19 + SignalR</Badge>
            <Badge>RabbitMQ + PostgreSQL + Elasticsearch</Badge>
          </div>
          <div className="mt-6 max-w-3xl">
            <p className="font-display text-4xl font-semibold leading-tight text-white sm:text-5xl">
              Real-time transaction operations for gaming platforms.
            </p>
            <p className="mt-4 max-w-2xl text-sm leading-7 text-slate-300 sm:text-base">
              A live control surface for distributed transaction orchestration: ingestion, worker
              processing, search, player intelligence, health signals, and audit visibility.
            </p>
          </div>
          <div className="mt-8 flex flex-wrap gap-3 text-xs text-slate-400">
            <span>API {"->"} RabbitMQ {"->"} Worker {"->"} PostgreSQL {"->"} SignalR {"->"} Dashboard</span>
            <span>Redis caching</span>
            <span>Elasticsearch indexing</span>
          </div>
        </Card>

        <Card className="flex flex-col justify-between border-emerald-400/15">
          <div>
            <div className="flex items-center gap-3">
              <div className="rounded-2xl bg-emerald-500/10 p-3 text-emerald-300">
                <ShieldCheck className="h-5 w-5" />
              </div>
              <div>
                <p className="text-sm text-slate-400">System Health Snapshot</p>
                <p className="font-display text-xl text-white">Operational Posture</p>
              </div>
            </div>
            <div className="mt-6 space-y-3">
              {(overviewQuery.data?.serviceHealth ?? []).map((service) => (
                <div key={service.service} className="rounded-2xl border border-white/8 bg-white/5 p-4">
                  <div className="flex items-center justify-between gap-4">
                    <span className="font-medium text-white">{service.service}</span>
                    <Badge variant={getStatusVariant(service.status)}>{service.status}</Badge>
                  </div>
                  <p className="mt-2 text-sm text-slate-400">{service.detail}</p>
                </div>
              ))}
            </div>
          </div>
          <div className="mt-6 flex items-center justify-between rounded-2xl border border-cyan-400/10 bg-cyan-500/5 px-4 py-3 text-sm text-slate-300">
            <span>SignalR Active Connections</span>
            <span className="font-semibold text-cyan-300">
              {overviewQuery.data?.activeConnections ?? 0}
            </span>
          </div>
        </Card>
      </section>

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {statCards.map((item) => (
          <Card key={item.label} className="border-white/8">
            <div className="flex items-start justify-between">
              <div>
                <p className="text-sm text-slate-400">{item.label}</p>
                <p className="mt-3 font-display text-3xl text-white">{item.value}</p>
              </div>
              <div className="rounded-2xl bg-white/5 p-3 text-cyan-300">
                <item.icon className="h-5 w-5" />
              </div>
            </div>
          </Card>
        ))}
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <Card>
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="text-sm text-slate-400">Transaction Monitor</p>
              <h2 className="font-display text-2xl text-white">Live throughput feed</h2>
            </div>
            <Badge variant="info">
              <Wifi className="mr-2 h-3.5 w-3.5" />
              SignalR streaming
            </Badge>
          </div>
          <div className="mt-5 overflow-hidden rounded-3xl border border-white/8">
            <div className="grid grid-cols-[1.4fr_1fr_1fr_0.8fr] gap-4 border-b border-white/8 bg-white/5 px-4 py-3 text-xs uppercase tracking-[0.2em] text-slate-400">
              <span>Transaction</span>
              <span>Player / Game</span>
              <span>Amount</span>
              <span>Status</span>
            </div>
            <div className="divide-y divide-white/6">
              {monitorItems.map((item) => (
                <div
                  key={item.externalTransactionId}
                  className="grid grid-cols-[1.4fr_1fr_1fr_0.8fr] gap-4 px-4 py-4 text-sm"
                >
                  <div>
                    <p className="font-medium text-white">{item.externalTransactionId}</p>
                    <p className="mt-1 text-xs text-slate-500">{item.correlationId}</p>
                  </div>
                  <div>
                    <p className="text-white">{item.playerUsername}</p>
                    <p className="mt-1 text-xs text-slate-500">{item.gameName}</p>
                  </div>
                  <div className="text-white">
                    {currencyFormatter.format(item.amount)} {item.currency}
                  </div>
                  <div>
                    <Badge variant={getStatusVariant(item.status)}>{item.status}</Badge>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </Card>

        <Card>
          <div className="flex items-center gap-3">
            <div className="rounded-2xl bg-white/5 p-3 text-cyan-300">
              <Bell className="h-5 w-5" />
            </div>
            <div>
              <p className="text-sm text-slate-400">Live Notifications</p>
              <h2 className="font-display text-2xl text-white">Operator inbox</h2>
            </div>
          </div>
          <div className="mt-5 space-y-3">
            {notifications.length === 0 && (
              <div className="rounded-2xl border border-dashed border-white/10 p-5 text-sm text-slate-500">
                Waiting for worker lifecycle events.
              </div>
            )}
            {notifications.map((item) => (
              <div key={item.id} className="rounded-2xl border border-white/8 bg-white/5 p-4">
                <p className="font-medium text-white">{item.title}</p>
                <p className="mt-2 text-sm text-slate-400">{item.body}</p>
                <p className="mt-3 text-xs uppercase tracking-[0.2em] text-slate-500">
                  {new Date(item.createdAtUtc).toLocaleString()}
                </p>
              </div>
            ))}
          </div>
        </Card>
      </section>

      <section className="grid gap-6 lg:grid-cols-[1fr_1fr]">
        <Card>
          <div className="flex items-center gap-3">
            <Search className="h-5 w-5 text-cyan-300" />
            <div>
              <p className="text-sm text-slate-400">Search Transactions</p>
              <h2 className="font-display text-2xl text-white">Elasticsearch-facing filters</h2>
            </div>
          </div>
          <form
            className="mt-5 grid gap-3 sm:grid-cols-2"
            onSubmit={(event) => {
              event.preventDefault();
              startTransition(() => setSubmittedFilters(searchDraft));
            }}
          >
            <Input
              placeholder="Player ID or username"
              value={searchDraft.player ?? ""}
              onChange={(event) =>
                setSearchDraft((current) => ({ ...current, player: event.target.value }))
              }
            />
            <Input
              placeholder="Transaction ID"
              value={searchDraft.transactionId ?? ""}
              onChange={(event) =>
                setSearchDraft((current) => ({ ...current, transactionId: event.target.value }))
              }
            />
            <Input
              placeholder="Game name"
              value={searchDraft.game ?? ""}
              onChange={(event) =>
                setSearchDraft((current) => ({ ...current, game: event.target.value }))
              }
            />
            <Input
              placeholder="Status: Completed, Failed, Processing"
              value={searchDraft.status ?? ""}
              onChange={(event) =>
                setSearchDraft((current) => ({ ...current, status: event.target.value }))
              }
            />
            <Button className="sm:col-span-2" type="submit">
              Run Search
            </Button>
          </form>
          <div className="mt-5 space-y-3">
            {(transactionsQuery.data ?? []).slice(0, 4).map((transaction) => (
              <div key={transaction.transactionId} className="rounded-2xl border border-white/8 bg-white/5 p-4">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="font-medium text-white">{transaction.externalTransactionId}</p>
                    <p className="mt-1 text-sm text-slate-400">
                      {transaction.playerUsername} on {transaction.gameName}
                    </p>
                  </div>
                  <Badge variant={getStatusVariant(transaction.status)}>{transaction.status}</Badge>
                </div>
              </div>
            ))}
          </div>
        </Card>

        <Card>
          <div className="flex items-center gap-3">
            <UserRoundSearch className="h-5 w-5 text-emerald-300" />
            <div>
              <p className="text-sm text-slate-400">Player Lookup</p>
              <h2 className="font-display text-2xl text-white">Redis-backed profile snapshot</h2>
            </div>
          </div>
          <form
            className="mt-5 flex gap-3"
            onSubmit={(event) => {
              event.preventDefault();
              startTransition(() => setSelectedPlayerId(playerLookupDraft));
            }}
          >
            <Input
              value={playerLookupDraft}
              onChange={(event) => setPlayerLookupDraft(event.target.value)}
              placeholder="PLY-10001"
            />
            <Button type="submit">Lookup</Button>
          </form>
          {playerQuery.data && (
            <div className="mt-5 grid gap-3 sm:grid-cols-2">
              <div className="rounded-2xl border border-white/8 bg-white/5 p-4">
                <p className="text-sm text-slate-400">Username</p>
                <p className="mt-2 text-lg text-white">{playerQuery.data.username}</p>
              </div>
              <div className="rounded-2xl border border-white/8 bg-white/5 p-4">
                <p className="text-sm text-slate-400">Lifetime Volume</p>
                <p className="mt-2 text-lg text-white">
                  {currencyFormatter.format(playerQuery.data.lifetimeVolume)}
                </p>
              </div>
              <div className="rounded-2xl border border-white/8 bg-white/5 p-4">
                <p className="text-sm text-slate-400">Country / Currency</p>
                <p className="mt-2 text-lg text-white">
                  {playerQuery.data.country} / {playerQuery.data.currency}
                </p>
              </div>
              <div className="rounded-2xl border border-white/8 bg-white/5 p-4">
                <p className="text-sm text-slate-400">Transactions</p>
                <p className="mt-2 text-lg text-white">{playerQuery.data.totalTransactions}</p>
              </div>
            </div>
          )}
        </Card>
      </section>

      <section className="grid gap-6 lg:grid-cols-[1fr_1fr]">
        <Card>
          <div className="flex items-center gap-3">
            <Database className="h-5 w-5 text-amber-300" />
            <div>
              <p className="text-sm text-slate-400">Audit Logs</p>
              <h2 className="font-display text-2xl text-white">Traceable operator events</h2>
            </div>
          </div>
          <div className="mt-5 space-y-3">
            {(auditLogsQuery.data ?? []).slice(0, 6).map((log) => (
              <div key={log.auditLogId} className="rounded-2xl border border-white/8 bg-white/5 p-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="font-medium text-white">{log.action}</p>
                  <p className="text-xs uppercase tracking-[0.2em] text-slate-500">{log.actor}</p>
                </div>
                <p className="mt-2 text-sm text-slate-400">
                  {log.entityType} / {log.entityId}
                </p>
              </div>
            ))}
          </div>
        </Card>

        <Card>
          <div className="flex items-center gap-3">
            <Activity className="h-5 w-5 text-cyan-300" />
            <div>
              <p className="text-sm text-slate-400">Recent Activity</p>
              <h2 className="font-display text-2xl text-white">Dashboard heartbeat</h2>
            </div>
          </div>
          <div className="mt-5 space-y-3">
            {(overviewQuery.data?.recentActivity ?? []).map((activity) => (
              <div key={activity.transactionId} className="rounded-2xl border border-white/8 bg-white/5 p-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="font-medium text-white">{activity.externalTransactionId}</p>
                  <Badge variant={getStatusVariant(activity.status)}>{activity.status}</Badge>
                </div>
                <p className="mt-2 text-sm text-slate-400">
                  {activity.playerUsername} on {activity.gameName}
                </p>
                <p className="mt-2 text-sm text-white">
                  {currencyFormatter.format(activity.amount)} {activity.currency}
                </p>
              </div>
            ))}
          </div>
        </Card>
      </section>
    </main>
  );
}
