# Phase Status

## Phase 1-14

Planning files: `planning_document/phase_implementation/PHASE_01_*` through `PHASE_14_*`

Status: COMPLETE based on merged implementation, recorded green validation, and live database ledger/object verification.

Known issue: some older phase summaries retain intermediate statuses. `PROCESS_TRACKER.md`, merged Git history, test reports, and this tracker are authoritative.

## Phase 15

Phase: 15 - Reports & Async Exports

Planning file: `planning_document/phase_implementation/PHASE_15_REPORTS_EXPORTS.md`

Status: COMPLETE

Completed:
- Live repeat-safe V1.14.0 upgrade with export jobs/events, 15 procedures, indexes, constraints, audit evidence, and ledger row.
- Dapper/stored-procedure report repository, tenant/platform scope service, APIs, Worker processor, durable SignalR relay, and protected artifact store.
- CSV, XLSX, and PDF generation with opaque keys, hashing, formula neutralization, bounded queries, and requester-only download.
- Angular tenant/platform report centers with preview, filtering, queue/history, polling recovery, SignalR refresh, and authenticated downloads.
- Server-derived TenantId/store scope; Worker revalidates active requester permissions and current store scope.

Pending:
- None for Phase 15. High-value/VIP classification is a Phase 18 dependency and is not fabricated here.

Database:
- `ReportExportJobs`, `ReportExportEvents`, V1.14.0, 15 Phase 15 procedures installed in actual `CustSearch_AI` database.
- Upgrade double-run, verifier, and rollback-only SQL test pass.

Backend:
- Implemented report catalog/query/queue/list/download, Worker generation, event dispatch, audit, and artifact storage.

Frontend:
- Implemented `/customer-admin/reports` and `/admin/reports` routes and navigation.

Testing:
- .NET build PASS; unit 94/94; integration 214/214; Angular 76/76; lint PASS; production build PASS; Playwright 47/47; Python 7/7 and Ruff PASS.
- Real Worker CSV completion/hash/header test PASS; test records/artifact removed.

Known issues:
- Actual host is SQL engine version 17 with compatibility 160, not SQL Server 2022.
- Angular has a pre-existing 61-byte `admin-shell.scss` budget warning.

Files changed:
- See `CODE_CHANGE_LOG.md` and Git status.

Stored procedures changed:
- `ReportExportJob_Create/Get/List/Claim/Progress/Complete/Fail/Expire`
- `ReportExportRequesterScope_Get`, `ReportAudit_Write`
- `ReportExportEvent_Claim/Complete/Fail`
- `TenantReport_Get`, `PlatformReport_Get`

Next exact task:
- Begin Phase 16 operational hardening audit from the verified Phase 15 baseline.

## Phase 16

Phase: 16 - Operational Platform

Planning file: `planning_document/phase_implementation/PHASE_16_OPERATIONS.md`

Status: COMPLETE

Completed:
- Typed platform/tenant/store settings with effective precedence and audited changes.
- Dedicated bounded audit search, Worker heartbeat, health summary, Redis/backplane readiness and retention workers.
- Report artifact cleanup is retryable and idempotent; expired consent disables and cryptographically erases active templates; anonymous visitor cleanup respects configured scope retention.
- Platform and tenant Angular operational pages and permission navigation.

Pending:
- None for Phase 16. A real Redis cluster/backplane and SQL Server 2022 deployment remain environment-specific Phase 17 production gates.

Database:
- Live `V1.15.0`; 66 tables, 69 procedures, 16 version rows, 33 platform defaults, 0 export test rows.

Backend:
- Dapper operations repository/service, protected controllers, Redis health/backplane configuration, runtime metrics, Worker heartbeat and two retention paths.

Frontend:
- `/admin/system-health`, `/admin/settings`, `/admin/audit-logs`, `/customer-admin/settings`, `/customer-admin/audit-logs`.

