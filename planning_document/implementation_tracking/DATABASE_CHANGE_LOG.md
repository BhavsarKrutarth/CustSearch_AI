# Database Change Log

| Date | Phase | Object type | Object name | Change | Reason | Test performed | Result |
|---|---:|---|---|---|---|---|---|
| 2026-08-25 | Baseline | Inventory | `CustSearch_AI` | Recorded live schema, keys, indexes, procedures, ledger, and seed/security counts | Establish pre-change evidence | Integrated-security catalog queries | PASS |
| 2026-08-25 | 15 | Tables | `ReportExportJobs`, `ReportExportEvents` | Added tenant/platform export lifecycle and durable requester events | Async exports and REST/SignalR recovery | Double-run, verifier, rollback lifecycle | PASS |
| 2026-08-25 | 15 | Stored procedures | 15 Phase 15 procedures | Added queue/claim/progress/complete/fail/expire, requester revalidation, audit, event relay, tenant/platform reports | Dapper-only bounded report workflow | Verifier, rollback SQL tests, real Worker | PASS |
| 2026-08-25 | 15 | Security fix | `ReportExportRequesterScope_Get` | Made platform null-TenantId identity comparison NULL-safe | Real Worker rejected valid platform requester | Repeated live Worker export | PASS |
| 2026-08-25 | 15 | Version ledger | `V1.14.0` | Added exact single Phase 15 version | Upgrade traceability | Repeated runner/verifier | PASS |
| 2026-08-25 | 15 | Canonical | `database/CustSearchAi.sql` | Appended tested V1.14.0 block | Fresh-install parity | Disposable DB: 64 tables, 62 SPs, 15 versions, DBCC constraints | PASS |
| 2026-08-25 | 16 | Tables/indexes | `SystemSettings`, `WorkerHeartbeats` | Added typed scoped settings, precedence indexes and durable Worker status | Operational configuration and health | Repeated live runner, DBCC, rollback SQL | PASS |
| 2026-08-25 | 16 | Stored procedures | Seven new procedures | Added settings, audit paging, heartbeat, health, privacy retention and artifact cleanup acknowledgement | Dapper-only operations and bounded Worker cleanup | Live verifier/rollback suite and real Worker | PASS |
| 2026-08-25 | 16 | Stored procedure hardening | `ReportExportJob_Expire` | Retains opaque reference until idempotent file deletion is acknowledged and emits durable expiry event | Make cleanup retryable after file-system failure | Real Worker expired-job test | PASS |
| 2026-08-25 | 16 | Seed/security | 33 settings; 3 platform permissions | Added safe defaults, explicit false household face-similarity auto-link, settings/health grants | Planned operational policy and RBAC | Count, precedence and negative safety tests | PASS |
| 2026-08-25 | 16 | Version ledger | `V1.15.0` | Added exact single Phase 16 version | Upgrade traceability | Repeated runner/verifier | PASS |
| 2026-08-25 | 18 | Tables/indexes | Nine `Security*` tables; camera-zone composite key | Added tenant/store-enforced rules, signed ingestion receipts, observations, incidents, items, opaque evidence, immutable actions, deliveries and payment correlations | Reviewable unpaid-exit foundation without watchlist | Repeated runner, verifier, DBCC, canonical fresh install | PASS |
| 2026-08-25 | 18 | Stored procedures | Six `Security*` procedures | Added replay-safe observation ingest, versioned rules, bounded scoped incident reads and validated human state transitions | Dapper-only business data path and cross-tenant defense | Rollback valid/replay/wrong-store tests | PASS |
| 2026-08-25 | 18 | Seed/security | 13 permissions; 14 safe settings | Added granular RBAC and shadow-mode-disabled defaults | Human review/privacy-safe rollout | Verifier and live catalog checks | PASS |
| 2026-08-25 | 18 | Version/canonical | `V1.16.0`, `CustSearchAi.sql` | Added exact ledger row and synchronized canonical schema | Upgrade/fresh-install parity | Disposable 75-table/75-procedure/17-version install | PASS |
