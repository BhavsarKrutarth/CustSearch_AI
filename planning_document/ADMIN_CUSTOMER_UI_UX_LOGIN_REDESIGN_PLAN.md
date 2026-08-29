# Admin + Customer Admin UI/UX and Login Reliability Plan

Date: 2026-08-29  
Scope: `src/CustSearch.Admin`

## Objective

Replace the current basic Angular presentation with a modern, calm, professional
operations product. The visual direction is intentionally non-gimmicky: no neon AI
gradients, robot/chat surfaces, fake confidence scores, or decorative dashboards. Use
clear hierarchy, excellent empty/loading/error states, keyboard accessibility, and fast
task completion for both Platform Admin and Customer/Tenant Admin.

## Current findings

- One Angular application serves both Platform Admin and Customer Admin through separate
  route/navigation contexts.
- `AdminShell` already filters navigation by server-issued permissions and supports light,
  dark, and system themes, but the visual language is still mostly basic cards and text.
- Login posts `/api/auth/login`; a blank tenant code is interpreted by the API as a
  Platform Admin login. Customer/Tenant Admin login requires the tenant code.
- Refresh uses an HttpOnly cookie plus an in-memory access token, with a single-flight
  refresh interceptor. This must remain unchanged from a security perspective.
- Login currently displays one generic error for all failures and does not show a safe
  correlation reference, field-level guidance, offline/server/rate-limit distinction, or
  password visibility control.
- The local API (`https://localhost:7277`) and Angular dev server were not running during
  this check, so a real credential login could not be completed in this session. Valid
  smoke credentials must be supplied only through the local environment; never commit or
  log a password.

## Design direction

- Editorial enterprise layout: strong typography, neutral surfaces, one restrained accent,
  compact density, consistent 8px spacing rhythm, and meaningful status colors.
- Separate Platform and Customer shells through an explicit context label, navigation
  grouping, breadcrumbs, and a responsive mobile drawer.
- Replace decorative “AI” branding with product/workspace identity. AI-derived data may be
  shown as an evidence-backed insight with source/time/confidence context, never as a
  decision or accusation.
- Every data page gets a consistent state model: loading skeleton, empty explanation,
  recoverable error with Retry, permission-denied state, and success feedback.
- Preserve accessible focus rings, semantic headings, visible labels, reduced motion, and
  WCAG AA contrast in light and dark themes.

## Information architecture

Platform Admin: Overview, Tenants, Users/Stores, Plans & Billing, Operations/Health,
Audit, Settings.  
Customer Admin: Overview, Customers, Visits, Stores/Staff, Retail, Cameras, Security,
Alerts, Reports, Storage, Settings.

The shell should expose only authorized groups and provide a “current workspace” switch
only when the server session grants that scope. No client-selected tenant id may be sent
to tenant APIs.

## Login reliability requirements

1. Add an explicit workspace choice: Platform Admin or Customer Admin. Platform mode sends
   `tenantCode: null`; Customer mode requires a trimmed tenant code before submit.
2. Keep username/email and tenant code trimmed; never trim or transform the password.
3. Map safe server errors by status/code: invalid credentials, locked/unavailable account,
   rate limited, offline/API unavailable, malformed response, and unexpected server error.
   Show a short user message plus the server correlation id when present.
4. Disable duplicate submits, show progress, clear password after every response, and focus
   the error summary. Add show/hide password and an accessible “try again” action.
5. On successful login validate the JWT expiry and user payload before routing. Route based
   on `isPlatformAdmin`; if the payload is incomplete, clear memory and show a recoverable
   error instead of navigating into a broken shell.
6. Preserve refresh/logout security: access tokens stay in memory, refresh stays HttpOnly,
   auth endpoints are not intercepted with a bearer token, and a failed refresh clears the
   session and redirects to login once.
7. Add automated tests for platform login, customer login, missing tenant code, 401/423/429/
   5xx/network failures, malformed success payload, duplicate submit, and refresh recovery.

## UI implementation phases

### UX-0 — Baseline and instrumentation

