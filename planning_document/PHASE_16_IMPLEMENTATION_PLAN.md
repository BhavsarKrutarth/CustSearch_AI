# Phase 16 Implementation Plan — Operational Platform

## Requirement extraction

- Audit: permission-protected, paged platform and tenant audit search; tenant/store scope is server-derived.
- Worker: durable worker heartbeat and operational job execution without an interactive identity.
- Redis: optional distributed-cache health and SignalR Redis-backplane configuration; development must remain usable when Redis is disabled/unavailable.
- Settings: typed settings with `Platform Default -> Tenant Default -> Store Override` precedence; platform and tenant administration must not cross scope.
- System health: platform view for API, SQL, Worker, Redis/backplane, cameras, webhook/report queues and SignalR metrics.
- Retention: bounded, repeatable cleanup for expired report artifacts/events and other explicitly configured operational records.

## Existing implementation audit

| Area | Existing | Gap / decision |
|---|---|---|
| Audit | `AuditLogs`, indexes and writes exist; Phase 15 reports can show audit activity | No dedicated paged audit API/UI; implement stored-procedure search and authorized API/UI |
| Worker | Integration and report workers use SQL leases | No durable heartbeat/health state; add database heartbeat and retention worker |
| Redis/backplane | None | Add opt-in configuration and health checks; disabled is the safe default and does not block development |
| Settings | Permission catalog contains `Settings.View/Manage`; feature-specific settings exist | No general platform/tenant/store settings store or precedence resolver |
| Health | `/health/live` and SQL-only `/health/ready` exist | No authenticated operational summary or worker/queue/camera/SignalR status |
| Retention | Recognition withdrawal deletes templates; report expiry procedure marks rows expired | No worker that deletes expired artifact files and records operational cleanup |

## Database requirements

- Add `SystemSettings` with explicit scope checks, typed values, unique scope keys and tenant/store integrity.
- Add `WorkerHeartbeats` and a bounded heartbeat upsert.
- Add settings list/effective-resolution/upsert stored procedures.
- Add paged audit stored procedures that enforce platform or tenant/store scope.
- Add operational-health stored procedure and retention claim/cleanup procedures.
- Keep scripts rerunnable and register `V1.15.0` only after `V1.14.0`.

## Backend / frontend requirements

- Dapper repositories and services only for Phase 16 business queries.
- Controllers derive platform/tenant/store authorization from `ICurrentUserContext`.
- Worker heartbeat and retention hosted services with validated intervals/batch sizes.
- Opt-in Redis distributed cache and SignalR backplane; do not require Redis when disabled.
- Angular platform system-health and scoped settings/audit pages with permissions, loading/error/empty states and pagination.

## Security and acceptance criteria

- A tenant cannot read/write platform settings, another tenant's settings or unauthorized store overrides.
- A client-provided tenant identifier is never authoritative.
- Secret values are not stored in `SystemSettings`; deployments use environment/secret providers.
- Audit searches are bounded and do not expose `BeforeJson`/`AfterJson` unless explicitly authorized and requested.
- Retention is allow-listed and bounded; artifact deletion rejects path traversal and is idempotent.
- SQL upgrade/verification/tests, .NET build/tests, Angular lint/test/build and relevant worker/API tests execute successfully before Phase 16 is complete.
