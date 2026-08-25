# Phase 16 — Operational Platform

Status: Complete (2026-08-25)

## Scope

Audit and worker hardening, Redis/backplane readiness, settings, health, retention and operational controls.

## Done Summary

- Installed repeat-safe `V1.15.0` in the live database with `SystemSettings`, `WorkerHeartbeats`, typed/scope constraints, precedence indexes, 33 safe platform defaults, audit search, system-health, worker-heartbeat and retention procedures.
- Implemented `Platform Default -> Tenant Default -> Store Override` resolution. Tenant/store identifiers originate in authenticated server context; store access is rechecked and unsafe face-similarity household auto-linking is platform-locked in both service and SQL.
- Added paged platform/tenant audit APIs which omit large before/after payloads and enforce tenant/store visibility before paging.
- Added durable Worker heartbeat, report artifact expiry/deletion acknowledgement, consent-expiry template erasure, biometric metadata lifecycle and anonymous-visitor retention with audit evidence.
- Added optional Redis distributed cache and SignalR Redis backplane configuration. Redis is disabled by default; when enabled and unavailable it reports a degraded warning without replacing SQL readiness.
- Added authenticated platform system health for API, SQL, Worker, Redis/backplane, SignalR connection/reconnect metrics, Python configuration status, cameras and queue depth.
- Added Angular platform health/settings/audit and tenant settings/audit pages, permission guards, pagination, loading/error/empty behavior and tenant-safe typed clients.
- Verified live SQL runner/constraints/rollback tests, real API liveness/readiness/auth boundary, real Worker heartbeat/stop state, real expired-artifact cleanup and privacy-retention SQL behavior.
- Validation: .NET build 0 warnings/errors; unit 98/98; integration 219/219; Angular lint, 78/78 tests and production build pass; Playwright 47/47 pass. The existing 61-byte SCSS budget warning remains non-failing.
