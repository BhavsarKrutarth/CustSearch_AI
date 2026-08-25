# CustSearch AI — Phase Process Tracker

Last updated: 2026-08-25 (Asia/Kolkata)

## Execution Rules

- Work is performed one phase at a time.
- No implementation phase starts until the user explicitly approves that phase.
- Once a phase is approved, implementation starts automatically without a second start confirmation.
- A phase is marked `Completed` only after its scoped build/tests pass and evidence is recorded here.
- After completing a phase, update this tracker first, report results, and wait for approval for the next phase.
- Fix failures within the active phase before asking to move forward.
- Software and dependencies required by the approved phase may be installed; versions must be pinned and recorded.
- Before implementation work, fetch the Git remote and use only a safe fast-forward pull when possible. Never reset, discard, or overwrite local user changes to force a pull.
- Never use EF Core migrations, `Database.Migrate()`, or `EnsureCreated()`. Database changes use versioned SQL scripts only.
- Preserve unrelated user changes and secrets. Secrets must not be committed.
- Add a short plain-language description above every new class, service, policy, guard, directive, SQL object and non-obvious security/business block so the user can quickly understand its purpose; explain intent instead of repeating syntax.

## Status Legend

- `Completed`: implementation and phase verification passed.
- `In Progress`: explicitly approved and currently being implemented.
- `Awaiting Approval`: ready to begin after explicit user approval.
- `Not Started`: blocked by earlier phases.
- `Blocked`: cannot proceed; reason and required decision must be recorded.

## Repository Baseline

- Project root: `D:\Project\AdminCore\CustSearch_AI\CustSearch_AI`
- Planning source: `planning_document/CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`
- Branch observed: `master`
- The current working tree contains tracked deletions for the original Razor Pages starter solution/project and an untracked planning directory.
- Safety decision for Phase 1: treat those deletions as user-owned and do not restore them unless explicitly requested. Build the new architecture in the paths required by the final planning document.

## Environment Inventory

| Component | Observed state | Planned handling |
|---|---|---|
| .NET | User-local SDK 8.0.424 installed and pinned by `global.json`; system SDK 10.0.303 remains available | Complete; all .NET projects target `net8.0` |
| Node.js / npm | Node 24.14.0, npm 11.9.0 | Complete; compatible Angular 21 dependencies are project-local and lock-file pinned |
| Angular CLI | Project-local CLI 21.2.21; Angular core 21.2.20 | Complete; global `ng` is intentionally not required |
| Python | User-scope Python 3.12.10 and project `.venv` installed | Complete; FastAPI/OpenCV/ONNX/test dependencies are pinned |
| SQL Server | `MSSQLSERVER` running at product version 17.0.1000.7; `sqlcmd` available | Foundation scripts applied to `CustSearch_AI`; database compatibility level is pinned to SQL Server 2022 (`160`) and scripts avoid version-specific 17.x features |
| Docker | Docker command not found | Install only when the approved deployment/Redis workflow requires it |
| Git | 2.53.0.windows.2 | Available |

## Mandatory JWT / Session Scope

Authentication work in Phase 2 must include:

- Strongly typed and startup-validated `Jwt` options loaded from `appsettings.json`, with environment overrides for deployment.
- Dynamic non-secret settings for issuer, audience, access-token lifetime, refresh-token lifetime, and clock skew.
- Signing key supplied through environment/secret storage, never committed in production configuration.
- Short-lived JWT access tokens with correct `exp`, issuer, audience, and server-side validation.
- Rotating refresh tokens with hashed token storage, expiration, revocation, reuse detection, logout revocation, and audit trail.
- Secure refresh-token cookie settings (`HttpOnly`, `Secure`, explicit `SameSite`, scoped path) configurable by environment.
- Angular single-flight refresh on access-token expiry/401, proactive expiry awareness, request retry once, and clean logout/redirect when refresh expires or fails.
- `GET /api/auth/me` session expiry information derived from validated server configuration.
- Automated tests for valid, expired, malformed, wrong issuer/audience, revoked refresh, expired refresh, rotation, reuse, logout, and concurrent 401 behavior.

Proposed non-secret configuration shape (final names validated during implementation):

