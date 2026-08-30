# CustSearch AI UI/UX Enterprise Redesign

## Current UI audit

The frontend is an Angular 21 standalone application using Angular Material/CDK, RxJS, Chart.js, SignalR, SCSS, lazy-loaded routes, and signal-based state. Authentication is handled by `POST /api/auth/login` with an HttpOnly refresh session and in-memory access token. `authGuard`, `roleGuard`, and `permissionGuard` remain authoritative at route level; the shell only filters navigation for convenience.

The previous baseline had a light-first token set, a narrow shell, text-character navigation icons, a minimal login card, a minimal routed customer dashboard, and several feature screens with local one-line styles. The existing API services and security-sensitive camera/session logic were intentionally retained.

## Visual direction and design tokens

The primary experience is a dark, dense operations console: deep blue-black workspace surfaces, compact 44px table rows, restrained teal/blue accents, thin borders, and semantic status colors. Light mode remains supported through `ThemeService` and the same semantic variables.

Central tokens live in `src/CustSearch.Admin/src/styles.scss`:

- Surfaces: `--cs-bg`, `--cs-sidebar`, `--cs-panel`, `--cs-panel-hover`, `--cs-panel-raised`
- Structure: `--cs-border`, `--cs-border-strong`, radii, shadows, spacing, z-index layers
- Content: `--cs-text`, `--cs-secondary`, `--cs-muted`
- Meaning: `--cs-primary`, `--cs-success`, `--cs-warning`, `--cs-danger`, `--cs-info`
- Layout: sidebar width/collapsed width, topbar height, input height, table row height

Compatibility aliases (`--color-*`) remain so existing feature screens migrate safely without changing their behavior.

Typography uses an Inter/system sans-serif stack with 24-32px page titles, 15-17px section headings, compact 11-14px UI text, and 11px table headings.

## Shared components and shell

- `AdminShell` now provides a fixed/sticky 252px sidebar, 74px collapsed rail, mobile drawer, grouped permission-filtered navigation, inline SVG icons, active route indicator, workspace context, global-search shell with `Ctrl K` hint, theme selector, notification/security controls, user menu, and logout.
- `CsIcon` (`app-cs-icon`) is a dependency-free inline SVG icon set for navigation and shell actions.
- Global layout primitives include page headers, filter bars, panels, table states, semantic badges, and focus/reduced-motion behavior.
- Customer-wise theming is implemented through `ThemeService`, `TenantThemePalette`, and the guarded `/customer-admin/theme` route. Tenant admins can customize separate light and dark palettes, including brand, surfaces, text, status colors, and semantic action buttons. The selected tenant context is applied through semantic CSS variables, so switching tenants clears the previous tenant's inline palette.
- Shared `.cs-btn-*` variants now provide consistent primary, secondary, success, warning, danger, neutral, and outline action treatment without component-level color duplication.
- The supplied shell review image was handled: profile menu wording is now `My profile & security` followed by `Custom theme`; the custom-theme item opens `/customer-admin/theme`; sidebar labels use dedicated high-contrast sidebar tokens in light mode; navigation scrollbar chrome is hidden without disabling scrolling; and search, menu, close, and collapse controls use consistent SVG icons instead of placeholder characters.

## Screens implemented

- Login: split enterprise visual, abstract analytics/CCTV signal artwork, responsive single-column mobile layout, labeled fields, safe password toggle, submitting state, sanitized errors, and secure-session messaging. Authentication contract and redirects are unchanged.
- Platform dashboard: API-backed tenant/user/camera/MRR metrics, compact portfolio KPI grid, lifecycle health view, refresh/retry states, and governed-platform posture panel.
- Customer dashboard: routed Phase 5 dashboard now uses `PhaseFiveApiService.dashboard()` for tenant-scoped metrics, compact KPI grid, operational capacity meters, and an explicit empty state for visit analytics not yet exposed by the backend. No random production values were added.
- Camera monitoring and alert center inherit the shell, semantic tokens, responsive layout, and existing camera authorization/preview and SignalR/REST behavior. Their contracts were not changed.
- Existing list/detail modules remain lazy-loaded and permission-protected; further per-screen table extraction is tracked below.
- Staff, User, and Anonymous Visitor management now share the same dense table/action language: semantic status badges, icon-only edit/reset/deactivate actions, stable test IDs, empty states, and readable pagination controls. Users/Staff use client pagination because their existing APIs return arrays; Visitors retain server-side pagination.

## Responsive and accessibility rules

