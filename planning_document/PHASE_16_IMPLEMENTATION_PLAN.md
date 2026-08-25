# Phase 16 — Operational Platform Implementation Plan

**Branch:** `phase16-operations`  
**Prerequisite:** exact fully green Phase 15 head `baeee4d0ace5f4fe0e1abf92bfad1926ea767461`

1. Harden `AuditLogs` as append-only application state, reject unsafe sensitive metadata, and add SQL update protection.
2. Centralize platform/tenant/store settings with Store → Tenant → Platform precedence and keep opaque secret references in a separate table/API.
3. Coordinate notifications, integrations, exports and retention with SQL-authoritative pause controls, serializable leases, idempotent batches, heartbeats and graceful cancellation.
4. Prepare optional Redis SignalR scale-out while retaining SQL as the only source of critical queue/audit truth.
5. Add liveness/readiness for SQL, optional Redis, worker heartbeat and SignalR registration.
6. Add bounded audited retention policies/runs and safe dead-letter retry controls.
7. Add a permission-gated Angular operational dashboard with queue, health, settings, masked references, retention and pause/resume state.
8. Validate .NET, Angular, Playwright, Python, security, V1.15 repeatability and fresh canonical SQL before completion.