```json
{
  "Jwt": {
    "Issuer": "CustSearch.API",
    "Audience": "CustSearch.Admin",
    "AccessTokenLifetimeMinutes": 15,
    "RefreshTokenLifetimeDays": 7,
    "ClockSkewSeconds": 30,
    "RefreshCookie": {
      "Name": "custsearch_refresh",
      "Secure": true,
      "HttpOnly": true,
      "SameSite": "Strict",
      "Path": "/api/auth"
    }
  }
}
```

## Phase Tracker

Individual implementation files for all 18 phases are indexed in `phase_implementation/README.md`. Every phase file ends with a `Done Summary` that is filled with evidence only after that phase passes its completion gates.

| Phase | Scope | Required completion evidence | Status |
|---|---|---|---|
| 0 — Planning & Safety Baseline | Inspect final plan, repository state, required tooling, approval workflow, test strategy, and JWT-expiry requirement; create tracker | Tracker created; baseline and gates recorded | Completed |
| 1 — Foundation | Install/validate foundation tooling; create `.sln`, .NET projects/references, Angular workspace, Python skeleton, database folders/scripts, `DatabaseVersions`, DbContext and Dapper base; structured Serilog/correlation logging; README/env examples; record the Store location design | .NET restore/build/test; Angular install/lint/test/production build; safe SQL connectivity/script validation; structured-log smoke test; no EF migrations; repository structure review | Completed |
| 2 — Multi-Tenant Auth Foundation | Tenant model and ownership, tenant context, tenant-aware repositories/SPs, Platform/Tenant authentication, refresh-token flow, full dynamic JWT/session-expiry handling; shared custom Light/Dark/System design system and initial Platform/Customer Admin shells based on approved references | Auth unit/integration tests including expiry/rotation/revocation/reuse; cross-tenant denial tests; theme persistence/accessibility/UI tests; all Phase 1 gates remain green | Completed |
| 3 — Authorization & Admin Shells | Platform/tenant roles and permissions, Angular shells/navigation, auth/role/permission guards, typed clients, suspension/session rules | API policy tests, UI guard tests, unauthorized/forbidden E2E coverage, full builds | Completed |
| 4 — Platform Tenant Management | Platform dashboard, tenant CRUD, activate/suspend, plans, quotas, usage, detail summary, platform audit | CRUD/business/security/integration tests; Platform Admin E2E | Completed |
| 5 — Tenant Users, Stores, Shop Owner & Staff | Tenant users/roles, ShopOwner/TenantOwner, staff profiles/assignments/shifts/presence, stores/quotas/categories, canonical address/map coordinates/geofence/time-zone/location verification, dynamic store voice settings, Customer Admin dashboard base | Tenant/store isolation, location validation, permission and CRUD tests; Customer Admin E2E slice | Completed |
| 6 — Shopper Customers | Customer, anonymous visitor and search APIs; Angular customer/visitor features; smart profile | API/unit/integration/UI tests; tenant-isolation validation | Completed |
| 7 — Households & Visits | Households, members, visit parties and visits with verified relationship rules | Relationship/privacy rules and tenant isolation tests; feature E2E | Completed |
| 8 — Products & Retail Billing | Products, retail invoices/items/payments/participants, spend attribution and tenant reports | Transaction/idempotency/attribution/isolation tests; invoice E2E | Completed |
| 9 — Platform Billing | Platform invoices/items/payments, subscription plan and tenant billing pages | Platform-vs-retail separation tests; authorization and E2E | Completed |
| 10 — Preferences & Staff Voice Tagging | Personal/household preferences, manual tags, store-configured trigger parser, aliases, confirmation, recalculation, audit | Parser ambiguity/unknown-category/security tests; no hard-coded trigger; feature E2E | Completed |
| 11 — Alerts & Real-Time | Alerts/notifications, SignalR, authorized groups, Angular realtime client, reconnect/recovery/de-duplication, outbox, health metrics | Authenticated connect, unauthorized group denial, reconnect, duplicate-event and REST recovery tests | Completed |
| 12 — Integrations | Inbound APIs, integrations, HMAC webhooks, idempotency, retries and delivery logs | Signature/idempotency/retry/tenant-scope tests; integration E2E | Completed |
| 13 — Cameras, Python CCTV & Tracking | Cameras/zones, Python FastAPI/OpenCV/ONNX, person/customer/staff sessions, dwell/zone/proximity evidence, visit parties; Demo Mode | Python lint/tests, API integration tests, demo E2E without physical camera, optional approved RTSP smoke test | Completed |
| 14 — Consent-Based Recognition | Consent-gated enrollment/recognition and review workflow | Consent/withdrawal/data-minimization/security tests; recognition review E2E | In Progress |
| 15 — Reports & Async Exports | Platform/tenant, staff, conversion, dwell, voice, family and billing reports; async CSV/Excel/PDF exports and progress events | Accuracy/authorization/isolation tests; worker/export/WebSocket E2E | Not Started |
| 16 — Operational Platform | Audit, worker hardening, Redis/backplane readiness, settings, health, retention | Worker/retry/cache/health/retention tests; operational smoke tests | Local Pass / Environment Blocked |
| 17 — Full Quality & Deployment | Full .NET/Python/Angular/Playwright suites, Swagger, Postman, docs, IIS SPA rewrite/WebSockets, deployment hardening | Every command in final validation matrix passes; deployment smoke evidence; no unresolved critical/high findings | In Progress |