- Capture screenshots/routes for both contexts and record current Angular test/build counts.
- Add a small client error mapper and correlation-safe logger; never log credentials,
  tokens, response bodies, or personal data.
- Verify all API routes used by the shell against the current server route map.

### UX-1 — Design system foundation

- Consolidate tokens in `styles.scss`: type scale, surfaces, borders, spacing, elevation,
  focus, status, density, and responsive breakpoints.
- Create shared primitives: page header, breadcrumb, stat/metric card, data table,
  filter bar, empty state, skeleton, error state, toast/notice, badge, and confirmation
  dialog. Keep components semantic and small.
- Fix variable-name drift in legacy pages (for example access-denied styles using token
  names that are not defined by the current theme).

### UX-2 — Shell and navigation redesign

- Rework `AdminShell` into responsive sidebar/drawer + topbar with workspace identity,
  breadcrumbs, notification status, theme control, account menu, and mobile navigation.
- Add active-route context, page-level loading progress, skip links, and a consistent
  “you do not have access” recovery path.
- Ensure platform and customer contexts have visibly distinct accent/context labels while
  sharing the same component system.

### UX-3 — Login and session hardening

- Implement the login reliability requirements above in `login-page.*`, auth error mapper,
  and focused tests.
- Exercise real local API login using the smoke runner password supplied through
  `CUSTSEARCH_SMOKE_PASSWORD` or `CUSTSEARCH_MANUAL_TEST_PASSWORD`; do not write it to
  files, shell history, screenshots, or logs.
- Test Platform Admin, Customer Admin, wrong tenant code, wrong password, locked account,
  refresh expiry, logout, and direct deep-link navigation.

### UX-4 — High-value page redesign

- Redesign dashboards around decisions and next actions, not decorative charts.
- Redesign tenants/customer/staff/stores tables with saved filters, server pagination,
  column priority on mobile, inline status, and clear bulk-action confirmation.
- Redesign retail, camera, security, alerts, reports, and storage pages using the shared
  primitives; retain all existing permission boundaries and audit behavior.

### UX-5 — Quality, accessibility, and rollout

- Add component/page tests for every new shared primitive and auth state.
- Run Angular lint, full unit tests, production build, keyboard-only checks, responsive
  checks at 390/768/1280px, and Chromium smoke flows for both admin contexts.
- Review performance budgets and remove unnecessary bundle weight; fix new warnings before
  rollout. Release behind a reversible UI flag if product wants gradual adoption.

## Login UAT checklist

- Start API HTTPS profile on `https://localhost:7277` and Angular proxy on `http://localhost:4200`.
- Use a local-only strong password through the documented smoke runner.
- Platform: choose Platform Admin, leave tenant code blank, sign in, verify `/platform-admin`.
- Customer: choose Customer Admin, enter the exact tenant code, sign in, verify
  `/customer-admin/dashboard` and tenant-scoped navigation.
- Verify wrong password, wrong tenant code, empty customer tenant code, API stopped,
  throttled login, logout, refresh, and direct protected URL behavior.
- Record only status, safe message, correlation id, route, and timestamp in the progress
  log; never record password or bearer/refresh token values.

## Definition of done

- Both admin contexts look intentional and consistent on desktop/mobile/light/dark/system.
- Login failures are actionable and distinguish configuration/network/auth problems without
  leaking security-sensitive detail.
- Real Platform and Customer smoke logins pass with local-only credentials.
- All existing authorization, tenant isolation, refresh, audit, and security tests remain
  green; no API accepts a browser-supplied tenant id.
- `WORK_PROGRESS_LOG.txt` and `ALL_PHASE_WORK_PROGRESS.txt` contain the implementation,
  test, UAT, error/fix, and pending-rollout history.

## Corporate-grade product blueprint (expanded direction)

The redesign must feel like a governed B2B operations product (not a collection of KPI
cards). Every screen follows the same information hierarchy: page purpose, scope/context,
primary decision, supporting evidence, then secondary actions.

### Product areas and role workspaces

**Platform Admin workspace**

- Command Center: tenant health, adoption, service incidents, expiring subscriptions and
  unresolved high-severity alerts.
