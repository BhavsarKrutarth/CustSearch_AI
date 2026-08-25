# Code Change Log

| Phase | File | Change | Reason | Testing |
|---:|---|---|---|---|
| Baseline | `planning_document/database/*` | Added live database inventory, gap analysis, and phase map | Mandatory baseline | Compared with live catalog/ledger |
| Baseline | `planning_document/implementation_tracking/*` | Added persistent tracker/handoff set | Session continuation | Updated with executed evidence |
| 15 | `src/CustSearch.Application/ReportsExports/*` | Added report/export contracts, status, filters, request context | Stable application boundary | Solution build PASS |
| 15 | `src/CustSearch.Infrastructure/ReportsExports/*` | Added Dapper repository/service, Worker processor, durable dispatcher, artifact writers | Implement report/export workflow | Unit, integration, live Worker PASS |
| 15 | `src/CustSearch.API/*` | Added tenant/platform APIs, exception filter, authenticated hub/publisher/relay and JWT hub path | Secure HTTP/realtime boundary | Build PASS; E2E pending |
| 15 | `src/CustSearch.Worker/*` | Added export hosted service and fail-closed worker-only context/metrics | Process queued exports and allow Worker startup | Real Worker export PASS |
| 15 | `src/CustSearch.Admin/src/app/features/reports/*` | Added report page/API/realtime services/tests | Tenant/platform report UI | 76/76, lint, build PASS |
| 15 | `tests/CustSearch.UnitTests/PhaseFifteenReportArtifactTests.cs` | Added CSV/XLSX/PDF/formula/path tests | Artifact security and validity | PASS |
| 15 | `tests/CustSearch.IntegrationTests/PhaseFifteenReportsApiAuthorizationTests.cs` | Added report API status, permission, scope, and TenantId-boundary tests | Authorization evidence | 7/7 and full 214/214 PASS |
| 15 | `tests/CustSearch.IntegrationTests/PhaseFifteenReportSignalRTests.cs` | Added requester-only SignalR publisher/group tests | Prevent cross-user progress delivery | 3/3 and full 214/214 PASS |
| 15 | `tests/CustSearch.Admin.E2E/tests/phase15-reports-exports.spec.ts` | Added tenant preview/queue/progress/download and permission-guard workflow | Browser acceptance coverage | 2/2 focused; 47/47 full PASS |
| 15 | `database/CustSearchAi.sql`, `database/build-phase15-canonical.ps1` | Persisted exact V1.14.0 canonical block and reproducible append guard | Fresh-install parity | Disposable fresh install PASS |
| Regression | Four Phase 11-14 integration test files | Disabled SQLite pooling in temp-file connection strings | Prevent pooled handles blocking deterministic teardown | Integration 204/204 PASS |
| 16 | `src/CustSearch.Application/Operations/*`, `src/CustSearch.Infrastructure/Operations/*` | Added operations contracts, Dapper repository/service and retention maintenance | Settings/audit/health/Worker data path | Build/unit/integration/live SQL PASS |
| 16 | `src/CustSearch.API/Controllers/OperationsController.cs`, `Operations/*`, `Program.cs` | Added scoped operations APIs, Redis cache/backplane readiness, health probe and runtime SignalR metrics | Protected operational administration | API auth 5/5; real live/ready/auth 200/200/401 |
| 16 | `src/CustSearch.Worker/*` | Added durable heartbeat, report artifact cleanup and privacy retention hosted services | Worker hardening and retention | Real Worker start/heartbeat/stop and cleanup PASS |
| 16 | `src/CustSearch.Admin/src/app/features/operations/*` and routes/navigation/catalog | Added platform health/settings/audit and tenant settings/audit UI | Complete operational admin surface | Angular 78/78, lint/build, Playwright 47/47 PASS |
| 16 | Phase 15 artifact/API test files plus Phase 16 tests | Added idempotent deletion, scope/safety and API authorization coverage; serialized API hosts | Prevent traversal, unsafe override, cross-scope access and flaky host concurrency | Unit 98/98; integration 219/219 PASS |
| 17 | `Invoke-QualityGates.ps1`, `README.md` | Added one fail-fast SQL/.NET/Python/Angular/Playwright quality entry point and current commands | Reproducible release validation | Integrated local gate PASS |
| 17 | API OpenAPI files and `Program.cs` | Added JWT Bearer scheme and per-operation authorization metadata | Accurate authenticated API discovery | Build PASS; live Swagger/401 PASS |
| 17 | `postman/*`, `planning_document/deployment/*`, Angular `public/web.config` | Added secret-free API examples, configuration/runbooks and IIS SPA/WebSocket rules | Deployment handoff | JSON/XML parse and production-copy PASS |
| 17 | E2E config/server/Phase 11 spec and package lock | Added real WebSocket disconnect/reconnect recovery test; patched Playwright and `ws` advisories | Realtime resilience and supply-chain hardening | 48/48; npm audit 0 |
| 18 | `database/09_Upgrade/V1.16.0_Phase18_RetailSecurity.sql` and runner/verifier/test/canonical builder | Added additive tenant/store security foundation, replay protection and human transition constraints | Start Phase 18 from verified database contracts | Live rerun/verifier/rollback and canonical fresh install PASS |
| 18 | `planning_document/PHASE_18_IMPLEMENTATION_PLAN.md` | Extracted security, privacy, dependency and delivery requirements | Persistent implementation direction | Compared with authoritative Phase 18/addendum |