## Per-Phase Validation Matrix

Run what exists in the current phase, and keep all earlier checks green:

```text
dotnet restore
dotnet build --no-restore
dotnet test --no-build

cd src/CustSearch.Admin
npm ci
npm run lint
npm test -- --watch=false
npm run build -- --configuration production

cd tests/CustSearch.Admin.E2E
npm ci
npx playwright test

cd src/CustSearch.AI
python -m pip install -r requirements.txt
python -m pytest
```

Also validate, as applicable:

- Versioned SQL scripts are repeat-safe where required and never drop the database automatically.
- No EF migration artifacts or runtime migration calls exist.
- Tenant-owned reads/writes/procedures cannot cross tenant boundaries.
- Secrets, JWTs, passwords, camera credentials and face embeddings are absent from logs and committed files.
- Angular deep links, REST, SignalR/WebSocket reconnect and authorized group restoration work.
- Demo Mode keeps CI independent from physical CCTV/ONNX availability.

## Phase Completion Log

| Date | Phase | Result | Verification summary | Next gate |
|---|---|---|---|---|
| 2026-08-15 | 0 — Planning & Safety Baseline | Completed | Planning document and repo inspected; environment inventoried; phase/approval/testing/JWT scope recorded | Await explicit approval for Phase 1 |
| 2026-08-15 | 1 — Foundation | In Progress | User approved Phase 1; database scripts, structured logging, Store location planning and safe Git synchronization added to scope | Complete Phase 1 verification |
| 2026-08-15 | 1 — Foundation | Completed | 9 .NET projects built with 0 warnings/errors; 8 .NET tests, 3 Angular tests and 3 Python tests passed; Angular lint/production build and Python Ruff passed; npm/NuGet vulnerability scans clean; SQL scripts passed twice with one V1.0.0 row; API live/ready/info and correlation log smoke passed | Await explicit approval for Phase 2 |
| 2026-08-16 | 2 — Multi-Tenant Auth Foundation | In Progress | User approved Phase 2 and supplied Customer Admin light and Platform Admin dark UI references; dedicated Phase 2 log created | Complete tenant/auth/JWT/theme implementation and validation |
| 2026-08-16 | 2 — Multi-Tenant Auth Foundation | Completed | Tenant/auth SQL and isolation SP applied repeat-safely; atomic rotating refresh/JWT/cookie API completed; functional login plus Customer light/Platform dark/System UI completed; 29 .NET and 19 Angular tests passed; all build, format, SQL, Python and vulnerability gates green | Await explicit approval for Phase 3 |
| 2026-08-16 | 3 — Authorization & Admin Shells | In Progress | User explicitly approved Phase 3; dedicated sub-phase implementation plan created; Git remained ahead 0/behind 0 | Complete authorization, navigation, guards, typed clients and suspension/session verification |
| 2026-08-16 | 3 — Authorization & Admin Shells | Completed | Database-authoritative authorization/session rules, typed Angular guards/navigation and repeat-safe V1.2.0 SQL completed; 11 unit, 30 integration, 32 Angular, 2 Playwright and 3 Python tests passed; independent audit clear | Await explicit approval for Phase 4 |
| 2026-08-16 | 4 — Platform Tenant Management | In Progress | User explicitly approved Phase 4 and requested multiple agents; safe Git fetch remained ahead 0/behind 0; SQL/data, backend/API and Angular/E2E workstreams started | Complete tenant lifecycle, plans, quotas, usage, audit and Platform Admin validation |
| 2026-08-16 | 4 — Platform Tenant Management | Completed | Tenant lifecycle/plans/quotas/usage/audit APIs and Platform UI completed; SQL passed twice; 15 unit, 41 integration, 37 Angular, 5 Playwright and 3 Python tests passed; independent re-audit release-ready | Await explicit approval for Phase 5 |
| 2026-08-24 | 10 — Preferences & Staff Voice Tagging | Completed | Phase 10 Validate run 32740932609: Release build, 71 unit, 141 integration, 59 Angular, 32 Playwright and 3 Python tests passed; V1.9 upgrade twice, standalone installer twice plus verifier, and canonical fresh install passed on SQL Server 2022 | Merge the fully tested Phase 10 branch into AIMainBranch |
| 2026-08-24 | 11 — Alerts & Real-Time | In Progress | User approved Phase 11 after the merged Phase 10 baseline and all Phase 6–10 workflow results were re-verified green | Complete alert/outbox/SignalR/Angular implementation and full regression validation |
| 2026-08-24 | 11 — Alerts & Real-Time | Completed | Phase 11 Validate run 32746620273: Release build, 75 unit, 157 integration, 64 Angular, 34 Playwright and 3 Python tests passed; V1.10 upgrade twice, standalone installer twice plus verifier, and canonical fresh install passed on SQL Server 2022 | Keep draft PR unmerged until explicit merge approval |
| 2026-08-24 | 12 — Integrations | In Progress | Phase 11 PR 13, AIMain merge, V1.10 canonical and all Phase 6–11 checks re-verified green; safe Phase 12 branch created from merge commit b851c467 | Complete secure integrations implementation and full Phase 5–12 validation |
| 2026-08-24 | 12 — Integrations | Completed | 81 unit, 174 integration/API, 67 Angular, 36 Playwright and 3 Python tests green; Phase 6–12 workflows green; V1.11 upgrade/runner/verifier/canonical fresh install green | Draft PR 14 retained for review; no AIMain merge requested |
| 2026-08-25 | 13 — Cameras, Python CCTV & Tracking | Completed | Phase 13 Validate run 32798417105: 86 unit, 188 integration/API, 70 Angular, 38 Playwright and 7 Python tests green; Phase 6–12 workflows green; V1.12 upgrade/runner/verifier/canonical fresh install green | Create Phase 14 branch from the exact tested Phase 13 head |
| 2026-08-25 | 14 — Consent-Based Recognition | In Progress | Branch created from fully green Phase 13 head 9aa0b256; consent, encrypted derived-template, human-review and V1.13 implementation started | Complete Phase 5–14 validation |
| 2026-08-25 | 14 — Consent-Based Recognition | Completed | Phase 14 Validate run 32800628656: 90 unit, 204 integration/API, 73 Angular, 40 Playwright and 7 Python tests green; privacy/security gates and V1.13 upgrade/runner/verifier/canonical fresh install green | Draft PR 16 retained for review; no AIMain merge requested |
| 2026-08-25 | 15 — Reports & Async Exports | In Progress | Phase 14 final head b73704a2 and all Phase 6–14 workflows re-verified green; Phase 15 branch created from that exact head | Complete reports/exports implementation and Phase 5–15 validation |
| 2026-08-25 | 15 — Reports & Async Exports | Completed | Phase 15 Validate run 32807216952: Release build 0 warnings/errors, 97 unit, 213 integration/API, 76 Angular, 42 Playwright and 7 Python tests green; V1.14 upgrade/runner/verifier/fresh canonical green | Create Phase 16 branch from the exact tested Phase 15 head; keep draft PR 18 unmerged |
| 2026-08-25 | 16 — Operational Platform | In Progress | Branch created from exact final-green Phase 15 head baeee4d0; audit, worker coordination, settings, health, retention and controls implementation started | Complete Phase 5–16 validation |
| 2026-08-25 | 16 — Operational Platform | Local Pass / Environment Blocked | Audit run `CUSTSEARCH_SMOKE_20260825_001`: local suites, live runner/verifier/constraints, isolated canonical install and real-SQL auth/refresh smoke green after retry-strategy repair | Execute SQL Server 2022 and Redis multi-node gates; continue Phase 17 |
| 2026-08-25 | 17 — Full Quality & Deployment | In Progress | Current README/setup/catalogs/smoke data, project comments and proxy/HSTS hardening added; two-tenant SQL/API smoke proves cross-tenant denial | Complete full regression and IIS deployment smoke |

