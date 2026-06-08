# Free Demo Deployment

This repo now supports a reduced-cost `demo mode` so you can deploy a public portfolio version without RabbitMQ, Redis, a separate worker, or Elasticsearch.

## What demo mode does

- keeps the API, Postgres, frontend, and SignalR demo visible
- processes transactions in-memory inside the API process
- falls back to in-memory cache when `Redis__ConnectionString` is empty
- keeps live transaction updates via the SignalR service
- avoids RabbitMQ and Elasticsearch as hard runtime dependencies

## Recommended free demo topology

- frontend: Netlify
- API: Render web service
- SignalR: Render web service
- database: Supabase Postgres

## Required environment variables for API

```text
ASPNETCORE_URLS=http://+:8080
DemoMode__Enabled=true
Postgres__ConnectionString=<your-supabase-postgres-connection-string>
Redis__ConnectionString=
SignalRService__BaseUrl=https://<your-signalr-service>/ 
SignalRService__ApiKey=local-dev-key
```

Leave `Redis__ConnectionString` empty to use in-memory cache.

## Required environment variables for SignalR

```text
ASPNETCORE_URLS=http://+:8080
InternalAuth__ApiKey=local-dev-key
```

## Netlify frontend variables

```text
VITE_API_BASE_URL=https://<your-api-service>
VITE_SIGNALR_URL=https://<your-signalr-service>
```

## Deploy order

1. Create a Supabase project and copy the Postgres connection string.
2. Deploy `gameflow-signalr-demo` first.
3. Deploy `gameflow-api-demo` with `DemoMode__Enabled=true`.
4. Deploy the frontend to Netlify.

## Notes

- In demo mode, transaction processing is still asynchronous, but the queue is in-memory inside the API service.
- If the API instance restarts, in-flight in-memory queued messages are lost. That is acceptable for a portfolio demo, not for production.
- Existing local Docker Compose flow still works for the full architecture.
