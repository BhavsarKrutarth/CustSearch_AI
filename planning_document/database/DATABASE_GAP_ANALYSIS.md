# Database Gap Analysis

Last updated: 2026-08-25

| Requirement | Existing live database | Gap / action |
|---|---|---|
| Phase 1-14 versioned schema | `V1.0.0` through `V1.13.0` present exactly once | No live ledger gap found; phase-specific regression remains required before final completion claims |
| Tenant/store-scoped operational reads | Existing Dapper stored procedures accept server-supplied tenant/store scope | Preserve the same pre-filtering pattern in Phase 15 reports |
| Platform and tenant report catalog | `TenantReport_Get` and `PlatformReport_Get` cover major operational domains | Expand factual master-plan variants/drill-downs before complete parity |
| Async export jobs | `ReportExportJobs` and `ReportExportEvents` installed | Implemented and live-verified |
| Worker-safe claiming | Atomic claim/progress/complete/fail plus requester revalidation installed | Implemented and live Worker-verified |
| Authorized download | Opaque reference/hash/size/expiry persisted; API rechecks requester | Implemented; API isolation matrix remains pending |
| CSV / Excel / PDF generation | Bounded Worker writers implemented | Unit tests and real CSV Worker test pass; E2E remains pending |
| Export progress events | Durable SQL event relay plus requester SignalR group implemented | End-to-end relay test remains pending |
| Retention/cleanup | Report artifact, consent/template and anonymous visitor retention are installed and Worker-scheduled | Deployment must set reviewed retention values and keep Worker running |
| Operational settings | Typed platform/tenant/store precedence and 33 defaults installed | No schema gap; production values require owner/privacy review |
| System health / heartbeat | SQL health summary and durable Worker heartbeat installed | Redis/Python external connectivity needs environment-specific configuration/test |
| SQL Server 2022 validation | Local engine is major version 17 with compatibility 160 | Re-run upgrade/fresh-install gates on actual SQL Server 2022 before production readiness |
| Phase 18 security persistence | V1.16.0 tables, indexes, permissions, safe settings and six procedures installed | Backend risk/POS engine, APIs, evidence access, Worker, SignalR and Angular remain in progress |