## Phase 1 Completion Evidence

- Git: fetched `origin`; local `master` and `origin/master` were already aligned (ahead 0, behind 0), so no pull mutation was required. Existing user-owned Razor starter deletions were preserved.
- Folder structure: separate Domain, Contracts, Application, Infrastructure, Integrations, API, Worker, Angular and Python projects under `src`; separate Unit, Integration, AI and Admin E2E test folders under `tests`.
- .NET: `dotnet restore`, build and tests passed; 9 projects, 0 warnings, 0 errors, 8 tests passed; format verification clean; NuGet reported no vulnerable direct/transitive packages.
- Angular: clean `npm ci`, lint, 3 unit tests and production build passed; lazy dashboard chunk generated; `npm audit` reported 0 vulnerabilities.
- Python: Python 3.12.10 installed; pinned FastAPI/OpenCV/ONNX environment created; Ruff passed and 3 tests passed in Demo Mode.
- SQL: server connectivity confirmed; database compatibility pinned to SQL Server 2022 level 160; safe scripts created/applied for database, `DatabaseVersions`, unique version index and V1.0.0 record; repeat runs succeeded and left exactly one V1.0.0 row.
- Logging: Serilog rolling API/Worker logs and Python JSON logging configured; correlation ID validation/propagation tested; API `/health/live`, `/health/ready` and `/api/system/info` returned HTTP 200.
- Rules: no EF migration artifacts, `Database.Migrate()`, `EnsureCreated()` or `Add-Migration` implementation calls found.
- Planning: Store location now covers canonical address, optional coordinates/geofence, source, verification, tenant permission/audit and Store time zone rules.

