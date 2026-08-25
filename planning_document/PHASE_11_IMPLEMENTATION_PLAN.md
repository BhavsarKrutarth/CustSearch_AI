# Phase 11 — Alerts & Real-Time Implementation Plan

**Status:** Completed  
**Branch:** `phase11-alerts-realtime`  
**Validated baseline:** `AIMainBranch` at `a66721e64b56f7e7a5175ab5b56519661cff6ad7`  
**Database target:** SQL Server 2022 / V1.10.0

## Baseline gate

Phase 10 is merged and green. Pull request 12 merged at `a66721e64b56f7e7a5175ab5b56519661cff6ad7`; Phase 10 Validate run `32741657416` and all triggered Phase 6–10 workflows completed successfully. `planning_document/PHASE_10_TEST_REPORT.md` records the passing evidence.

## Architecture rules

1. Alert, recovery event and notification outbox records commit atomically.
2. External delivery never executes inside the main alert transaction.
3. Tenant identity, actor identity and authorized stores come only from the authenticated server context.
4. The browser cannot supply `TenantId` or join an arbitrary SignalR group.
5. Tenant-wide alerts publish only to `tenant:{TenantId}`; store alerts publish only to `store:{StoreId}`.
6. SignalR is a notification path, never the authoritative state store. REST recovery uses a durable event cursor.
7. Clients and the database de-duplicate by durable event/deduplication/idempotency keys.
8. Only configured channel adapters execute. No email, SMS, WhatsApp, push or provider credential is hard-coded.
9. Database deployment is versioned, standalone, repeat-safe T-SQL; no EF production migrations.

## 11A — Alert domain

Create tenant/store-scoped `Alerts` with type, severity, title/message, entity reference, lifecycle timestamps, acknowledgement actor, correlation and authoritative deduplication key. Support New, Delivered, Acknowledged, Resolved and Expired transitions.

## 11B — Transactional notification outbox

Create `NotificationOutbox` with Pending, Processing, Delivered, Failed, Retrying and DeadLetter states. Persist channel, versioned event payload, attempts, next attempt, last error, correlation and unique idempotency key in the same transaction as alert state.

## 11C/11D — SignalR and event contracts

Add authenticated `/hubs/alerts`. The server automatically assigns groups from validated claims. Publish versioned `alert.created`, `alert.updated`, `alert.acknowledged` and `alert.resolved` envelopes through a configured SignalR channel adapter.

## 11E/11F — Recovery and de-duplication

Persist ordered `RealtimeEvents`. Provide an authorized recovery endpoint using `afterEventId`, bounded page size and store filtering. Angular de-duplicates received/replayed event IDs and reloads the authoritative alert list after reconnect.

## 11G — Notification adapters

Ship in-app persistence and SignalR. Keep an adapter boundary for Email, SMS, WhatsApp and Push; unconfigured providers are never invoked and secrets remain environment/configuration owned.

## 11H — Health metrics

Expose outbox backlog, successes, failures, retries, dead letters, oldest pending timestamp, active SignalR connections and reconnect count.

## 11I — Angular

Add a permission-guarded notification center with unread count, alert detail, acknowledge/resolve actions, connection state, real-time updates and cursor-based missed-alert recovery.

## 11J — Validation gates

- tenant and store SignalR isolation
- no arbitrary group join API
- transactional outbox persistence
- retry/dead-letter behavior
- idempotency and concurrent duplicate suppression
- reconnect recovery and client event de-duplication
- unauthorized acknowledgement/resolve denial
- browser `TenantId` injection rejection
- full .NET, Angular, Playwright and Python regressions
- V1.10.0 upgrade twice on SQL Server 2022
- standalone `database/run-phase11.sql` twice plus verifier
- canonical fresh install through V1.10.0

## Completion rule

Satisfied on 2026-08-24. Phase 11 Validate run `32746620273` passed every required code, security, UI, regression and SQL Server gate. The validated canonical V1.10.0 SQL was persisted by commit `23ae4cb374a2462c29463bd300408e4630573a6c`. Detailed evidence is recorded in `planning_document/PHASE_11_TEST_REPORT.md`.
