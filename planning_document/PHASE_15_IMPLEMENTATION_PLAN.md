# Phase 15 — Reports & Async Exports Implementation Plan

Status: In Progress

## Requirements

- Separate platform-wide and tenant-scoped operational reports.
- Tenant/store/date/status/category/camera and domain-specific filters where applicable.
- Staff performance/conversion, dwell/journey, voice audit, household/visit party, retail, alerts, camera/recognition, webhook/integration, billing and audit reporting.
- Async CSV, Excel and PDF exports with queued/processing/completed/failed/expired lifecycle.
- Worker generation, durable progress, REST recovery and SignalR readiness notification.
- Authorized temporary download with opaque storage reference and expiry.

## Database

- `ReportExportJobs` with tenant-null platform scope, requester, report type, normalized filter JSON, format, status, progress, artifact metadata, error, timestamps and row version.
- Queue/requester/expiry indexes and tenant/requester foreign keys.
- Atomic claim/progress/complete/fail procedures with `SET NOCOUNT ON`, `SET XACT_ABORT ON`, validation and guarded state transitions.
- Tenant/platform report procedures that filter tenant/store scope before aggregation.

## Backend / Worker

- Application contracts for report catalog, report query and export lifecycle.
- Dapper-only repository/service for Phase 15 reads/writes.
- Tenant and platform controllers with exact permissions and no browser-supplied TenantId.
- Worker processor with bounded batches, safe artifact paths, hashing and idempotent state transitions.
- CSV, valid Excel workbook and PDF writers.
- Durable progress/readiness events connected to the authenticated SignalR boundary; REST remains authoritative.

## Frontend

- Tenant `/customer-admin/reports` and platform report routes.
- Typed API service, catalog/filter UI, bounded preview and export queue/history.
- Progress updates with REST refresh/recovery and secure download action.
- Loading, validation, error and empty states.

## Security

- Server-derived tenant and authoritative store scope.
- Platform cross-tenant use requires platform report permission and audit.
- Requester-scoped job visibility/download unless a specifically authorized administrative path is implemented.
- Opaque storage keys, strict report/format allowlists, bounded filters and no public file URLs.
- No arbitrary SQL/object names, file paths or formulas accepted from browser input.
- CSV/Excel formula injection neutralization and PDF/content escaping.

## Tests / acceptance

- SQL double-run, exact `V1.14.0` ledger row, constraints/indexes/SP structure and state transition tests.
- Report accuracy, date boundaries, no-data, paging/row limits, tenant/store isolation and wrong-requester download tests.
- Worker success/failure/retry, artifact hash/expiry and path traversal tests.
- API 200/400/401/403/404/409 behavior.
- Angular lint/unit/build and Playwright export/progress/download flow.
- Full .NET/Python/Angular regression plus canonical fresh-install verification.