## Phase 2 Completion Evidence

- Git safety: fetched `origin`; local `master` remained ahead 0 and behind 0, so no pull mutation was required and all user-owned changes were preserved.
- Database: `run-phase2.ps1` includes the foundation chain and repeat-safe tenant/auth tables, indexes, tenant-isolated stored procedure and V1.1.0 record. Final verification: compatibility 160, four Phase 2 tables, one stored procedure, and exactly one row each for V1.0.0/V1.1.0.
- Tenant/auth: Platform and tenant user boundaries, tenant predicates, password hashing, authentication audit events, hashed rotating refresh tokens, expiry/revocation/logout/reuse-family invalidation and atomic concurrent refresh consumption are implemented.
- JWT/session: issuer, audience, access lifetime, refresh lifetime, clock skew and cookie policy are strongly typed and startup validated. Production signing key is blank in committed settings and supplied through `Jwt__SigningKey`; `/me` returns expiry from the validated `exp` claim.
- API verification: restore and nine-project build passed with 0 warnings/errors; 8 unit and 21 integration tests passed. Real TestServer coverage includes login, refresh rotation, cookie flags/deletion, logout, `/me`, malformed/expired/wrong-issuer/wrong-audience JWTs, tenant isolation and concurrent refresh.
- Angular: functional login, in-memory-only access token, proactive expiry detection, single-flight refresh for concurrent 401s, exactly-one retry, server logout and safe failure redirect are implemented. Auth endpoints bypass refresh interception.
- UI/design system: custom semantic Light/Dark/System tokens, responsive reusable shell/cards/tables/badges, Customer Admin light default and Platform Admin dark default match the approved directions while persisted user preference remains authoritative.
- Frontend verification: Angular lint, 19 tests and production build passed with no warnings; npm audit reported 0 vulnerabilities. NuGet found no vulnerable direct/transitive packages.
- Earlier gates: Python Ruff and 3 tests passed; no runtime EF migration/schema-creation calls or migration artifacts exist; structured logging and secret exclusions remain intact.
- Environment: .NET SDK 8.0.424 is pinned and its user-local installation directory was added to the persistent User PATH.

