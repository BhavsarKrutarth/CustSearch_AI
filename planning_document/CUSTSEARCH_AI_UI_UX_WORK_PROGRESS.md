# CustSearch AI UI/UX Work Progress

## UI-01 - Audit and design foundation

Status: COMPLETE

Files changed: `src/CustSearch.Admin/src/styles.scss`, `src/CustSearch.Admin/src/app/shared/cs-icon/cs-icon.ts`

Tests executed: `npm install`, `npm run lint`, `npm run test:ci`, `npm run build:production`.

Results: semantic dark/light tokens, compatibility aliases, global panel/table/state primitives, focus treatment, reduced-motion behavior, and semantic action-button variables added. No API or guard changes.

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

Angular lint and unit suite pass: 36 files / 96 tests. Production build passes. Full Playwright suite passes: 53/53.

Database/API smoke: SQL Server and API health are reachable. The tenant-scoped account matching the supplied username is active TenantAdmin for `TEN-35D77F00D7F0`, assigned to the UAT store, and already has the configured camera preview grant. DB-sourced `DisplayPassword` login returned 401 for both scoped identities, so the hash/display-password mismatch is unresolved without an authorized credential rotation. Physical preview is also blocked by `CctvPreview.Enabled=false`, unavailable AI frame service, and missing confirmed camera MAC/IP plus runtime RTSP secret.

Files changed for QA: `src/CustSearch.Admin/README.md`, `tenant-detail-page.html`, `auth-authorization.spec.ts`, `phase5-customer-admin.spec.ts`, and `phase6-shopper-customers.spec.ts`.

Next step: capture the responsive viewport screenshot matrix and resolve the local UAT credential/configuration mismatch before live physical-camera validation.

## UI-14 - Customer-wise theme and color system

Status: COMPLETE (frontend foundation)

Files changed: `src/CustSearch.Admin/src/app/core/theme/theme.service.ts`, `theme.service.spec.ts`, `tenant-theme.models.ts`, `theme-buttons.scss`, `src/CustSearch.Admin/src/app/features/theme/tenant-theme-page.ts`, `tenant-theme-page.html`, `tenant-theme-page.scss`, `src/CustSearch.Admin/src/app/shared/admin-shell/admin-shell.ts`, `admin-shell.html`, `src/CustSearch.Admin/src/app/shared/cs-icon/cs-icon.ts`, `src/CustSearch.Admin/src/app/app.routes.ts`, and `src/CustSearch.Admin/src/styles.scss`.

Results: added a role-guarded Custom theme page for customer administrators; placed `Custom theme` below `My profile & security`; separate light/dark tenant palettes; validated six-digit hex input; tenant-scoped browser persistence; tenant context isolation; dedicated high-contrast sidebar tokens; hidden scrollbar chrome; consistent SVG shell controls; and semantic `.cs-btn-primary`, `.cs-btn-secondary`, `.cs-btn-success`, `.cs-btn-warning`, `.cs-btn-danger`, `.cs-btn-neutral`, and `.cs-btn-outline` variants. Existing `light`, `dark`, and `system` theme preferences remain intact. No existing API request, response, camera session, permission, or route contract was changed.

Tests executed: `npm run lint`, `npm run test:ci`, `npm run build:production`, and `npx playwright test --workers=1`. Unit suite: 36 files / 96 tests passed. Production build passed with existing non-failing dashboard style-budget warnings. Playwright: 53/53 passed after the shell changes.

Known issue: theme changes currently persist only in the browser under the authenticated tenant code. Cross-device/customer-wide persistence requires a backend endpoint, a tenant-theme permission, validation, and audit logging; those were not invented because no existing contract was available.

Next step: product decision and backend contract for shared tenant branding, then migrate remaining feature-specific buttons/tables to the semantic variants and capture the responsive screenshot matrix.

## UI-15 - Staff, user, and visitor record management

Status: COMPLETE

Files changed: `src/CustSearch.Admin/src/app/features/customer-admin/phase-five-management-page.ts`, `src/CustSearch.Admin/src/app/features/customer-admin/phase-five-api.service.ts`, `src/CustSearch.Admin/src/app/features/visitors/visitor-list-page.ts`, `src/CustSearch.Admin/src/app/features/visitors/visitor-api.service.ts`, `src/CustSearch.Admin/src/app/shared/cs-icon/cs-icon.ts`, `src/CustSearch.Api/Controllers/ShopperCustomersController.cs`, `src/CustSearch.Application/ShopperCustomers/IShopperCustomerService.cs`, `src/CustSearch.Application/ShopperCustomers/ShopperCustomerModels.cs`, `src/CustSearch.Domain/Entities/PhaseSixCustomerEntities.cs`, `src/CustSearch.Infrastructure/ShopperCustomers/ShopperCustomerService.cs`, and the affected Playwright specs.

Results: Users and Staff now have compact semantic tables with client pagination for the existing array API contract, stable icon-only edit/reset/deactivate actions, active-state badges, empty states, and keyboard labels. Visitors now have server-backed pagination with page-size controls, add, edit, convert, and deactivate actions. All actions remain permission-gated by the existing route/API permission (`TenantUsers.Edit`, `Staff.Manage`, and `Visitors.Convert`).

Data safety: “Delete” is presented as deactivation for Users, Staff, and Visitors. No hard delete was added because these rows participate in authentication, visit, CCTV, audit, and tenant-history relationships. Visitor update/deactivation endpoints were added only because no existing browser API contract supported those requested operations; they retain tenant/store visibility checks and audit events. Converted visitors remain immutable.

Tests executed: `npm run lint`, `npm run test:ci` (36 files / 96 tests), `npm run build:production`, .NET unit tests (120/120), API compile verification, and affected Playwright phase 5/6 suite (16/16 including CRUD coverage). Full Playwright regression completed at 54/54.

Known issues: Users and Staff list endpoints still return arrays, so their pagination is client-side until a paged backend contract is introduced. Visitor add currently requires an authorized operator to enter a valid store ID; no store-picker API was invented. Confirmation uses the browser’s native confirmation dialog.

Next step: apply the same table/action treatment to Customers, Households, Visits, Cameras, Alerts, and Reports.
