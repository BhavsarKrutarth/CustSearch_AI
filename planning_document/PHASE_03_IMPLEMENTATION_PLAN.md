# Phase 3 — Authorization & Admin Shells Implementation Plan

Created: 2026-08-16 (Asia/Calcutta)

Approval: Approved by user

Status: Completed

## Objective

Implement backend-authoritative platform/tenant roles and granular permissions, tenant/session enforcement, typed Angular session/API infrastructure, authenticated Platform and Customer Admin navigation, route guards, permission-aware UI helpers, and complete unauthorized/forbidden regression coverage.

Phase 3 builds authorization infrastructure and shells. Tenant CRUD belongs to Phase 4; tenant users, stores, ShopOwner/staff and shop location implementation remain Phase 5.

## Execution Rules

- Main phases remain approval-gated. Phase 3 started automatically after explicit approval.
- Sub-phases below are executed in dependency order; independent backend, Angular and SQL/test slices may run in parallel.
- API authorization is authoritative. Angular guards and hidden navigation are user-experience controls only.
- Tenant scope comes only from validated server identity/session; ordinary tenant requests cannot choose an arbitrary TenantId.
- No EF migrations, `Database.Migrate()` or `EnsureCreated()` in runtime code. Schema changes use repeat-safe versioned SQL.
- Preserve existing user changes and never commit secrets, access tokens, refresh tokens or passwords.
- Every new Phase 3 class, service, policy, guard, directive, SQL object and non-obvious security block must have a short plain-language description immediately above it so its purpose is easy to understand. Comments must explain intent, not repeat syntax.

## Sub-Phase Plan

| Sub-phase | Scope | Required evidence | Status |
|---|---|---|---|
| 3.0 — Safety & Contracts | Safe Git fetch/divergence check; permission catalog; role/scope/session contracts; update tracker and Phase 3 log | Origin state recorded; contracts compile; no user changes overwritten | Completed |
| 3.1 — Authorization Data Model & SQL | Roles, Permissions, UserRoles and RolePermissions with platform/tenant ownership rules; repeat-safe SQL tables/indexes/seeds/version ledger; tenant-safe lookup procedures where useful | SQL runner passes twice; role/permission uniqueness and tenant constraints verified | Completed |
| 3.2 — Backend Policy Engine | Dynamic permission policies/handler, role policies where genuinely required, current-user permission/store claims, authoritative API enforcement and safe 401/403 envelopes | Unit/integration tests for allowed, unauthenticated, forbidden and cross-scope access | Completed |
| 3.3 — Suspension & Session Rules | Disabled user, suspended/inactive tenant and changed security-stamp session rejection; refresh-family/session revocation behavior; audit-safe failures | Existing access `/me`, refresh and policy tests prove suspension/revocation behavior | Completed |
| 3.4 — Angular Session & Typed Clients | Load `/api/auth/me`; typed auth/current-user/query/page/envelope models; tenant-aware same-origin API base; authenticated session bootstrap and 401/403 handling | Typed-client and session bootstrap tests; no TenantId trusted from storage/query string | Completed |
| 3.5 — Admin Navigation & UI Authorization | Platform and Customer navigation configs with permission metadata; auth/role/permission guards; `hasPermission` helper/directive; Access Denied page; responsive shell state | Guard, navigation filtering, directive and 403-page tests; API remains authoritative | Completed |
| 3.6 — Security & Quality Closure | Full .NET/Angular/Python/SQL regression, endpoint smoke tests, format/vulnerability/secret checks and focused security review | All Phase 3 and earlier gates green; evidence recorded in tracker/log | Completed |

## Initial Permission Catalog

Platform permissions begin with tenant lifecycle/usage, platform billing/subscriptions/reports/audit and explicit support access. Tenant permissions begin with tenant dashboard/users/stores/billing/reports/audit and the operational permission names defined in section 59 of the final planning document. Permission names are shared exactly between API and Angular.

Default role families:

- Platform: `PlatformSuperAdmin`, `PlatformOperationsAdmin`, `PlatformBillingAdmin`, `PlatformSupportAdmin`, `PlatformAuditor`.
- Tenant: `TenantAdmin`, `StoreAdmin`, `Manager`, `CRMStaff`, `BillingStaff`, `CameraOperator`, `IntegrationAdmin`, `Auditor`.

Phase 3 seeds role/permission definitions and authorization infrastructure. Role-management CRUD and tenant-user assignment screens are delivered in their owning later phases.

## Required Completion Gates

- Repeat-safe Phase 3 SQL runner and version row.
- .NET restore/build with zero warnings; unit/integration authorization tests.
- Real API-host 401 versus 403 tests, platform/tenant scope tests and suspension/session tests.
- Angular lint/tests/production build; auth, role and permission guard tests; permission-filtered navigation; Access Denied behavior.
- Existing rotating-refresh concurrency/cookie/JWT tests remain green.
- Python regression tests remain green.
- npm and NuGet vulnerability scans clean; no production secret committed.

## Work Log

| Date | Item | Status | Evidence / Notes |
|---|---|---|---|
| 2026-08-16 | User approval | Completed | User explicitly approved Phase 3 after requesting this sub-phase implementation file |
| 2026-08-16 | Git safety synchronization | Completed | `origin/master` fetched; ahead 0, behind 0; existing local work preserved |
| 2026-08-16 | Sub-phase plan creation | Completed | Phase 3 dependency order, ownership boundaries and validation gates recorded |
| 2026-08-16 | Authorization data and SQL | Completed | 82-permission catalog, scoped roles/grants, ownership triggers, tenant-safe lookup procedure and security-stamp upgrade applied twice |
| 2026-08-16 | Backend authorization/session enforcement | Completed | Dynamic permission policies, authoritative claim refresh, explicit cross-tenant support permission, suspension/inactive/stamp revocation and correlated audit behavior verified |
| 2026-08-16 | Angular authorization shell | Completed | Typed session/API clients, bootstrap, guards, permission directive, filtered navigation and Access Denied behavior implemented |
| 2026-08-16 | Phase 3 verification | Completed | .NET 11 unit + 30 integration, Angular 32, Playwright 2 and Python 3 tests passed; builds, lint, format and vulnerability gates clean |

## Completion Summary

Phase 3 is complete. Backend authorization is database-authoritative, tenant/platform scope is enforced without blanket platform bypass, and stale/disabled/suspended/inactive sessions are rejected and revoked. The Angular shells now bootstrap the current session, apply auth/role/permission guards, hide unauthorized navigation and handle 401/403 safely.

The repeat-safe SQL chain was applied twice. The live database has compatibility level 160, 82 permissions, four authorization tables, two ownership/scope triggers, one authorization procedure, a non-null `IssuedSecurityStamp` column and exactly one `V1.2.0` ledger row.

Final gates: .NET build 0 warnings/errors; 11 unit and 30 integration tests; Angular lint, 32 tests and production build; Playwright 2/2; Python Ruff and 3 tests; npm/NuGet vulnerability scans clean. Independent security audit found no remaining Phase 3 blockers.