Desktop uses the expanded shell, tablet uses the drawer/compact topbar, and mobile uses a single-column content layout with 44px+ interactive controls. Navigation exposes `aria-current`, icon-only controls have labels, form labels remain associated, errors use `role="alert"`, the drawer backdrop is keyboard-closeable, and reduced-motion preferences disable non-essential animation.

## Authorization and API contracts preserved

No route guard, role list, permission name, tenant request model, camera preview session, SignalR stream, or login response contract was renamed or weakened. Tenant dashboards consume the existing tenant summary endpoint; platform metrics consume the existing platform dashboard endpoint. No TenantId was introduced into browser request models. The only additive API contract is visitor update/deactivation, required to support the requested UI operations where no existing endpoint existed; it enforces the same tenant/store visibility and audit boundary.

## Record actions and pagination

- Users: existing create/update/role/store APIs are preserved. Edit, password reset, and deactivate actions are permission-aware; deactivation uses the existing update endpoint with `IsActive = false`.
- Staff: existing create/update APIs are preserved. Edit and deactivate use the existing staff update endpoint with `IsActive = false`.
- Anonymous Visitors: existing search/create/convert APIs are preserved, with additive `PUT /api/tenant/visitors/{id}` and `DELETE /api/tenant/visitors/{id}` for audited code/status updates and reversible deactivation. Converted visitors cannot be edited or deactivated.
- Hard deletion is intentionally not exposed. User/staff/visitor records are referenced by auth, visit, CCTV, and audit history; UI “delete” means confirmed deactivation, retaining the row and history.
- Icon actions use the shared inline SVG `CsIcon` set (`edit`, `key`, `user-plus`, `trash`) and `data-testid` hooks for stable automation.

## Testing login instruction (local/dev/UAT only)

Never guess or hardcode a testing password. Before testing, query the latest `Users` records and select an active account:

```sql
SELECT * FROM Users;
SELECT * FROM Tenants;
```

Use `UserName` or `Email` as the login identifier and the current `DisplayPassword` value as the local/dev/UAT testing password. Verify `IsActive = 1`. For tenant testing, verify `Users.TenantId = Tenants.Id`; use the correct tenant-scoped user only. For Platform Admin testing, use a platform-scope account with `TenantId = NULL` or the appropriate platform scope. Do not accidentally use a different tenant’s account.

Examples: `smoke.platform`, `smoke.tenantadmin`, and `smoke.staff` must each use the current password read from that user’s `DisplayPassword` column. Check it again before every test run because setup may change it.

`DisplayPassword` is a local/dev/UAT convenience only. Do not store or display plaintext passwords in production, expose `DisplayPassword` from normal production APIs, or render it in the login page.

## Files changed in this phase

- `src/CustSearch.Admin/README.md` (local/dev/UAT login safety instruction)
- `src/CustSearch.Admin/src/styles.scss`
- `src/CustSearch.Admin/src/app/shared/cs-icon/cs-icon.ts`
- `src/CustSearch.Admin/src/app/shared/admin-shell/admin-shell.ts`
- `src/CustSearch.Admin/src/app/shared/admin-shell/admin-shell.html`
- `src/CustSearch.Admin/src/app/shared/admin-shell/admin-shell.scss`
- `src/CustSearch.Admin/src/app/core/navigation/admin-navigation.ts`
- `src/CustSearch.Admin/src/app/core/theme/theme.service.ts`
- `src/CustSearch.Admin/src/app/core/theme/theme.service.spec.ts`
- `src/CustSearch.Admin/src/app/core/theme/tenant-theme.models.ts`
- `src/CustSearch.Admin/src/app/core/theme/theme-buttons.scss`
- `src/CustSearch.Admin/src/app/features/theme/tenant-theme-page.ts`
- `src/CustSearch.Admin/src/app/features/theme/tenant-theme-page.html`
- `src/CustSearch.Admin/src/app/features/theme/tenant-theme-page.scss`
- `src/CustSearch.Admin/src/app/features/auth/login-page.ts`
- `src/CustSearch.Admin/src/app/features/auth/login-page.html`
- `src/CustSearch.Admin/src/app/features/auth/login-page.scss`
- `src/CustSearch.Admin/src/app/features/customer-admin/phase-five-dashboard-page.ts`
- `src/CustSearch.Admin/src/app/features/platform-admin/platform-dashboard.ts`
- `src/CustSearch.Admin/src/app/features/platform-admin/platform-dashboard.html`
- `src/CustSearch.Admin/src/app/features/platform-admin/platform-dashboard.scss`
- `src/CustSearch.Admin/src/app/features/auth/login-page.spec.ts` (lint-safe type assertion)
- `src/CustSearch.Admin/src/app/features/security/security-api.service.ts` (lint-safe payload copy; same API payload)
- `src/CustSearch.Admin/src/app/features/platform-tenants/tenant-detail-page.html` (accessible suspension-reason label)
- `tests/CustSearch.Admin.E2E/tests/auth-authorization.spec.ts` (required mocked tenant-admin form values)
- `tests/CustSearch.Admin.E2E/tests/phase5-customer-admin.spec.ts` (refresh/me mocks for full-page guard checks)
- `tests/CustSearch.Admin.E2E/tests/phase6-shopper-customers.spec.ts` (refresh/me mocks for full-page guard checks)
- `src/CustSearch.Admin/src/app/features/customer-admin/phase-five-management-page.ts` (user/staff icon actions, semantic table styling, pagination)
- `src/CustSearch.Admin/src/app/features/customer-admin/phase-five-api.service.ts` (existing update APIs exposed as deactivation helpers)
- `src/CustSearch.Admin/src/app/features/visitors/visitor-list-page.ts` (visitor add/edit/deactivate UI, server pagination controls)
- `src/CustSearch.Admin/src/app/features/visitors/visitor-api.service.ts` (visitor create/update/deactivate request types)
- `src/CustSearch.Api/Controllers/ShopperCustomersController.cs` (additive visitor update/deactivate endpoints)
- `src/CustSearch.Application/ShopperCustomers/IShopperCustomerService.cs`, `ShopperCustomerModels.cs`, `src/CustSearch.Domain/Entities/PhaseSixCustomerEntities.cs`, and `src/CustSearch.Infrastructure/ShopperCustomers/ShopperCustomerService.cs` (audited visitor lifecycle implementation)
- `tests/CustSearch.Admin.E2E/tests/phase5-customer-admin.spec.ts` and `phase6-shopper-customers.spec.ts` (stable CRUD action coverage)

