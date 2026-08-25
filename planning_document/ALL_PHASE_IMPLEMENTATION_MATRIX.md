# CustSearch AI — All-Phase Implementation Matrix

Run: `CUSTSEARCH_SMOKE_20260825_001`

Evidence baseline: audit branch `audit/all-phases-database-smoke`, live database queried on
2026-08-25 through encrypted Windows Integrated Security.

Status is based on source, database objects, historical phase reports, and tests actually observed;
a PR title or live table alone is not completion evidence. “Project” abbreviations are Domain (D),
Contracts (C), Application (A), Infrastructure (I), API, Worker (W), Admin (NG), AI/Python (PY).

| Phase | Requirement | Project | Implementation file | API/route | Table | View | Function | Procedure | Index | Tests | Status | Gap |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Foundation, logging, SQL/Dapper, Angular/Python skeleton | D/C/A/I/API/W/NG/PY | solution projects, `Program.cs`, persistence/logging foundation | health/system | `DatabaseVersions`, `SystemSettings` | — | — | foundation bootstrap | version uniqueness | .NET/NG/PY/SQL historical + current regression | COMPLETE_AND_VERIFIED | SQL engine-specific rerun uses current environment limits. |
| 2 | Tenant ownership, auth, JWT/refresh lifecycle | D/C/A/I/API/NG | Authentication, JWT, current-user/tenant context | `/api/auth/*` | `Tenants`, `Users`, `RefreshTokens`, auth events | — | — | tenant/auth isolation operations | tenant/user/token indexes | auth HTTP, rotation, expiry, reuse and isolation | COMPLETE_AND_VERIFIED | None found in current regression. |
| 3 | RBAC, policies, guarded shells | D/C/A/I/API/NG | authorization policies, guards, navigation | authorization probes and guarded routes | `Roles`, `Permissions`, assignments | — | — | authorization lookups | role/permission ownership indexes | API policy, Angular guard, E2E denial | COMPLETE_AND_VERIFIED | None found. |
| 4 | Platform tenant/plans/quota/lifecycle/audit | D/C/A/I/API/NG | `PlatformTenantManagementService`, controller, platform pages | `/api/platform/*`, `/admin/tenants` | plans, subscriptions, quotas, usage, audit | — | — | platform tenant procedures | tenant/lifecycle indexes | service/API/Angular/Playwright | COMPLETE_AND_VERIFIED | None found. |
| 5 | Stores, tenant users, staff, categories, voice settings | D/C/A/I/API/NG | `TenantOperationsService`, controller, Phase 5 pages | `/api/tenant/*`, customer-admin setup routes | stores, staff, shifts, assignments, categories, voice config | — | — | tenant operations procedures | tenant/store ownership indexes | scope/quota/location/UI regression | COMPLETE_AND_VERIFIED | Phase implementation file status text is stale and must be reconciled. |
| 6 | Customers, anonymous visitors, smart profile/search | D/C/A/I/API/NG | `ShopperCustomerService`, repository, controller, customer/visitor pages | tenant customer/visitor routes | customers, visitors, store assignments | — | — | customer/visitor search | tenant/store/search indexes | CRUD, conversion, isolation, UI/E2E | COMPLETE_AND_VERIFIED | Phase file status text is stale. |
| 7 | Verified households, visits and co-visit parties | D/C/A/I/API/NG | `HouseholdsVisitsService`, controller, household/visit pages | `/api/tenant/households`, visits, visit-parties | households/members, visits, parties/members | — | — | household/visit/party searches | tenant/store/date indexes | relationship/privacy/isolation/UI | COMPLETE_AND_VERIFIED | Phase file status text is stale; co-visit/family separation remains explicit. |
| 8 | Products and factual retail billing | D/C/A/I/API/NG | `RetailBillingService`, repository/controller, retail pages | product/invoice/payment/report routes | products, retail invoices/items/payments/participants/attributions | — | — | retail search/detail/report procedures | store/invoice/product indexes | transaction, attribution, isolation, E2E | COMPLETE_AND_VERIFIED | None found. |
| 9 | Platform subscription billing, separate from retail | D/C/A/I/API/NG | `PlatformBillingService`, controller, billing pages | platform/tenant platform-billing routes | platform invoices/items/payments | — | — | platform billing list/detail | tenant/status/reference indexes | separation, lifecycle, authorization, E2E | COMPLETE_AND_VERIFIED | None found. |
| 10 | Preferences and configurable confirmed voice tagging | D/C/A/I/API/NG | `PreferencesVoiceService`, controller, voice pages | tenant preferences/voice routes | preference signals/scores, voice sessions, aliases | — | — | preference/voice operations | tenant/store/customer indexes | ambiguity, confirmation, privacy, UI | COMPLETE_AND_VERIFIED | Phase file status text is stale. |
| 11 | Durable alerts, SignalR, recovery/de-duplication | D/C/A/I/API/W/NG | alerts service/controller/hub/realtime client | `/api/tenant/alerts`, `/hubs/alerts` | alerts, notification outbox, realtime events | — | — | alert/recovery queries | tenant/store/cursor/status indexes | auth groups, dedupe, reconnect/E2E | COMPLETE_AND_VERIFIED | Multi-node Redis delivery remains an environment test. |
| 12 | Secure inbound/outbound integrations | D/C/A/I/API/W/NG | integration services/controllers/outbox worker | tenant integration + inbound routes | configurations, inbound events, outbox, delivery logs | — | — | integration queue/query operations | idempotency/status/tenant indexes | HMAC, replay, retry/dead-letter/isolation | COMPLETE_AND_VERIFIED | External provider credentials intentionally absent. |
| 13 | Cameras, zones, anonymous tracking, Demo Mode | D/C/A/I/API/NG/PY | camera tracking service/controllers; Python tracking/runtime | camera APIs, internal CCTV API, Python `/v1/cctv/*` | cameras, zones, tracks, handoffs/events | — | — | camera/tracking operations | store/camera/event indexes | API, 7 Python scenarios, Demo E2E | COMPLETE_AND_VERIFIED | Physical RTSP/production ONNX calibration is environment-dependent. |
| 14 | Consent-gated recognition and human review | D/C/A/I/API/NG | `RecognitionService`, controller, recognition page | `/api/tenant/recognition` | consents, templates, candidates | — | — | recognition operations | consent/tenant/store/status indexes | consent, withdrawal, encryption/review/E2E | COMPLETE_AND_VERIFIED | Production key-management and legal approval remain deployment controls. |
| 15 | Tenant/platform reports and async exports | C/A/I/API/W/NG | reports repository/service/controllers/export worker/page | tenant/platform report/export routes | report export jobs/events | — | — | bounded report/search procedures | requester/status/expiry indexes | accuracy, isolation, download, 42+ E2E history | COMPLETE_AND_VERIFIED | None found after export stream lock repair. |
| 16 | Operations, leases, health, retention, masking | D/C/A/I/API/W/NG | operational service/controller/health checks/workers/dashboard | `/api/platform/operations`, `/health/ready` | settings, secret refs, worker controls/leases/heartbeats, retention | — | — | `OperationalRetention_Run` | scope/key/lease/retention indexes | 104 unit, 225 integration, 78 NG, 49 E2E, 7 PY | BLOCKED | Local functional PASS; actual SQL Server 2022 and Redis multi-node checks remain blocked. |
| 17 | Full quality, API/deployment docs, IIS/WebSocket hardening | API/W/NG/PY/docs | partial workflows/config and this audit documentation | Swagger/health plus deployment routes | no new schema required by plan | — | — | — | — | local suites green; deployment smoke not executed | PARTIAL | Required catalogs/runbooks, IIS artifacts, production hardening and deployed smoke evidence incomplete. |
| 18 | Reviewable suspected unpaid-exit security workflow | required across all projects | no Phase 18 source branch/files in selected chain | no security APIs/Admin routes in source | nine `Security*` tables exist live only | — | — | six `Security*` procedures exist live only | live security indexes/constraints | no source tests in selected branch | PARTIAL | Critical code/live drift: V1.16 is live but versioned script/backend/UI/worker/Python/tests are absent from this branch. Do not recreate or downgrade DB. |

## Dependency chain and first unfinished work

`1 → 2 → … → 16 → 17 → 18` is the authoritative order. Phase 16 is locally green but retains
two external environment blockers. The first implementable repository gap is Phase 17 documentation,
deployment hardening and smoke automation. Phase 18 source must only begin after recovering or safely
reconstructing the live V1.16 schema into a repeat-safe versioned script and completing Phase 17 gates.

## Documentation drift found

- `phase_implementation/README.md` and several Phase 5–10 implementation files still show old
  in-progress/not-started states despite later validated source and process-tracker evidence.
- Root `README.md` was replaced during this audit with current Phase 16/17 setup and validation commands.
- The live database is ahead of the selected repository chain (`V1.16.0` versus canonical `V1.15.0`).
