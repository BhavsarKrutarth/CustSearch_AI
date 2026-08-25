# Database Phase Mapping

Last updated: 2026-08-25

| Phase | Main database objects | Backend / frontend | Validation state |
|---:|---|---|---|
| 1-4 | Version ledger, identity/RBAC, tenants/plans/usage/audit | Foundation, auth, authorization, platform tenant admin | Historical green evidence; live objects verified |
| 5 | Stores, staff, assignments, categories, voice settings | Tenant operations and Customer Admin base | Historical green evidence; live objects verified |
| 6 | Customers, assignments, anonymous visitors; search SPs | Customer/visitor API and Angular | Historical green evidence; live objects verified |
| 7 | Households, members, visit parties, visits; search/detail SPs | Household/visit API and Angular | Historical green evidence; live objects verified |
| 8 | Products and retail invoice/payment/attribution tables; retail report SPs | Retail API and Angular | Historical green evidence; live objects verified |
| 9 | Platform billing tables and SPs | Platform/tenant billing UI | Historical green evidence; live objects verified |
| 10 | Preference/voice tables and SPs | Preference/voice API and UI | Historical green evidence; live objects verified |
| 11 | Alerts, realtime events, notification outbox and SPs | REST, SignalR, notification center | Historical green evidence; live objects verified |
| 12 | Integration config/inbound/outbox/log tables and SPs | Integration APIs, worker and UI | Historical green evidence; live objects verified |
| 13 | Cameras/zones/tracks/handoffs/events and SPs | CCTV API, Python and Angular | Historical green evidence; live objects verified |
| 14 | Consents/templates/candidates and SPs | Recognition API/Python/UI | Historical green evidence; live objects verified |
| 15 | `ReportExportJobs`, `ReportExportEvents`, 15 report/export/audit procedures and queue/requester/event indexes | Dapper report APIs, Worker exporters, SignalR relay, Angular report/export center | Complete; live/canonical/Worker/API/E2E validation green |
| 16 | `SystemSettings`, `WorkerHeartbeats`, settings/audit/health/retention procedures and operational indexes | Dapper operations APIs, Worker heartbeat/retention, optional Redis/backplane, Angular operations UI | Complete; live SQL, Worker, API, .NET, Angular and E2E validation green |
| 17 | No mandatory new business schema; deployment verification artifacts | Full quality command, OpenAPI/Postman, IIS/runbooks and reconnect E2E | Implemented; SQL Server 2022 and multi-node Redis gates pending |
| 18 | Nine security tables, six procedures, permissions/settings and enforced tenant/store relationships | Security API/rules engine/worker/Python/Angular in progress | Database runner/verifier/rollback/canonical green; application pending |