## Verification and screenshots

- `npm install`: completed; 600 packages audited, no vulnerabilities reported.
- `npm run lint`: passed.
- `npm run test:ci`: passed, 36 test files / 96 tests, including tenant palette isolation and light/dark separation tests.
- `npm run build:production`: passed. Angular reports non-failing style-budget warnings for existing customer/platform dashboard styles; no production budget errors remain.
- `npx playwright test --workers=1`: passed, 53/53 tests. The tenant-edit fixture was stabilized by mocking its existing administrator lookup and waiting for the authoritative form value. No theme route or shell authorization failure was observed.
- Affected CRUD regression suite: phase 5/6 passed 16/16 after the table-width and accessible-name fixes; new visitor add/edit/deactivate and user/staff deactivation coverage is green. Full Playwright regression passed 54/54.
- Database/API smoke: SQL Server `KRUTARTH-BHAVSA / CustSearch_AI` connected and API `/health/live` returned 200.
- Database-backed tenant authorization: the tenant-scoped identity matching the supplied username is active, has the TenantAdmin role, is assigned to the UAT store, has the camera permissions, and already has an active preview grant for the configured UAT camera. No grant mutation was necessary.
- Credential smoke: current SQL `DisplayPassword` values were used only in process memory and were not printed. Both platform and tenant API login attempts returned 401, indicating the local display-password value and API password hash are not currently synchronized; no password was guessed, reset, or committed.
- Physical camera smoke: the configured camera row is present, but its documented MAC was not present in the current ARP table. The API has `CctvPreview.Enabled=false`, the AI frame service on `127.0.0.1:8000` is unavailable, and no RTSP secret/IP was added. Live physical monitoring remains blocked until the camera MAC/IP and authorized runtime secret are confirmed and preview is enabled deliberately.
- Screenshot capture: the supplied `change.png` review was applied. Automated Chromium browser regression passed after the shell changes; a complete manual screenshot matrix at 1920, 1440, 1366, 1024, 768, 430, 390, and 360 widths remains a follow-up.

## Remaining TODOs

1. Extract shared KPI/table/filter/loading components where existing feature templates justify reuse.
2. Connect visit/customer analytics widgets only to existing authoritative endpoints; keep explicit empty states where no endpoint exists.
3. Apply the new table surface to customer, household, visit, camera, and report pages with stable `data-testid` selectors.
4. Capture screenshots at 1920, 1440, 1366, 1024, 768, 430, 390, and 360 widths.
5. Review style-budget warnings during the next per-screen extraction pass.
6. Add an authenticated backend tenant-theme endpoint and audit trail if themes must be shared across browsers/devices. The current implementation deliberately uses tenant-keyed browser storage because no existing theme persistence API or permission contract was found; no backend contract was changed.
7. Introduce paged Users/Staff endpoints when the backend contract is ready; current UI pagination is deliberately client-side over the existing array response.