## Phase 3 Completion Evidence

- Authorization model: four scoped authorization tables, seven indexes, two ownership/scope triggers, one tenant-safe lookup procedure, 82 shared permissions and least-privilege platform/tenant role templates are implemented.
- SQL: `run-phase3.ps1` passed twice; compatibility is 160; `RefreshTokens.IssuedSecurityStamp` is non-null `NVARCHAR(64)`; `V1.2.0` exists exactly once; negative ownership/scope probes were rejected.
- API security: dynamic permission and scope policies, JSON 401/403 handling, database-refreshed authorization claims and explicit `PlatformSupport.AccessTenant` cross-tenant access are enforced.
- Session rules: disabled users, suspended/inactive tenants and changed security stamps reject access/direct refresh and revoke sessions; rejection audits carry correlation ID/IP and suppress duplicate flooding.
- Angular: typed session/API clients, single-flight bootstrap, auth/role/permission guards, permission directive, filtered navigation, Access Denied and safe 401/403 flows are implemented without trusting a client-selected TenantId.
- Verification: .NET build passed with 0 warnings/errors; 11 unit and 30 integration tests passed; .NET format clean; Angular lint, 32 tests and production build passed; Playwright 2/2; Python Ruff and 3 tests passed.
- Supply chain and audit: npm and NuGet vulnerability scans found no known vulnerabilities; independent Phase 3 security/completeness audit found no remaining blocker.
- Detailed Phase 3 log: `PHASE_03_IMPLEMENTATION_LOG.md`.

## Phase 4 Completion Evidence

- Database: repeat-safe Phase 4 runner passed twice. Live totals are 14 user tables and 5 procedures; Phase 4 contributed 5 tables, 10 named indexes, 3 procedures, 2 triggers, one `TRIAL` plan and exactly one `V1.3.0` row.
- Provisioning: tenant creation transactionally creates exactly eight tenant-scoped roles with least-privilege grants and zero platform-scope grants; failure rolls back the tenant and roles.
- Platform API: dashboard, tenant list/detail/create/edit/activate/suspend, operational summary, usage, audit, subscription plan CRUD and audited subscription/quota assignment enforce platform scope plus exact permissions.
- Lifecycle/security: tenant code is server-generated and immutable; Base64 concurrency versions reject stale writes with 409; suspension revokes refresh sessions without reactivating/deactivating user accounts; audit data is allowlisted and correlated.
- Subscription integrity: replacement closes the prior current row transactionally, preserves history and prevents competing current records. Live rollback-only SQL evidence produced 3 history rows, 1 current and 2 cancelled, with 0 probe tenants persisted.
- Billing/quotas: inactive plans, invalid periods/enums, expired overrides and limits below authoritative usage are rejected. MRR includes only effective Active/PastDue subscriptions and normalizes annual value to monthly.
- Platform Admin UI: dynamic dashboard and guarded tenant directory/create/edit/detail/lifecycle/summary/usage/audit/plan workflows use typed same-origin clients, semantic dark/light/system styling and permission-aware controls.
- Verification: .NET build 0 warnings/errors; 15 unit and 41 integration tests; Angular clean install/lint, 37 tests and production build; Playwright 5/5; Python Ruff and 3 tests; npm/NuGet audits clean.
- Audit and log: independent re-audit declared Phase 4 release-ready with no remaining blocker. Detailed evidence is in `PHASE_04_IMPLEMENTATION_LOG.md`; Phase 5 is `Awaiting Approval` and has not started.