- Tenant Operations: tenant directory, lifecycle status, plan/entitlements, usage, contacts,
  feature flags and an auditable tenant detail workspace.
- Identity & Governance: users, roles, permission templates, invitations, access reviews,
  session/security events and audit export.
- Commercial: plans, invoices, payment status, limits and billing exceptions.
- Platform Operations: integrations, storage, camera fleet health, jobs and API health.

**Customer Admin workspace**

- Operations Home: store/zone scope, today’s visits, review queue, camera availability and
  action-required alerts.
- Customer & Visitor 360: searchable profiles, household relationships, visit timeline,
  consent/retention indicators and related evidence.
- Store & Camera Operations: store hierarchy, camera health, coverage, motion rules and
  incident evidence.
- Review & Security: recognition review queue, alerts, cases, evidence timeline and audit.
- Team & Settings: scoped users/roles, integrations, notification preferences and retention.

### Navigation and page composition

- Persistent sidebar with grouped domains, role-filtered items, unread counts and a command
  search (`/` shortcut) for pages, customers, tenants and actions.
- Top context bar always shows workspace, tenant/store scope, environment, last sync time,
  notifications and profile/session controls.
- Standard page header: breadcrumb, title, one-line purpose, scope selector, last-updated
  timestamp and one primary action; secondary actions live in an overflow menu.
- Detail screens use tabs (`Overview`, `Activity`, `Access`, `Evidence`, `Settings`) and a
  right-side contextual action panel instead of long undifferentiated forms.
- URL-driven filters and tabs so links are reproducible, bookmarkable and safe to refresh.

### Design system and interaction quality bar

- Figma-ready token contract for color, typography, spacing, elevation, radius, motion and
  density; implement tokens as Angular CSS custom properties.
- Three density modes: Comfortable (default), Compact (operations tables), Touch (tablet).
- Neutral navy/slate foundation with one brand accent and semantic success/warning/danger;
  no gradients used to encode status and no decorative “AI” motifs.
- Component library before page work: AppShell, PageHeader, ScopeBar, Metric, DataTable,
  FilterBar, SavedView, StatusBadge, Timeline, EvidencePanel, Drawer, Dialog, Toast,
  EmptyState, ErrorState, Skeleton and PermissionGate.
- Keyboard-first interactions, visible focus, 44px touch targets, WCAG AA contrast, reduced
  motion support and screen-reader labels for icons/graphs.

### Enterprise workflows (must be designed before coding)

- Tenant onboarding: create → configure plan → invite owner → connect integration → verify
  health → activate, with resumable steps and audit events.
- User access: invite → role template → scope selection → approval (if required) → activation;
  show effective permissions before save.
- Incident review: alert → evidence/timeline → assign owner → add note → resolve/escalate;
  preserve immutable history.
- Camera remediation: offline camera → diagnostics → retry/reconfigure → verify stream →
  close incident, with clear retry safety and last-seen timestamps.
- Billing exception: failed payment/limit → impact summary → action → confirmation → audit.

### Responsive and state requirements

- Desktop (1440/1280): three-column operational layouts where useful; tablet (1024/768):
  collapsible navigation and two-column detail; mobile (390): single-column cards, filter
  drawer, sticky primary action and table-to-list transformation.
- Every async page defines loading, partial-data, empty, permission-denied, offline and
  server-error states before implementation.
- Destructive operations require reason/confirmation; long operations show progress and
  allow safe navigation without duplicate submissions.

### Delivery governance and acceptance gates

- Phase 1: inventory and role/task mapping; approve sitemap and critical workflows.
- Phase 2: tokens and shared components; visual regression baseline.
- Phase 3: shell and authentication; both roles pass navigation/accessibility UAT.
- Phase 4: highest-value workflows (tenant onboarding, visitor 360, incident review,
  camera remediation); API contracts remain unchanged unless explicitly approved.
- Phase 5: remaining modules, migration, training notes and release checklist.
- Gate each phase on product sign-off, keyboard/mobile review, no new console errors,
  Angular tests/build green, authorization/tenant-isolation tests green, and measured
  performance (LCP/route load) within agreed budgets.