Testing:
- SQL runner/verifier/rollback suite PASS; DBCC constraints clean.
- .NET build PASS; unit 98/98; integration 219/219.
- Angular lint PASS; 78/78 tests; production build PASS; Playwright 47/47.
- Real API live/ready/protected health returned 200/200/401; real Worker heartbeat and artifact cleanup PASS.

Known issues:
- Local engine is SQL major version 17, not requested SQL Server 2022.
- No Redis server is configured locally, so enabled Redis/backplane connectivity needs a deployment-environment test.
- Existing Angular SCSS budget warning is 61 bytes.

Files changed:
- See `CODE_CHANGE_LOG.md` and Git status.

Stored procedures changed:
- `SystemSetting_List`, `SystemSetting_Upsert`, `WorkerHeartbeat_Upsert`, `AuditLog_Search`, `SystemHealth_Get`, `OperationalRetention_Run`.
- `ReportExportJob_Expire` hardened; `ReportExportJob_ArtifactDeleted` added.

Next exact task:
- Begin Phase 17 quality/deployment requirements audit and close reproducible deployment/CI/security-test gaps.

## Phase 17

Phase: 17 - Full Quality & Deployment

Planning file: `planning_document/phase_implementation/PHASE_17_QUALITY_DEPLOYMENT.md`

Status: IMPLEMENTED — TESTING PENDING

Completed:
- Fail-fast repository quality command and guaranteed-cleanup canonical fresh-install verifier.
- JWT-aware Swagger, secret-free Postman collection/environment, IIS SPA/WebSocket config and production runbooks.
- Deterministic forced SignalR reconnect/cursor-recovery E2E.
- Patched E2E dependencies; both npm projects audit with zero vulnerabilities.

Pending:
- Execute database validation on actual SQL Server 2022.
- Exercise enabled Redis/backplane across multiple API nodes in the deployment environment.

Testing:
- Integrated local quality gate PASS: SQL 66/69/16 plus DBCC/cleanup; .NET 98+219; Python 7; Angular 78; Playwright 48.
- Live Swagger Bearer metadata and anonymous 401 boundary PASS.

Known issues:
- Existing Angular SCSS budget warning is 61 bytes.
- Local SQL/Redis environment cannot satisfy the two production deployment gates above.

Next exact task:
- Continue Phase 18 schema/rule-engine implementation from this locally validated baseline while retaining the Phase 17 deployment blockers.

## Phase 18

Phase: 18 - Reviewable Retail Security / Suspected Unpaid Exit

Planning file: `planning_document/phase_implementation/PHASE_18_RETAIL_SECURITY_THEFT_DETECTION.md` and authoritative security addendum

Status: IN PROGRESS

Completed:
- Requirement/dependency/safety extraction; confirmed no pre-existing incident implementation and excluded unapproved watchlist storage.
- Live repeat-safe V1.16.0 schema with nine scoped tables, six procedures, 13 permissions and 14 disabled/shadow-safe settings.
- Signed-ingestion persistence with timestamp/body/nonce/idempotency controls, active camera ownership and strict observation validation.
- Human transition constraints require a reason for confirmed loss/false positive and preserve immutable action/audit history.
- Canonical V1.16.0 synchronization and fresh-install validation.

Pending:
- Domain/application risk engine, paid POS correlation and candidate creation.
- HMAC service authentication/rate limits, user/internal APIs, evidence short-lived access and audit.
- SignalR/outbox, Worker maintenance, Angular security pages, Python scenarios and full Phase 18 tests.

Database:
- Live V1.16.0: 75 tables, 75 procedures, 17 version rows; security tables currently contain zero records.

Testing:
- Repeated runner/verifier, DBCC, rollback valid/replay/wrong-store tests and disposable canonical fresh install PASS.

Known issues:
- Phase 17 SQL Server 2022 and Redis deployment gates remain pending.

Next exact task:
- Define Phase 18 domain/application enums/contracts and implement the server-side risk/state engine plus Dapper repository against the installed procedures.
