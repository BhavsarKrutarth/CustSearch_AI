# CustSearch AI UI/UX Work Progress

## UI-01 - Audit and design foundation

Status: COMPLETE

Files changed: `src/CustSearch.Admin/src/styles.scss`, `src/CustSearch.Admin/src/app/shared/cs-icon/cs-icon.ts`

Tests executed: `npm install`, `npm run lint`, `npm run test:ci`, `npm run build:production`.

Results: semantic dark/light tokens, compatibility aliases, global panel/table/state primitives, focus treatment, and reduced-motion behavior added. No API or guard changes.

Known issues: two existing dashboard component styles exceed the warning threshold; no error budget remains exceeded.

Next step: shared shell implementation.

## UI-02 - Shared AdminShell

Status: COMPLETE

Files changed: `admin-shell.ts`, `admin-shell.html`, `admin-shell.scss`, `admin-navigation.ts`.

Results: grouped permission-filtered nav, inline SVG icons, active state, collapsed rail, mobile drawer, topbar search shell, theme/user controls, responsive content width, and preserved logout flow.

Authorization: existing server-issued permissions remain the only source for visible navigation; route guards remain unchanged.

## UI-03 - Login

Status: COMPLETE

Files changed: `login-page.ts`, `login-page.html`, `login-page.scss`, `login-page.spec.ts`.

Results: responsive enterprise split-screen login; unchanged `POST /api/auth/login` body, HttpOnly credentials, in-memory token storage, platform/customer redirect, safe error handling, and password visibility behavior.

Testing instruction: local/dev/UAT credentials must be selected from the latest `Users` and `Tenants` data; use `UserName`/`Email`, `DisplayPassword`, `IsActive = 1`, and matching tenant scope. Never guess or hardcode passwords; never expose `DisplayPassword` in production.

## UI-04 - Reusable dashboard foundation

Status: IN PROGRESS

Files changed: global primitives plus the routed tenant dashboard and platform dashboard.

Results: KPI, panel, loading, empty, error, lifecycle, and capacity patterns are established in the two primary dashboards.

Next step: extract reusable components only after the remaining feature templates are migrated, to avoid unnecessary abstraction.

## UI-05 - Platform dashboard

Status: COMPLETE

Files changed: `platform-dashboard.ts`, `platform-dashboard.html`, `platform-dashboard.scss`.

Results: all displayed portfolio metrics remain API-backed; compact six-card overview, MRR panel, lifecycle distribution, posture panel, refresh/retry states.

## UI-06 - Customer dashboard

Status: COMPLETE

Files changed: `phase-five-dashboard-page.ts`.

Results: routed customer dashboard now uses `PhaseFiveApiService.dashboard()` and tenant-scoped summary values. Missing visit analytics are clearly marked as an integration empty state instead of being fabricated.

## UI-07 to UI-10 - Feature modules

Status: IN PROGRESS

Existing routes and services inspected; global semantic tokens now flow through shell, camera, alert, customer, household, visit, retail, report, and platform modules. Per-screen dense table/filter extraction remains.

## UI-11 - Responsive/mobile polish

Status: IN PROGRESS

Shell and login breakpoints are implemented for desktop, tablet, and mobile. Full viewport screenshot review and remaining feature-specific table overflow review are pending.

## UI-12 - Accessibility and cross-browser QA

Status: IN PROGRESS

Keyboard labels, focus states, semantic alerts, navigation current state, and reduced motion were addressed in changed screens. Full browser matrix and screen-reader pass are pending.

## UI-13 - Automated tests

Status: COMPLETE

Angular lint and unit suite pass: 36 files / 94 tests. Production build passes. Full Playwright suite passes: 53/53 with one transient tenant-edit DOM timing failure reproduced as 1/1 pass in isolation and then cleared on the complete rerun.

Database/API smoke: SQL Server and API health are reachable. The tenant-scoped account matching the supplied username is active TenantAdmin for `TEN-35D77F00D7F0`, assigned to the UAT store, and already has the configured camera preview grant. DB-sourced `DisplayPassword` login returned 401 for both scoped identities, so the hash/display-password mismatch is unresolved without an authorized credential rotation. Physical preview is also blocked by `CctvPreview.Enabled=false`, unavailable AI frame service, and missing confirmed camera MAC/IP plus runtime RTSP secret.

Files changed for QA: `src/CustSearch.Admin/README.md`, `tenant-detail-page.html`, `auth-authorization.spec.ts`, `phase5-customer-admin.spec.ts`, and `phase6-shopper-customers.spec.ts`.

Next step: capture the responsive viewport screenshot matrix and resolve the local UAT credential/configuration mismatch before live physical-camera validation.
