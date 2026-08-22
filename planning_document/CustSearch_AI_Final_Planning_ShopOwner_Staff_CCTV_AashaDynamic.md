CustSearch AI — Final Production Project Plan
ASP.NET Core 8 Web API + Angular Platform Admin + Angular Customer Admin + Multi-Tenant SQL Server 2022 + Entity Framework Core + Dapper + Stored Procedures + CCTV AI + SignalR/WebSocket

1. Project Objective
Create a production-grade retail intelligence application named:
CustSearch AI
Main capabilities:
    • Customer Management
    • Customer Search
    • Anonymous Visitor Management
    • CCTV Monitoring
    • Person Detection
    • Face Detection
    • Consent-Based Recognition
    • Customer Visits
    • Purchase History
    • Invoices
    • Returning Customer Alerts
    • VIP Alerts
    • Notifications
    • Household / Family Management
    • Customer Preferences
    • Household Preferences
    • Webhooks / Existing Shop Software Integration
    • Admin Management
    • Role & Permission Management
    • Reports
    • Audit Logs
    • System Health
    • Platform / Super Admin
    • Client / Tenant Management
    • Customer Admin Panel
    • Tenant User Management
    • Tenant-wise Stores, Customers, Invoices and Reports
    • Platform Billing / Subscription Invoices
    • Tenant-wise Audit and Usage Reports
    • Real-time SignalR / WebSocket Event Handling
    • Shop Owner / Store Owner Operations
    • Tenant Staff Management and Staff Performance
    • CCTV-Based Customer + Staff Journey Tracking
    • Customer Dwell-Time / Zone Interest Analytics
    • Staff-to-Customer Interaction Tracking
    • Dynamic Store-Wise Voice Trigger Keyword (default example: Aasha Add)
    • Voice-Assisted Customer Category / Interest Tagging
    • Staff Assisted Conversion and Monthly Performance Reports
Planning Review — Angular Conversion Decisions
This revision intentionally changes the original Admin architecture from server-rendered ASP.NET MVC/Razor to an Angular SPA. The backend, database, AI, household, spend-attribution, recognition, integration and privacy business rules remain server-side and unchanged unless explicitly corrected for consistency/security.
Key corrections made in this revision:
    • Angular is now the required Admin frontend.
    • MVC/Razor/jQuery Admin dependencies are removed.
    • CustSearch.Admin is a Node/Angular project, not a .NET project.
    • Angular auth, permission guards, REST clients and SignalR client are defined.
    • IIS SPA fallback + WebSocket deployment is defined.
    • Angular unit tests and Playwright E2E are added to build validation.
    • Admin routes are normalized to lowercase /admin/... routes.
    • The Aadhaar/PAN-by-face idea is removed because it conflicts with the plan's own consent/privacy rules; identity-document handling, if ever required, must be a separate authorized workflow.
    • Duplicate/incorrect feature numbering and webhook wording are cleaned up where practical without rewriting all business sections.
    • Multi-tenant Client/Customer Admin architecture is now required.
    • Platform Admin can manage every client tenant; Customer Admin can manage only its own tenant.
    • Tenant-level users, stores, shopper customers, invoices, reports, cameras, integrations and settings are isolated by TenantId.
    • SignalR is explicitly handled over WebSocket when available, with reconnect, authorization, group rejoin, event de-duplication and REST state recovery.

1.1 Final Admin Product Decision
CustSearch AI will be treated as a multi-tenant platform.

There are two admin experiences inside the Angular application:
    1. Platform Admin — CustSearch AI owner-side admin.
    2. Customer Admin — each client/tenant's private admin.

The Platform Admin can manage every tenant and see tenant-wise operational/reporting summaries.
The Customer Admin can manage only its own tenant including users, stores, shopper customers, invoices, reports, cameras, alerts and integrations.
All tenant-owned data must be isolated server-side with TenantId.
Real-time updates use ASP.NET Core SignalR over WebSocket (preferred transport), with authenticated server-side tenant/store/user groups, reconnect handling and REST recovery.

2. Final Technology Stack
Backend
    • ASP.NET Core 8 Web API
    • Minimal hosting model (Program.cs)
    • REST API controllers / endpoints
    • SignalR hubs
    • Entity Framework Core
    • Dapper
    • Stored Procedures
    • Redis
    • JWT Authentication + Refresh Tokens
    • Role-Based + Permission-Based Authorization
    • Background Worker Service
    • FluentValidation
    • Serilog
    • Swagger/OpenAPI
    • Health Checks
    • Rate Limiting
Structured logging requirements:
    • Use Serilog with structured message templates, UTC timestamps and environment-specific sink configuration.
    • Enrich API and Worker logs with Application, Environment, CorrelationId, TraceId and, only after authorization, TenantId/UserId/StoreId when applicable.
    • Accept or create a validated correlation ID at the API boundary, return it in the response and propagate it to Dapper, Worker, webhook and Python-service calls.
    • Configure minimum levels and file retention dynamically through appsettings/environment overrides.
    • Never log passwords, JWT/refresh tokens, signing keys, camera credentials, webhook secrets, face embeddings or full sensitive request bodies.
    • Keep audit logs separate from diagnostic logs; diagnostic log retention must not replace business audit retention.
The backend must remain UI-independent. It must not return Razor Views or contain Angular page rendering logic.
Database
    • Microsoft SQL Server 2022
    • Database Name: CustSearch_AI
    • Server: KRUTARTH-BHAVSA
Important Database Rule
Use Entity Framework Core, but:
DO NOT USE EF CORE MIGRATIONS
Do not use:
Add-Migration
Database.Migrate()
Database schema/version changes must be handled through versioned .sql scripts.
AI / CCTV
    • Python
    • FastAPI
    • OpenCV
    • ONNX Runtime
    • NumPy
    • HTTPX
    • Uvicorn
    • RTSP/IP Camera integration
    • Person Detection
    • Person Tracking
    • Face Detection
    • Face Quality Validation
    • Consent-Based Face Recognition
    • Visit Party / Co-Visit Detection
    • Zone Tracking
    • Dwell Time Tracking
    • all camera detect handle
    • Windows Server deployment support for the Python AI service (Windows Service/NSSM or reverse-proxied process; IIS is not the Python runtime itself)
Admin UI — Angular SPA
Use:
    • Angular (stable project-approved version)
    • Angular CLI
    • TypeScript
    • Standalone components and lazy-loaded feature routes
    • Angular Router
    • Angular Reactive Forms
    • Angular Signals + RxJS
    • Angular Material + Angular CDK
    • SCSS
    • Chart.js through an Angular-compatible wrapper/integration
    • @microsoft/signalr JavaScript client
    • JWT access-token handling through Angular HTTP interceptors
    • Route guards for authentication, roles and granular permissions
    • Centralized API error handling
    • Responsive desktop/tablet admin layout
Do NOT use for Admin:
    • ASP.NET Core MVC Views
    • Razor Views
    • jQuery
    • React
    • Next.js
    • Vue
Important:
    • Angular is a separate SPA frontend.
    • CustSearch.API remains the only backend entry point for Admin business operations.
    • Angular must never connect directly to SQL Server, Redis or the Python AI database/state.
    • All business rules remain in the Application/API layers, not inside Angular components.
    • SignalR is used only for real-time UI events; authoritative state must still be reloadable from REST APIs.

3. Repository / Solution Structure
Create:
CustSearch_AI/
│
├── CustSearch_AI.sln
├── README.md
├── docker-compose.yml
├── .env.example
│
├── src/
│   ├── CustSearch.Domain/
│   ├── CustSearch.Contracts/
│   ├── CustSearch.Application/
│   ├── CustSearch.Infrastructure/
│   ├── CustSearch.Integrations/
│   ├── CustSearch.API/
│   ├── CustSearch.Admin/          # Angular SPA (not a .NET project)
│   ├── CustSearch.Worker/
│   └── CustSearch.AI/
│
├── tests/
│   ├── CustSearch.UnitTests/
│   ├── CustSearch.IntegrationTests/
│   ├── CustSearch.Admin.E2E/      # Playwright end-to-end tests
│   └── CustSearch.AI.Tests/
│
├── database/
│   ├── 01_Database/
│   ├── 02_Tables/
│   ├── 03_Indexes/
│   ├── 04_Types/
│   ├── 05_Functions/
│   ├── 06_Views/
│   ├── 07_StoredProcedures/
│   ├── 08_Seed/
│   ├── 09_Upgrade/
│   └── 10_TestData/
│
├── docs/
└── postman/
Add all .NET projects to:
CustSearch_AI.sln
.NET projects inside CustSearch_AI.sln:
    • CustSearch.Domain
    • CustSearch.Contracts
    • CustSearch.Application
    • CustSearch.Infrastructure
    • CustSearch.Integrations
    • CustSearch.API
    • CustSearch.Worker
    • CustSearch.UnitTests
    • CustSearch.IntegrationTests
Angular Admin is kept in the same repository but is not a .csproj and must not be treated as a .NET project:
src/CustSearch.Admin
Python AI project remains inside the same repository:
src/CustSearch.AI
3.1 Angular Admin Project Structure
Create the Angular application in:
src/CustSearch.Admin/
│
├── angular.json
├── package.json
├── package-lock.json
├── tsconfig.json
├── tsconfig.app.json
├── tsconfig.spec.json
├── proxy.conf.json
│
├── public/
│   └── assets/
│
└── src/
    ├── main.ts
    ├── index.html
    ├── styles.scss
    │
    ├── environments/
    │   ├── environment.ts
    │   └── environment.production.ts
    │
    └── app/
        ├── app.config.ts
        ├── app.routes.ts
        │
        ├── core/
        │   ├── auth/
        │   │   ├── auth.service.ts
        │   │   ├── auth.store.ts
        │   │   ├── auth.guard.ts
        │   │   ├── role.guard.ts
        │   │   ├── permission.guard.ts
        │   │   ├── auth.interceptor.ts
        │   │   └── refresh-token.interceptor.ts
        │   │
        │   ├── api/
        │   │   ├── api-client.service.ts
        │   │   ├── api-error.interceptor.ts
        │   │   └── correlation-id.interceptor.ts
        │   │
        │   ├── realtime/
        │   │   ├── signalr.service.ts
        │   │   └── signalr-events.ts
        │   │
        │   ├── layout/
        │   ├── permissions/
        │   ├── notifications/
        │   └── services/
        │
        ├── shared/
        │   ├── components/
        │   ├── directives/
        │   ├── pipes/
        │   ├── models/
        │   ├── validators/
        │   └── utils/
        │
        └── features/
            ├── auth/
            ├── dashboard/
            ├── live-monitoring/
            ├── current-visitors/
            ├── visit-parties/
            ├── visitors/
            ├── customers/
            ├── households/
            ├── customer-preferences/
            ├── household-preferences/
            ├── visits/
            ├── products/
            ├── invoices/
            ├── payments/
            ├── cameras/
            ├── camera-health/
            ├── camera-events/
            ├── recognition-review/
            ├── alerts/
            ├── alert-rules/
            ├── notifications/
            ├── consents/
            ├── biometric-profiles/
            ├── integrations/
            ├── inbound-apis/
            ├── webhooks/
            ├── webhook-deliveries/
            ├── sync-logs/
            ├── reports/
            ├── platform-tenants/
            ├── tenant-dashboard/
            ├── tenant-users/
            ├── tenant-billing/
            ├── tenant-reports/
            ├── staff/
            ├── staff-performance/
            ├── staff-tracking/
            ├── customer-journeys/
            ├── store-categories/
            ├── voice-commands/
            ├── voice-command-audit/
            ├── users/
            ├── roles-permissions/
            ├── audit-logs/
            ├── settings/
            ├── system-health/
            └── demo-tools/
Rules:
    • Use feature-first folders.
    • Lazy-load major feature routes.
    • Keep reusable UI in shared/.
    • Keep singleton infrastructure in core/.
    • Do not put API calls directly inside page components.
    • Do not duplicate backend DTO definitions manually across many features; use a dedicated typed models/contracts layer in Angular.
    • Do not place secrets in environment*.ts; frontend environment files may contain only public configuration such as API base URL.
3.2 Angular Admin Routing
Use lowercase kebab-case routes:
/login

/admin/dashboard
/admin/tenants
/admin/tenants/create
/admin/tenants/:tenantId
/admin/tenants/:tenantId/users
/admin/tenants/:tenantId/stores
/admin/tenants/:tenantId/invoices
/admin/tenants/:tenantId/reports
/admin/tenants/:tenantId/usage
/admin/tenants/:tenantId/audit
/admin/live-monitoring
/admin/current-visitors
/admin/visit-parties
/admin/visitors
/admin/customers
/admin/customers/:id
/admin/households
/admin/households/:id
/admin/customer-preferences
/admin/household-preferences
/admin/visits
/admin/products
/admin/invoices
/admin/invoices/:id
/admin/payments
/admin/cameras
/admin/camera-health
/admin/camera-events
/admin/recognition-review
/admin/alerts
/admin/alert-rules
/admin/notifications
/admin/consents
/admin/biometric-profiles
/admin/integrations
/admin/inbound-apis
/admin/webhooks
/admin/webhooks/create
/admin/webhooks/deliveries
/admin/sync-logs
/admin/reports
/admin/users
/admin/roles-permissions
/admin/audit-logs
/admin/settings
/admin/system-health
/admin/demo-tools

Customer/Tenant Admin routes (same Angular app with tenant-scoped navigation):
/customer-admin/dashboard
/customer-admin/stores
/customer-admin/users
/customer-admin/staff
/customer-admin/staff/:id
/customer-admin/staff-performance
/customer-admin/staff-tracking
/customer-admin/customer-journeys
/customer-admin/store-categories
/customer-admin/voice-commands
/customer-admin/voice-command-audit
/customer-admin/customers
/customer-admin/customers/:id
/customer-admin/households
/customer-admin/visits
/customer-admin/products
/customer-admin/invoices
/customer-admin/invoices/:id
/customer-admin/payments
/customer-admin/reports
/customer-admin/live-monitoring
/customer-admin/cameras
/customer-admin/alerts
/customer-admin/notifications
/customer-admin/integrations
/customer-admin/webhooks
/customer-admin/audit-logs
/customer-admin/settings
/customer-admin/billing
/customer-admin/profile

The sidebar is generated from a route/navigation configuration that includes the required permission for each item. Hidden navigation is only a UX feature; the API must independently enforce every permission.
3.3 Angular Authentication Flow
Recommended production flow:
Angular Login Form
      ↓
POST /api/auth/login
      ↓
ASP.NET validates user
      ↓
Short-lived Access Token
+ Secure Refresh Token
      ↓
Angular loads /api/auth/me
      ↓
User + Roles + Permissions + Assigned Stores
      ↓
Build Admin Navigation
Rules:
    • Prefer short-lived JWT access tokens.
    • Prefer refresh tokens in HttpOnly, Secure, appropriately configured SameSite cookies when deployment topology allows it.
    • Do not keep long-lived refresh tokens in localStorage.
    • Access token attachment is handled by an Angular HTTP interceptor.
    • On 401, use a single-flight refresh strategy so simultaneous API requests do not trigger multiple refresh calls.
    • On refresh failure, clear session state and redirect to /login.
    • On 403, show an Access Denied page; do not silently hide the backend authorization failure.
    • Login/logout/refresh actions must be audited by the backend where appropriate.
Required auth APIs:
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me
GET /api/auth/me should return at minimum:
{
  "userId": 10,
  "userName": "admin",
  "displayName": "Store Admin",
  "roles": ["StoreAdmin"],
  "permissions": ["Customers.View", "Customers.Create"],
  "storeIds": [1],
  "sessionExpiresUtc": "..."
}
3.4 Angular Authorization / Permissions
Angular must support the same granular permission names defined by the backend, for example:
Customers.View
Customers.Create
Customers.Edit
Households.ManageMembers
Invoices.Create
Cameras.Manage
Recognition.Review
Alerts.Configure
Integrations.Manage
Reports.Export
Users.Manage
Roles.Manage
Settings.Manage
AuditLogs.View
Implement:
    • authGuard — user must be authenticated.
    • roleGuard — use only where a role-level rule is genuinely required.
    • permissionGuard — route requires one or more permissions.
    • hasPermission directive/helper — controls button/menu rendering.
    • Backend authorization policies remain authoritative.
Example route behavior:
/admin/customers              → Customers.View
Create Customer button        → Customers.Create
Edit Customer button          → Customers.Edit
/admin/recognition-review     → Recognition.Review
/admin/webhooks               → Webhooks.View
Webhook configuration actions → Webhooks.Manage
3.5 Angular API Client Strategy
Use typed feature services:
CustomerApiService
HouseholdApiService
VisitorApiService
VisitPartyApiService
InvoiceApiService
PaymentApiService
PreferenceApiService
CameraApiService
RecognitionApiService
AlertApiService
NotificationApiService
IntegrationApiService
WebhookApiService
ReportApiService
UserApiService
RolePermissionApiService
SettingsApiService
SystemHealthApiService
DemoApiService
Every list endpoint should use a consistent query model:
pageNumber
pageSize
search
sortBy
sortDirection
filters
Recommended response envelope:
{
  "data": [],
  "pageNumber": 1,
  "pageSize": 25,
  "totalCount": 0,
  "totalPages": 0
}
Do not return raw EF entities directly to Angular. Use API contracts/DTOs from CustSearch.Contracts.
3.6 Angular Forms / Validation
Use Angular Reactive Forms for:
    • Login
    • Customer Create/Edit
    • Household Create/Edit
    • Household Member linking
    • Visitor conversion
    • Invoice creation
    • Payment creation
    • Camera configuration
    • Alert rules
    • Notification settings
    • Consent capture/withdrawal
    • Integration/webhook configuration
    • User management
    • Role/permission management
    • System settings
Validation rules must exist on both sides:
Angular validation = fast user feedback
ASP.NET validation = authoritative security/business validation
Never rely only on frontend validation.
3.7 Angular Real-Time / SignalR
Angular uses the official SignalR JavaScript client to connect to an Admin hub, for example:
/hubs/admin
Handle these events:
VisitorEntered
CustomerEntered
CustomerExited
ReturningCustomerDetected
ReturningHouseholdMemberDetected
VipCustomerDetected
HighValueCustomerDetected
UnknownVisitorDetected
VisitPartyDetected
RecognitionReviewRequired
CameraOnline
CameraOffline
NewAlert
InvoiceCreated
WebhookFailed
DashboardUpdated
Client responsibilities:
    • Start connection after authenticated session is established.
    • Pass/resolve authentication according to the chosen token/cookie model.
    • Automatic reconnect with backoff.
    • Resubscribe to store-specific groups after reconnect.
    • De-duplicate events using event IDs where available.
    • Update Angular Signals/RxJS streams.
    • Show toast/in-app notifications according to permissions.
    • Never treat a SignalR event as the sole permanent data source; refresh detail state from REST APIs when needed.
Server responsibilities:
    • Put users into allowed store/role groups only after authorization.
    • Never broadcast sensitive customer data globally.
    • Publish the minimum event payload needed for the UI.
    • Use event IDs/correlation IDs for troubleshooting and de-duplication.
3.8 Angular State Management
Default approach:
    • Angular Signals for page/session UI state.
    • RxJS for async streams, HTTP orchestration and SignalR streams.
    • Feature services/facades for reusable state.
    • Avoid adding a heavy global state library until there is a demonstrated need.
Global state should be limited to items such as:
Authenticated User
Roles
Permissions
Assigned Stores
Selected Store
Theme/Layout State
Unread Alert Count
Camera Online/Offline Summary
Do not cache sensitive biometric data in browser state longer than required for the active workflow.
3.9 Angular UI / UX Standards
Custom shared design system:
    • Build CustSearch-owned semantic design tokens and reusable Angular components; do not ship an unmodified framework theme.
    • Support explicit Light, Dark and System theme preferences with an accessible visible selector.
    • Customer Admin defaults to the approved clean light direction: white surfaces, deep navy navigation and indigo actions.
    • Platform Admin defaults to the approved premium dark direction: aubergine canvas/surfaces, restrained glass effects and gold accents.
    • Both admin experiences must remain usable in either Light or Dark mode; admin type selects the default, not a permanent restriction.
    • Store only the non-sensitive theme preference in localStorage. Access/refresh tokens and biometric/customer data must never be stored with theme state.
    • Use semantic variables for canvas, surface, navigation, primary, text, border, success, warning, danger, focus and chart colors so feature components never hard-code theme-specific colors.
    • Respond to operating-system color-scheme changes while System mode is active and prevent a wrong-theme flash during startup where practical.
    • Preserve WCAG contrast, visible keyboard focus and non-color status labels in both themes.
Admin shell:
Topbar
Sidebar
Breadcrumbs
Page Title / Actions
Content Area
Global Toasts
Confirmation Dialogs
Loading Indicator
Access Denied Page
Not Found Page
Global Error Page
Use Angular Material/CDK for:
    • Tables
    • Pagination
    • Sorting
    • Dialogs
    • Menus
    • Tooltips
    • Tabs
    • Form fields
    • Date pickers
    • Autocomplete
    • Progress indicators
Use responsive behavior:
    • Desktop: full sidebar + data tables.
    • Tablet: collapsible sidebar + horizontally scrollable table where needed.
    • Mobile: Admin is supported for essential monitoring/actions, but dense operational pages may use card/list layouts instead of forcing desktop tables.
Accessibility:
    • Keyboard-accessible controls.
    • Visible focus states.
    • Semantic labels.
    • Sufficient contrast.
    • Do not use color alone to communicate alert/health status.
3.10 Dashboard / Charts
Angular Dashboard consumes:
GET /api/dashboard/summary
GET /api/dashboard/charts
Prefer one summary API and one chart API rather than many small requests during first load.
Dashboard should support:
    • Initial HTTP load.
    • Incremental SignalR updates.
    • Manual refresh.
    • Store filter.
    • Date/time range where applicable.
    • Loading/skeleton states.
    • Empty states.
    • API error state.
3.11 Angular Error Handling
Standardize backend error responses using Problem Details (application/problem+json) where practical.
Angular global error handling should map:
400 → Validation / bad request
401 → Refresh or login
403 → Access Denied
404 → Not Found
409 → Business conflict / duplicate / idempotency conflict
422 → Domain validation if used
429 → Rate limit message
500 → Generic error + correlation ID
503 → Dependency unavailable / health warning
Never show raw stack traces to Admin users.
3.12 Angular Security Rules
    • Never embed SQL connection strings, camera credentials, webhook secrets or API secrets in Angular.
    • Never trust Angular role/permission checks as security enforcement.
    • Sanitize/avoid unsafe dynamic HTML.
    • Avoid storing sensitive customer/biometric data in browser persistence.
    • Protect against XSS by using Angular binding patterns; avoid bypass-security APIs unless formally reviewed.
    • Configure Content Security Policy at deployment where feasible.
    • Restrict CORS to known origins if Angular and API are hosted on different origins.
    • Prefer same-origin deployment for Admin + API where operationally practical.
    • Use HTTPS only in production.
3.13 Angular Testing Strategy
Unit/component tests:
AuthService
HTTP interceptors
Route guards
Permission checks
Feature services
Critical form validation
SignalR connection/reconnect handling
High-risk page components
End-to-end tests with Playwright:
Admin login/logout
Role/permission access
Customer search/create/edit
Visitor conversion
Household create/link member
Invoice ₹5,000 payer attribution
Customer/household smart profile
Alert rule creation
Live monitoring simulated event
Recognition review
Camera status
Webhook create/delivery retry view
Reports/export authorization
System settings authorization
Demo mode flows
E2E tests must use test/demo data and must not require a physical camera for the default CI pipeline.
3.14 Angular Development Configuration
Development:
Angular: http://localhost:4200
ASP.NET API: configured local HTTPS URL
Python AI: configured local service URL, reachable only through backend integration where possible
Use proxy.conf.json so Angular can call relative paths during development:
/api  → ASP.NET Core API
/hubs → ASP.NET Core SignalR
Angular code should prefer relative URLs in same-origin production deployments.
3.15 Angular Production Deployment — IIS
Recommended same-origin topology:
https://custsearch.example.com/
    /admin/*  → Angular static files
    /api/*    → ASP.NET Core 8 API
    /hubs/*   → ASP.NET Core SignalR
Deployment steps:
1. npm ci
2. npm run build -- --configuration production
3. Publish Angular `dist/` output to IIS static Admin location.
4. Configure SPA fallback/rewrite so `/admin/customers/125` returns Angular `index.html`.
5. Publish ASP.NET Core API separately under `/api` or the configured application path.
6. Enable WebSocket support for SignalR.
7. Use HTTPS certificate.
8. Configure cache headers: long cache for hashed assets, no aggressive cache for `index.html`.
9. Keep runtime secrets only on the server side.
10. Run smoke tests for REST + SignalR + Angular deep links after deployment.
If Admin and API use separate hostnames, configure exact CORS origins and credentials; do not use wildcard CORS with credentials.
3.16 Angular Build Quality Gates
Before a phase is considered complete:
npm ci
npm run lint
npm test -- --watch=false
npm run build -- --configuration production
Also run Playwright E2E for milestone releases.
The Angular build must complete with no TypeScript errors and no broken lazy routes.


3.17 Multi-Tenant / Client Organization Architecture
Terminology is mandatory to avoid confusion:
    • Platform Customer / Client / Tenant = the shop, retailer, business or organization paying for/using CustSearch AI.
    • Shopper Customer = the end consumer detected/registered inside that tenant's store.
    • Platform Admin = CustSearch AI owner/admin who can manage all tenants.
    • Customer Admin / Tenant Admin = admin user belonging to exactly one tenant and allowed to manage only that tenant.

Create root entity:
Tenant
Fields:
    • Id
    • TenantCode (TEN-000001)
    • LegalName
    • DisplayName
    • PrimaryContactName
    • PrimaryEmail
    • PrimaryMobile
    • CountryCode
    • TimeZone
    • CurrencyCode
    • SubscriptionPlanId nullable
    • SubscriptionStatus
    • TrialStartsUtc nullable
    • TrialEndsUtc nullable
    • SubscriptionStartsUtc nullable
    • SubscriptionEndsUtc nullable
    • MaxStores
    • MaxUsers
    • MaxCameras
    • IsActive
    • IsSuspended
    • SuspensionReason nullable
    • CreatedUtc
    • UpdatedUtc

Every tenant-owned business record must contain TenantId directly or be provably reachable through a tenant-owned parent.
For operational clarity and security, add TenantId directly to high-value/high-volume root tables such as:
    • Stores
    • Users
    • Customers
    • AnonymousVisitors
    • Households
    • VisitParties
    • CustomerVisits
    • Products
    • ProductCategories
    • Invoices
    • InvoicePayments
    • Cameras
    • Alerts
    • Notifications
    • Integrations
    • WebhookEndpoints
    • AuditLogs

Mandatory tenant rule:
No Customer Admin request may choose an arbitrary TenantId from the browser and trust it.
The backend resolves allowed tenant scope from authenticated claims/session and authorization rules.
Platform Admin APIs may accept explicit tenant filters because Platform Admin is cross-tenant by design.

3.18 Admin Hierarchy
Level 1 — Platform Admin
CustSearch AI owner-side control panel.
Responsibilities:
    • Create tenant/client account.
    • Edit tenant profile.
    • Activate/suspend tenant.
    • Reset/invite Tenant Admin.
    • Assign subscription plan.
    • Configure tenant quotas.
    • View tenant stores/users/cameras.
    • View tenant operational summary.
    • View tenant shopper counts.
    • View tenant invoice/sales summary.
    • View platform billing invoices.
    • View tenant usage.
    • View tenant health.
    • View tenant audit.
    • Impersonation/support access only through explicit audited support workflow if enabled.

Level 2 — Customer Admin / Tenant Admin
Client-side admin panel.
Responsibilities:
    • Manage own stores.
    • Add/edit/deactivate own staff users.
    • Assign allowed tenant roles/permissions.
    • Manage own shopper customers.
    • Manage households/families.
    • Manage visits and visit parties.
    • Manage products/categories.
    • View/create operational invoices and payments if permitted.
    • View customer purchase history.
    • View reports for own tenant.
    • Manage cameras and zones if permitted.
    • View live monitoring.
    • Manage alerts/notifications.
    • Manage integrations/webhooks.
    • View own audit logs.
    • View own plan, limits and platform billing invoices.
    • Never access another tenant.

Level 3 — Tenant Staff Users
Examples:
    • TenantManager
    • TenantCRMStaff
    • TenantBillingStaff
    • TenantCameraOperator
    • TenantIntegrationAdmin
    • TenantAuditor
Access remains permission based and tenant scoped.

3.19 Platform Admin Panel — Final Pages
Routes:
/admin/dashboard
/admin/tenants
/admin/tenants/create
/admin/tenants/:tenantId
/admin/tenants/:tenantId/overview
/admin/tenants/:tenantId/stores
/admin/tenants/:tenantId/users
/admin/tenants/:tenantId/customers
/admin/tenants/:tenantId/invoices
/admin/tenants/:tenantId/reports
/admin/tenants/:tenantId/cameras
/admin/tenants/:tenantId/integrations
/admin/tenants/:tenantId/audit
/admin/tenants/:tenantId/usage
/admin/subscription-plans
/admin/platform-invoices
/admin/platform-payments
/admin/platform-reports
/admin/system-health
/admin/audit-logs
/admin/settings

Platform Dashboard cards:
    • Total Tenants
    • Active Tenants
    • Trial Tenants
    • Suspended Tenants
    • Total Stores
    • Total Tenant Users
    • Total Shopper Customers
    • Total Cameras
    • Online Cameras
    • Offline Cameras
    • Today's Platform-Wide Visits
    • Today's Platform-Wide Retail Sales
    • Current WebSocket Connections
    • Failed Webhooks
    • Open Recognition Reviews
    • Platform Billing Due
    • Monthly Recurring Revenue if subscription billing is enabled

Platform Dashboard charts:
    • Tenants created by month
    • Active tenants trend
    • Usage by tenant
    • Visits by tenant
    • Retail sales by tenant
    • Camera count/health by tenant
    • Webhook failures by tenant
    • Alert volume by tenant
    • Platform invoice collection trend

Tenant list columns:
    • Tenant Code
    • Business Name
    • Primary Contact
    • Plan
    • Stores
    • Users
    • Cameras
    • Shopper Customers
    • Status
    • Subscription Status
    • Last Activity
    • Actions

Tenant detail tabs:
    • Overview
    • Stores
    • Users
    • Shopper Customers
    • Households
    • Visits
    • Retail Invoices
    • Reports
    • Cameras
    • Alerts
    • Integrations
    • Webhook Deliveries
    • Usage
    • Platform Billing
    • Audit Logs
    • Settings

3.20 Customer Admin Panel — Final Pages
Customer Admin sidebar should be tenant-focused, not platform-focused:
Dashboard
Stores
Users
Staff
Staff Performance
Staff Tracking
Store Categories
Voice Commands
Voice Command Audit
Customer Journeys
Customers
Households / Families
Visits
Visit Parties
Products
Retail Invoices
Payments
Customer Preferences
Household Preferences
Live Monitoring
Current Visitors
Cameras
Camera Health
Recognition Review
Alerts
Alert Rules
Notifications
Integrations
Webhooks
Webhook Deliveries
Reports
Audit Logs
Plan & Billing
Settings
My Profile

Customer Admin must not see:
    • Other tenants.
    • Platform-wide settings.
    • Platform-wide billing.
    • Other tenants' users/customers/invoices/reports.
    • Global secrets.
    • Global system configuration unless explicitly exposed as read-only health information.

3.21 Customer Admin Dashboard
Customer Admin dashboard filters:
    • Current tenant is fixed from authenticated scope.
    • Store selector: All Allowed Stores or one store.
    • Date selector: Today / Yesterday / Last 7 Days / Last 30 Days / Custom.

Cards:
    • People Inside
    • Known Customers Inside
    • Unknown Visitors Inside
    • Households Inside
    • Today's Visits
    • Returning Customers
    • New Customers
    • Today's Retail Sales
    • Today's Payments
    • Average Order Value
    • High Value Customers
    • Active Cameras
    • Offline Cameras
    • Open Alerts
    • Recognition Reviews
    • Webhook Failures

Charts:
    • Hourly Visitor Trend
    • Daily Visitor Trend
    • New vs Returning Customers
    • Known vs Unknown
    • Sales Trend
    • Payment Method Split
    • Top Product Categories
    • Top Customer Preferences
    • Household Activity
    • Camera Detection Activity
    • Alert Trend

Recent panels:
    • Latest recognized customers
    • Latest unknown visitors
    • Latest retail invoices
    • Latest alerts
    • Camera health changes
    • Failed integration/webhook deliveries

3.22 Customer Admin — User Management
Tenant Admin can create staff only inside its own tenant.

Create/Update user fields:
    • TenantId (server assigned, not user-editable for tenant admin)
    • UserCode
    • FirstName
    • LastName
    • DisplayName
    • Email
    • Mobile
    • UserName
    • RoleIds
    • StoreIds / AllStores flag
    • IsActive
    • MustChangePassword
    • LastLoginUtc
    • CreatedUtc
    • UpdatedUtc

Actions:
    • Invite User
    • Create User
    • Edit User
    • Activate
    • Deactivate
    • Reset Password / Send Reset Link
    • Assign Role
    • Assign Stores
    • Revoke Sessions
    • View Login History
    • View Audit History

Rules:
    • TenantAdmin cannot create PlatformSuperAdmin.
    • TenantAdmin cannot grant permissions that it does not possess unless a platform-defined role template explicitly allows delegated administration.
    • A tenant user can only be assigned stores belonging to the same tenant.
    • Disabled/suspended tenant blocks all tenant user login except approved support/system behavior.

3.23 Tenant Store Management
Every Store must belong to one Tenant.
Store fields add:
    • TenantId
    • StoreCode
    • StoreName
    • AddressLine1
    • AddressLine2 nullable
    • Landmark nullable
    • City
    • District nullable
    • StateOrProvince
    • PostalCode
    • CountryCode
    • Latitude nullable (decimal, valid range -90 to 90)
    • Longitude nullable (decimal, valid range -180 to 180)
    • GeoFenceRadiusMeters nullable
    • ExternalPlaceId nullable
    • LocationSource (Manual, MapPin, Geocoded, Imported)
    • IsLocationVerified
    • LocationVerifiedUtc nullable
    • LocationVerifiedByUserId nullable
    • TimeZone
    • ContactEmail
    • ContactMobile
    • IsActive

Store location rules:
    • A Store has one canonical physical location; zones and cameras remain child records within that Store.
    • Address-only entry is allowed. Latitude and Longitude must either both be present or both be absent.
    • Coordinates, geofence radius and time zone are server validated and tenant scoped.
    • Tenant users can view/edit a Store location only with the matching Store permission and authenticated TenantId.
    • Location changes create an audit record with old/new values, actor and correlation ID.
    • Map/geocoding provider integration is optional and must keep provider keys in server-side secrets, never Angular configuration.
    • Store-local reporting and camera timestamps use the configured Store time zone while persistence remains UTC.

Customer Admin store page:
    • View stores
    • Create store subject to plan quota
    • Edit store
    • Activate/deactivate store
    • Assign users
    • Assign cameras
    • Configure zones
    • Configure store notification recipients
    • View store-specific reports

3.24 Invoice Architecture — Two Different Invoice Types
Do not mix these two concepts.

A. Retail Invoice
Meaning:
Shopper purchase invoice inside a tenant store.
Existing Invoice / InvoiceItem / InvoicePayment / InvoiceParticipant model continues to represent this.
Required ownership fields:
    • TenantId
    • StoreId
    • Shopper CustomerId
    • HouseholdId nullable
    • PayerCustomerId
    • VisitId nullable
    • VisitPartyId nullable

Customer Admin can:
    • Search retail invoices.
    • Filter by store/customer/household/date/payment/status.
    • View details/items/payments.
    • Create/manual import when permitted.
    • Export invoice report.
    • Link invoice to customer/household where business rules allow.
    • Correct attribution only through audited authorized workflow.

Platform Admin can:
    • View tenant retail invoice summary.
    • Drill into tenant retail invoices only with explicit Platform permission and audit.
    • Report tenant-wise sales totals.

B. Platform Billing Invoice
Meaning:
Invoice generated by CustSearch AI platform to the tenant/client for subscription/license/setup/usage.
Create separate tables:
PlatformInvoice
PlatformInvoiceItem
PlatformPayment

PlatformInvoice fields:
    • Id
    • InvoiceNumber
    • TenantId
    • SubscriptionId nullable
    • InvoiceDateUtc
    • DueDateUtc
    • CurrencyCode
    • Subtotal
    • TaxAmount
    • DiscountAmount
    • FinalAmount
    • PaidAmount
    • BalanceAmount
    • Status (Draft/Issued/PartiallyPaid/Paid/Overdue/Void)
    • BillingPeriodStartUtc nullable
    • BillingPeriodEndUtc nullable
    • Notes
    • CreatedUtc
    • UpdatedUtc

Tenant Customer Admin can only view/download its own Platform Billing invoices and payment status.
Platform Admin can create/issue/void/record payment subject to permissions.

3.25 Customer / Tenant Wise Reports
Reports must support Platform scope and Tenant scope separately.

Platform reports:
    • Tenant Master Report
    • Tenant Status Report
    • Tenant Usage Report
    • Tenant Store Count Report
    • Tenant User Count Report
    • Tenant Camera Count/Health Report
    • Tenant Shopper Count Report
    • Tenant Visit Summary Report
    • Tenant Retail Sales Summary Report
    • Tenant Webhook Failure Report
    • Tenant Alert Summary Report
    • Platform Billing Invoice Report
    • Platform Payment Collection Report
    • Subscription Expiry Report
    • Tenant Audit Activity Report

Tenant Customer Admin reports:
    • Daily Visitor Report
    • Current Visitors Report
    • New Customer Report
    • Returning Customer Report
    • Household Visit Report
    • Retail Sales Report
    • Retail Invoice Report
    • Payment Report
    • Personal Spend Report
    • Household Spend Report
    • Product Sales Report
    • Product Category Report
    • Customer Preference Report
    • Household Preference Report
    • High Value Customer Report
    • VIP Customer Report
    • Camera Health Report
    • Recognition Report
    • Alert Report
    • Webhook Delivery Report
    • Integration Sync Report
    • User Activity Report
    • Audit Report

Common filters:
    • Tenant (Platform Admin only)
    • Store
    • User
    • Shopper Customer
    • Household
    • Date From
    • Date To
    • Status
    • Payment Method
    • Category
    • Camera

Export:
    • CSV
    • Excel
    • PDF when required

Large export rule:
Do not generate extremely large reports synchronously in the HTTP request.
Create ReportExportJob and process with Worker.
Notify requester when ready using SignalR/WebSocket event ReportExportReady.
Use signed/authorized temporary download endpoint.

3.26 Report Export Job
Create:
ReportExportJob
Fields:
    • Id
    • TenantId nullable for platform-level report
    • RequestedByUserId
    • ReportType
    • FilterJson
    • Format
    • Status
    • ProgressPercent
    • StorageReference
    • ErrorMessage
    • RequestedUtc
    • StartedUtc nullable
    • CompletedUtc nullable
    • ExpiresUtc nullable

Statuses:
Queued
Processing
Completed
Failed
Expired

WebSocket events:
ReportExportQueued
ReportExportProgress
ReportExportReady
ReportExportFailed

3.27 Tenant Subscription / Plan Management
Create:
SubscriptionPlan
TenantSubscription
TenantUsageSnapshot

SubscriptionPlan fields:
    • Id
    • PlanCode
    • PlanName
    • MonthlyPrice
    • AnnualPrice nullable
    • MaxStores
    • MaxUsers
    • MaxCameras
    • MaxMonthlyRecognitions nullable
    • MaxMonthlyApiCalls nullable
    • IsActive

TenantSubscription fields:
    • Id
    • TenantId
    • SubscriptionPlanId
    • BillingCycle
    • Status
    • StartsUtc
    • EndsUtc nullable
    • AutoRenew
    • CreatedUtc
    • UpdatedUtc

Quota behavior:
    • Validate on backend before creating store/user/camera.
    • Customer Admin dashboard shows current usage vs limits.
    • Platform Admin may override limits with audit reason.
    • Do not rely on Angular only for quota enforcement.

3.28 WebSocket / SignalR — Production Real-Time Architecture
SignalR is the application framework; WebSocket is the preferred real-time transport.
Use:
    • ASP.NET Core SignalR server
    • @microsoft/signalr Angular client
    • WebSockets enabled on IIS
    • Long Polling fallback only when WebSocket cannot be established and fallback is intentionally allowed

Recommended hubs:
/hubs/admin
/hubs/tenant

Option A (recommended): one authorized /hubs/realtime hub with server-managed groups.
Groups:
    • platform-admins
    • tenant:{tenantId}
    • tenant:{tenantId}:store:{storeId}
    • tenant:{tenantId}:role:{roleName} only if actually needed
    • user:{userId}

Never allow the client to freely join arbitrary tenant groups.
Server determines group membership from authenticated user claims and allowed store assignments.

3.29 WebSocket Connection Lifecycle
Angular RealtimeService responsibilities:
    1. Do not connect before authenticated session is established.
    2. Build HubConnection once per authenticated browser session.
    3. Start using WebSockets preferred.
    4. Register event handlers before start or in a deterministic bootstrap sequence.
    5. Update ConnectionState Signal:
       Disconnected / Connecting / Connected / Reconnecting / Failed.
    6. Handle onreconnecting.
    7. Handle onreconnected.
    8. Handle onclose.
    9. Reconnect with bounded exponential backoff + jitter.
    10. After reconnect, server revalidates identity and rejoins authorized groups.
    11. Reload authoritative dashboard/live-monitoring state from REST.
    12. De-duplicate replayed events by EventId.
    13. Stop connection on logout/session invalidation.
    14. Clear tenant-sensitive in-memory streams on logout or tenant context change.

Suggested reconnect delays:
0 sec
2 sec
5 sec
10 sec
30 sec
Then continue controlled retries or surface offline state according to application policy.

3.30 Real-Time Event Envelope
All important real-time messages should use a common envelope:
{
  "eventId": "evt_...",
  "eventType": "CustomerEntered",
  "occurredUtc": "2026-08-15T10:00:00Z",
  "tenantId": 25,
  "storeId": 10,
  "correlationId": "...",
  "data": {}
}

Rules:
    • EventId must be unique enough for de-duplication.
    • Include TenantId/StoreId for server/client routing, but never trust client-side filtering as authorization.
    • Keep payload minimal.
    • Do not broadcast full biometric profiles or secrets.
    • Detail pages fetch full authorized data through REST.

3.31 Real-Time Event Catalog
Platform events:
TenantCreated
TenantUpdated
TenantActivated
TenantSuspended
TenantSubscriptionChanged
TenantUsageThresholdReached
PlatformInvoiceIssued
PlatformInvoicePaid
TenantHealthChanged

Tenant operational events:
VisitorEntered
VisitorExited
CustomerEntered
CustomerExited
ReturningCustomerDetected
ReturningHouseholdMemberDetected
VipCustomerDetected
HighValueCustomerDetected
UnknownVisitorDetected
VisitPartyDetected
RecognitionReviewRequired
CameraOnline
CameraOffline
CameraHealthChanged
NewAlert
AlertAcknowledged
InvoiceCreated
InvoiceUpdated
PaymentCreated
CustomerCreated
CustomerUpdated
HouseholdUpdated
WebhookFailed
WebhookDelivered
DashboardUpdated
ReportExportQueued
ReportExportProgress
ReportExportReady
ReportExportFailed

User/security events where appropriate:
UserSessionRevoked
PermissionChanged
TenantSuspended
ForceLogout

3.32 Reliable Real-Time Publishing
Business transaction should not depend on the browser being connected.
Recommended pattern for critical domain events:
Database transaction
    ↓
Persist business data
    ↓
Persist Domain/Integration Event or Outbox record
    ↓
Commit
    ↓
Worker/Event Dispatcher
    ↓
SignalR publish + webhook queue where applicable

Create optional OutboxMessage table for critical reliable events:
    • Id
    • TenantId nullable
    • EventId
    • EventType
    • Payload
    • OccurredUtc
    • ProcessedUtc nullable
    • RetryCount
    • LastError

Benefits:
    • Avoid losing event after DB commit but before SignalR/webhook publish.
    • Supports retry.
    • Supports audit/troubleshooting.

3.33 WebSocket Scaling / Redis Backplane
Single server:
SignalR works directly.

Multiple API servers:
Use Redis backplane or Azure SignalR Service if deployment later scales horizontally.
Because Redis already exists in architecture, Redis backplane is a natural self-hosted option.

Rules:
    • Keep connection-specific temporary state out of local static memory where it breaks multi-node behavior.
    • Authorization still happens on every connection.
    • Store presence/connection metadata only if business requirement needs it.
    • Do not assume WebSocket connection itself proves user is online forever.

3.34 WebSocket Security
    • HTTPS/WSS only in production.
    • Authenticate hub connection.
    • Authorize tenant/store groups server-side.
    • Reject suspended tenant sessions.
    • Revalidate sensitive actions over REST; Hub is primarily server-to-client notification unless a specific secured client method is needed.
    • Rate-limit client-to-hub methods if exposed.
    • Validate message inputs.
    • Never accept TenantId group join request without server authorization.
    • Never send face embeddings, passwords, tokens, webhook secrets or camera passwords through SignalR.
    • Audit administrative real-time actions when applicable.

3.35 WebSocket Monitoring
System Health should show:
    • Current Hub Connections
    • Platform Admin Connections
    • Tenant Connections
    • Connections by Tenant
    • Reconnect Rate
    • Connection Failures
    • Messages Published
    • Messages Failed
    • Redis Backplane Health if enabled

Metrics/log fields:
    • ConnectionId (do not treat as user identity)
    • UserId
    • TenantId
    • StoreIds
    • ConnectedUtc
    • DisconnectedUtc
    • DisconnectReason category
    • CorrelationId where applicable

3.36 Tenant Isolation — Database and Query Rules
Every repository/service must receive an authorized tenant context.
Create:
ICurrentTenant
CurrentTenant
ITenantScopeValidator

CurrentTenant should expose:
    • TenantId nullable for Platform Admin cross-tenant context
    • IsPlatformAdmin
    • AllowedStoreIds
    • UserId

EF Core strategy:
Use explicit TenantId predicates in repositories/services for critical write paths.
Global query filters may be used carefully as defense-in-depth, but must not be the only tenant security mechanism, especially for Platform Admin operations/background jobs.

Dapper / Stored Procedure strategy:
Every tenant-owned SP must accept @TenantId unless the procedure is explicitly platform-level.
Example:
dbo.Customer_Search
    @TenantId
    @StoreId
    @SearchText
    @PageNumber
    @PageSize

Never write a tenant-owned SP that searches all tenants and relies on Angular to filter results.

Unique indexes should normally include TenantId where business keys are tenant-specific, for example:
    • UNIQUE(TenantId, CustomerCode)
    • UNIQUE(TenantId, HouseholdCode)
    • UNIQUE(TenantId, InvoiceNumber)
    • UNIQUE(TenantId, StoreCode)

3.37 Tenant-Aware Audit Logging
AuditLogs add:
    • TenantId nullable
    • StoreId nullable
    • UserId
    • ActorType
    • Action
    • EntityType
    • EntityId
    • BeforeJson masked as needed
    • AfterJson masked as needed
    • IpAddress
    • UserAgent
    • CorrelationId
    • CreatedUtc

Platform Admin cross-tenant actions must log target TenantId.
Support/impersonation access, if implemented, must create explicit audit records and visible support-session banner.

3.38 Tenant-Aware API Design
Platform APIs:
GET    /api/platform/tenants
POST   /api/platform/tenants
GET    /api/platform/tenants/{tenantId}
PUT    /api/platform/tenants/{tenantId}
POST   /api/platform/tenants/{tenantId}/activate
POST   /api/platform/tenants/{tenantId}/suspend
GET    /api/platform/tenants/{tenantId}/summary
GET    /api/platform/tenants/{tenantId}/reports/summary
GET    /api/platform/tenants/{tenantId}/usage
GET    /api/platform/invoices
POST   /api/platform/invoices

Tenant APIs should normally resolve tenant from login context:
GET    /api/tenant/dashboard/summary
GET    /api/tenant/dashboard/charts
GET    /api/tenant/users
POST   /api/tenant/users
GET    /api/tenant/stores
POST   /api/tenant/stores
GET    /api/customers
GET    /api/invoices
GET    /api/reports/...

Do not expose /api/tenant/{tenantId}/... to ordinary Tenant Admin unless there is a strong reason; this reduces accidental cross-tenant parameter misuse.

3.39 Authentication Claims for Multi-Tenant Admin
GET /api/auth/me should additionally return:
{
  "userId": 10,
  "userType": "TenantUser",
  "tenantId": 25,
  "tenantCode": "TEN-000025",
  "tenantName": "ABC Retail",
  "roles": ["TenantAdmin"],
  "permissions": ["Customers.View", "Reports.View"],
  "storeIds": [1,2],
  "isPlatformAdmin": false,
  "sessionExpiresUtc": "..."
}

Platform Admin example:
{
  "userId": 1,
  "userType": "PlatformUser",
  "tenantId": null,
  "roles": ["PlatformSuperAdmin"],
  "isPlatformAdmin": true
}

Never authorize from tenantId sent in localStorage/query string alone.

3.40 Customer Admin Branding / Profile
Optional tenant settings:
    • Logo
    • Business Display Name
    • Support Email
    • Time Zone
    • Currency
    • Date Format
    • Default Store
    • Notification Defaults

Do not allow arbitrary executable HTML/JavaScript branding.


3.41 Shop Owner + Staff Operating Model

Add a dedicated tenant-side operating hierarchy without creating a separate security boundary:

```text
Tenant / Client
   ↓
ShopOwner / TenantOwner
   ↓
StoreManager
   ↓
SalesStaff / CRMStaff / BillingStaff / CameraOperator
   ↓
Assigned Store(s)
```

Recommended tenant roles:

- `TenantOwner` / `ShopOwner`
- `StoreManager`
- `SalesStaff`
- `CRMStaff`
- `BillingStaff`
- `CameraOperator`
- existing integration/auditor roles where required

`ShopOwner` can, subject to tenant permissions:

- create/edit/deactivate staff
- assign stores and role templates
- configure store categories
- configure the store's dynamic voice keyword
- view staff live status
- view staff-customer interactions
- view staff performance and conversion reports
- configure customer/staff tracking settings
- configure family/group tracking settings
- view customer dwell-time and interest analytics
- configure notification behavior

Staff is still stored as an authenticated tenant `User`. Add `StaffProfile` for staff-specific operational metadata instead of creating an unrelated login system.

### 3.41.1 StaffProfile

Create:

```text
StaffProfile
```

Fields:

- Id
- TenantId
- UserId
- StaffCode
- PrimaryStoreId nullable
- JobTitle
- Department
- IsSalesStaff
- TrackingEnabled
- VoiceCommandEnabled
- CanReceiveCustomerInsights
- IsActive
- CreatedUtc
- UpdatedUtc

Optional staff tracking enrollment is separate from shopper biometric enrollment and must follow the configured workplace policy/notice/consent requirements.

### 3.41.2 Staff Shifts / Presence

Create:

```text
StaffShift
StaffPresenceSession
```

`StaffShift` fields:

- Id
- TenantId
- StoreId
- StaffUserId
- ScheduledStartUtc nullable
- ScheduledEndUtc nullable
- ActualStartUtc nullable
- ActualEndUtc nullable
- Status
- CreatedUtc

`StaffPresenceSession` fields:

- Id
- TenantId
- StoreId
- StaffUserId
- EntryUtc
- ExitUtc nullable
- PresenceSource (`Login`, `Manual`, `CCTV`, `Badge`, `Combined`)
- TrackingSessionId nullable
- CreatedUtc

Do not use camera presence as the sole payroll/attendance authority unless the tenant explicitly designs and validates that separate workflow.

---

3.42 Dynamic Store-Wise Voice Trigger Keyword

The phrase `Aasha Add` is only the default example. **It must never be hard-coded.**

Each store can configure its own trigger, for example:

```text
Store A → Aasha Add
Store B → Magic Add
Store C → Smart Add
Store D → Apna Add
```

Configuration hierarchy:

```text
Platform Default
      ↓
Tenant Default
      ↓
Store Override   ← highest priority for a store
```

Create:

```text
StoreVoiceCommandSetting
StoreVoiceCommandAlias
StaffVoiceCommand
```

### StoreVoiceCommandSetting fields

- Id
- TenantId
- StoreId
- IsEnabled
- TriggerKeyword
- NormalizedTriggerKeyword
- LanguageCode
- RequireCustomerConfirmation
- RequireCategoryConfirmationOnAmbiguity
- CommandSessionTimeoutSeconds
- AllowCategoryAutoCreate
- ResponseMode (`InApp`, `Toast`, `Voice`, `InAppAndVoice`)
- IsActive
- VersionNumber
- UpdatedByUserId
- UpdatedUtc

Recommended defaults:

```text
TriggerKeyword = "Aasha Add"
IsEnabled = true
RequireCustomerConfirmation = false when exactly one active customer interaction exists
RequireCategoryConfirmationOnAmbiguity = true
AllowCategoryAutoCreate = false
CommandSessionTimeoutSeconds = 30
```

### StoreVoiceCommandAlias fields

- Id
- TenantId
- StoreId
- StoreVoiceCommandSettingId
- AliasText
- NormalizedAliasText
- LanguageCode
- IsActive

Examples:

```text
Aasha Add
Asha Add
Aasha Category
Add Interest
```

Aliases are store scoped. A phrase valid in one store must not automatically become valid in another store.

### Dynamic keyword update flow

```text
ShopOwner opens Store Settings
        ↓
Voice Commands
        ↓
Changes "Aasha Add" → "Magic Add"
        ↓
ASP.NET validates + saves
        ↓
Audit Log
        ↓
Outbox Event
        ↓
SignalR/WebSocket
        ↓
StoreVoiceKeywordChanged
        ↓
Authorized staff clients refresh command configuration
```

Real-time event:

```text
StoreVoiceKeywordChanged
```

Payload should contain only store/config version metadata needed to refresh settings, not sensitive staff/customer data.

---

3.43 Store Product Category / Interest Taxonomy

Voice tagging must use a clean store category structure.

Use existing:

```text
ProductCategories
Products
```

Extend category support with:

```text
ProductCategoryAlias
```

Recommended category fields/behavior:

- TenantId
- CategoryCode
- CategoryName
- ParentCategoryId nullable
- NormalizedName
- IsActive
- SortOrder

Alias fields:

- Id
- TenantId
- StoreId nullable
- ProductCategoryId
- AliasText
- NormalizedAliasText
- LanguageCode
- IsActive

Example taxonomy:

```text
Saree
 ├── Banarasi Saree
 ├── Silk Saree
 ├── Cotton Saree
 └── Designer Saree
```

Aliases may include:

```text
Banarasi Sadi → Banarasi Saree
Banarsi Saree → Banarasi Saree
Banarasi → Banarasi Saree
```

Do not let the voice parser create random category text directly in the customer profile.

---

3.44 "Aasha Add" / Dynamic Voice Command Flow

Example store configuration:

```text
Store: Surat Main Store
Trigger Keyword: Aasha Add
Staff: STAFF-00025 / Ravi
Current Customer: CUST-00125 / Priya
Current Staff Interaction: SCI-000900
```

Staff is showing a Banarasi saree and says:

```text
Aasha Add Banarasi Saree
```

Flow:

```text
Staff authenticated session
        ↓
Resolve current Store
        ↓
Load current StoreVoiceCommandSetting
        ↓
Match dynamic trigger keyword / alias
        ↓
Resolve Staff identity
        ↓
Resolve active StaffCustomerInteraction
        ↓
Resolve exactly one target customer
        ↓
Parse "Banarasi Saree"
        ↓
Match Store Product Category / Alias
        ↓
Create CustomerPreferenceSignal
Source = StaffVoiceTag
        ↓
Recalculate Customer Interest Profile
        ↓
Return concise Staff Insight
        ↓
Audit + SignalR update
```

Create preference signal example:

```text
CustomerId: CUST-00125
PreferenceType: CategoryInterest
ReferenceType: ProductCategory
Reference: Banarasi Saree
Source: StaffVoiceTag
StaffUserId: STAFF-00025
InteractionId: SCI-000900
Reason: "Staff voice command while showing Banarasi Saree"
```

**Important:** this command indicates an observed/assisted interest signal. It does not mean the customer purchased the category.

### Ambiguous customer rule

If staff is simultaneously handling multiple customers and the active customer cannot be resolved confidently:

```text
Do NOT write preference
        ↓
Show customer selection/confirmation
        ↓
Staff selects target customer
        ↓
Then apply tag
```

### Ambiguous category rule

If staff says:

```text
Aasha Add Party Saree
```

and several categories match:

```text
Party Wear Saree
Designer Saree
Wedding Saree
```

show confirmation rather than silently choosing.

### Unknown category rule

If no category/alias matches:

```text
"Category not found"
```

Default action:

- do not create category
- allow ShopOwner/authorized category manager to add alias/category

Optional setting `AllowCategoryAutoCreate` stays OFF by default. If enabled, creation still requires `StoreCategories.Manage` and an audit record.

---

3.45 Staff Insight Returned After Voice Tag

After a successful tag, return a concise staff notification.

Example:

```text
Interest Added
Customer: Priya
Category: Banarasi Saree
Interest Score: 78 / 100

Top Current Interests:
1. Banarasi Saree — 78
2. Silk Saree — 71
3. Ethnic Wear — 66

Last Related Purchase:
Silk Saree — 14 Jun 2026

Recommended Context:
Customer frequently engages with Saree / Ethnic Wear.
```

This response may be shown as:

- in-app toast
- staff customer panel
- optional voice response
- optional handheld/POS notification

Do not expose unrelated sensitive customer data in the voice response.

SignalR events:

```text
CustomerInterestTagged
CustomerPreferenceUpdated
StaffInsightReady
```

---

3.46 Staff ↔ Customer Interaction Tracking

Create:

```text
StaffCustomerInteraction
StaffCustomerInteractionEvent
```

### StaffCustomerInteraction fields

- Id
- TenantId
- StoreId
- StaffUserId
- CustomerId nullable
- AnonymousVisitorId nullable
- CustomerVisitId nullable
- VisitPartyId nullable
- StartedUtc
- EndedUtc nullable
- DurationSeconds nullable
- StartSource (`Manual`, `Voice`, `CCTV`, `POS`, `Combined`)
- EndReason
- PrimaryCategoryId nullable
- ConvertedToInvoice
- ConvertedInvoiceId nullable
- AssistedRevenue nullable
- ConversionConfidence nullable
- Status
- CreatedUtc

### StaffCustomerInteractionEvent fields

- Id
- TenantId
- StoreId
- InteractionId
- EventType
- CustomerId nullable
- StaffUserId
- CategoryId nullable
- ProductId nullable
- Source
- EventUtc
- MetadataJson masked/minimized

Event types may include:

```text
InteractionStarted
ProductShown
CategoryDiscussed
VoiceInterestTagged
ZoneVisitedTogether
CustomerQuestion
BillingHandoff
InteractionEnded
InvoiceConverted
```

The system may use CCTV proximity as supporting evidence that staff and customer were interacting, but it must not treat physical proximity alone as proof of a sales conversation.

---

3.47 Staff + Customer CCTV Tracking Model

Track customer and staff separately.

```text
PersonTrackingSession
 ├── Shopper Customer / Anonymous Visitor
 └── Staff User
```

Create/extend:

```text
PersonTrackingSession
StaffTrackingSession
StaffZoneVisit
CustomerDwellSession
```

Person role:

```text
Customer
AnonymousVisitor
Staff
Unknown
```

Customer flow:

```text
Entrance
 ↓
Person Tracking ID
 ↓
Customer / Anonymous Visitor Session
 ↓
CustomerVisit
 ↓
Zone Visits
 ↓
Staff Interactions
 ↓
Billing / No Billing
 ↓
Exit
```

Staff flow:

```text
Shift / Presence Start
 ↓
Staff Tracking Identity
 ↓
Zones Visited
 ↓
Customer Interactions
 ↓
Assisted Conversions
 ↓
Presence End
```

Do not merge staff presence statistics into visitor/customer counts.

---

3.48 Customer Total Shop Dwell Time

For every customer/anonymous visitor visit calculate:

```text
EntryTimeUtc
ExitTimeUtc
TotalDwellSeconds
```

Example:

```text
Customer entered: 4:10 PM
Customer exited: 5:02 PM
Total shop dwell time: 52 minutes
```

Create/extend `CustomerDwellSession`:

- Id
- TenantId
- StoreId
- CustomerVisitId
- CustomerId nullable
- AnonymousVisitorId nullable
- EntryUtc
- ExitUtc nullable
- TotalDwellSeconds nullable
- TrackingQuality
- CreatedUtc

Handle temporary tracking loss with session continuation/cooldown logic so a customer is not counted as multiple separate visits due to short occlusion.

---

3.49 Customer Zone / Category Interest Analytics

Existing CCTV zone tracking becomes a complete customer journey signal.

Example:

```text
Total Visit: 52 minutes

Entrance          2 min
Banarasi Saree   18 min
Silk Saree       11 min
Footwear          4 min
Accessories       6 min
Billing           8 min
Other             3 min
```

System can infer a **behavioral interest signal** such as:

```text
Strong Zone Interest:
Banarasi Saree

Supporting Signals:
18 min dwell time
2 repeat entries into zone
Staff voice tag: Banarasi Saree
Product shown by Ravi
```

Interest scoring should combine evidence, for example:

```text
Purchase                     50%
Repeat Purchase              20%
Explicit Customer Preference 10%
Staff Voice / Manual Tag      8%
Staff Product Interaction     5%
Zone Dwell Time               4%
Repeat Zone Visit             3%
```

Weights are configurable. CCTV interest alone remains lower-confidence than a purchase or explicit preference.

---

3.50 Staff Assisted Conversion Attribution

A conversion must have an explainable rule.

Default example:

```text
Staff interacted with Customer
        ↓
Same Store + Same Customer Visit
        ↓
Invoice created within 60 minutes after interaction
        ↓
StaffAssistedConversion candidate
        ↓
Validate interaction evidence
        ↓
Create assisted conversion record
```

Create:

```text
StaffAssistedConversion
```

Fields:

- Id
- TenantId
- StoreId
- StaffUserId
- CustomerId
- CustomerVisitId
- StaffCustomerInteractionId
- InvoiceId
- InvoiceAmount
- AttributedRevenue
- ConversionRule
- ConfidenceScore
- IsConfirmed
- ConfirmedByUserId nullable
- ConvertedUtc
- CreatedUtc

Do not automatically assign full sales credit to every staff member who appeared near the customer.

Config:

```text
AssistedConversionWindowMinutes = 60
```

Allow the tenant to choose attribution policy:

- Primary Staff Only
- Confirmed Staff Only
- Split Among Confirmed Staff
- Report Interaction Only / No Revenue Attribution

---

3.51 Staff Monthly Performance Reports

ShopOwner/authorized manager must be able to answer:

```text
This month Ravi handled how many customers?
How many were known vs new?
How many converted to invoice?
What was conversion rate?
How much assisted revenue?
Which categories did he show most?
What is average customer interaction duration?
```

Staff monthly KPIs:

- Customers Handled
- Unique Customers Handled
- Anonymous Visitors Handled
- Returning Customers Handled
- New Customers Handled
- Interaction Count
- Total Interaction Minutes
- Average Interaction Duration
- Products Shown
- Categories Discussed/Tagged
- Voice Commands Used
- Successful Voice Tags
- Voice Command Failures / Ambiguities
- Converted Customers
- Conversion Rate
- Assisted Invoice Count
- Assisted Revenue
- Average Converted Invoice Value
- No-Sale Interactions
- Repeat Customers Assisted

Example:

```text
Staff: Ravi
Month: August 2026

Customers Handled:       186
Unique Customers:        151
Converted Customers:      68
Conversion Rate:        36.6%
Assisted Invoices:         72
Assisted Revenue:    ₹4,85,000
Average Interaction:    11 min
Voice Interest Tags:       94
Top Category: Banarasi Saree
```

Filters:

- Tenant
- Store
- Staff
- Role
- Month
- Date range
- Category
- Customer type
- Conversion status

Charts:

- Customers handled by day/week/month
- Conversion rate trend
- Assisted revenue trend
- Staff comparison
- Categories handled by staff
- Interaction duration distribution
- Voice-tag usage trend
- Customer revisit after staff interaction

---

3.52 Customer Journey Report

For each visit show a timeline:

```text
4:10 PM  Entered Main Gate
4:14 PM  Banarasi Saree Zone
4:19 PM  Ravi interaction started
4:23 PM  Aasha Add → Banarasi Saree
4:31 PM  Silk Saree Zone
4:45 PM  Ravi interaction ended
4:49 PM  Billing Zone
4:52 PM  Invoice INV-00125 ₹8,500
5:02 PM  Exit
```

Summary:

- Total visit duration
- Known/anonymous state
- Household/visit party context when enabled
- Zones visited
- Zone dwell time
- Staff handled by
- Categories/products shown
- Voice/manual interest tags
- Invoice/payment outcome
- Top inferred interests

---

3.53 Family / Multi-Person Tracking Settings

The user-requested multi-person/family-oriented entry tracking is configurable and **default ON** for the tenant/store where enabled.

Settings:

```text
MultiPersonTrackingEnabled = true
VisitPartyDetectionEnabled = true
FamilyGroupTrackingEnabled = true
VerifiedHouseholdContextEnabled = true
AutoSuggestFrequentCoVisitorsEnabled = true
AutoLinkHouseholdFromFaceSimilarity = false
```

Recommended mode enum:

```text
FamilyTrackingMode
- Off
- VisitPartyOnly
- VerifiedHouseholdContext
```

Recommended default:

```text
VerifiedHouseholdContext
```

Meaning:

- Track people entering together.
- Create/suggest a `VisitParty`.
- If recognized customers already belong to a verified Household, show that context.
- Do not infer `wife`, `father`, `mother`, `child`, etc. from appearance/face resemblance.
- Unknown members remain anonymous visitors until authorized registration/linking.

Settings UI:

```text
Customer Admin
  → Settings
    → CCTV & Tracking
      → Multi-Person Tracking        [ON]
      → Visit Party Detection        [ON]
      → Family/Household Context      [ON]
      → Frequent Co-Visitor Suggest  [ON]
```

Platform Admin may define defaults; Tenant/Store override must remain within platform privacy/security policy.

---

3.54 Voice Command UI — Customer Admin

Add routes:

```text
/customer-admin/store-categories
/customer-admin/voice-commands
/customer-admin/voice-command-audit
```

`Voice Commands` page:

- Store selector
- Enable / Disable
- Trigger Keyword
- Keyword aliases
- Language
- Response mode
- Confirmation mode
- Command timeout
- Test command
- Last configuration version
- Changed by
- Changed date/time

Example:

```text
Store: Surat Main
Voice Trigger: Aasha Add
Status: Enabled
Aliases: Asha Add, Add Interest
Language: hi-IN / en-IN
```

Test box:

```text
Transcript: "Aasha Add Banarasi Saree"
Resolved Trigger: Aasha Add
Resolved Category: Banarasi Saree
Result: Valid
```

Audit table:

- Time
- Store
- Staff
- Customer
- Transcript/normalized command (minimized where needed)
- Resolved Category
- Result
- Confirmation Required
- Error reason

---

3.55 Shop Owner / Staff Angular Pages

Customer Admin sidebar adds:

```text
Staff
Staff Performance
Staff Tracking
Customer Journeys
Store Categories
Voice Commands
Voice Command Audit
```

Staff detail tabs:

- Overview
- Assigned Stores
- Current Shift
- Customer Interactions
- Converted Customers
- Assisted Invoices
- Category Activity
- Voice Commands
- Zone Activity
- Monthly Performance
- Audit

ShopOwner dashboard cards add:

- Staff On Shift
- Staff Currently With Customers
- Customers Being Assisted
- Staff Conversions Today
- Staff Assisted Revenue Today
- Average Customer Dwell Time
- Customers Inside > X Minutes
- Top Interest Category Today
- Voice Tags Today

---

3.56 Staff / Voice / Journey APIs

Tenant-scoped APIs:

```text
GET    /api/tenant/staff
POST   /api/tenant/staff
GET    /api/tenant/staff/{staffId}
PUT    /api/tenant/staff/{staffId}
POST   /api/tenant/staff/{staffId}/activate
POST   /api/tenant/staff/{staffId}/deactivate

POST   /api/tenant/staff/{staffId}/shifts/start
POST   /api/tenant/staff/{staffId}/shifts/end
GET    /api/tenant/staff/{staffId}/performance
GET    /api/tenant/staff/{staffId}/interactions
GET    /api/tenant/staff/{staffId}/conversions

GET    /api/tenant/stores/{storeId}/voice-command-setting
PUT    /api/tenant/stores/{storeId}/voice-command-setting
POST   /api/tenant/stores/{storeId}/voice-command-setting/test

GET    /api/tenant/store-categories
POST   /api/tenant/store-categories
PUT    /api/tenant/store-categories/{categoryId}
POST   /api/tenant/store-categories/{categoryId}/aliases

POST   /api/tenant/staff-customer-interactions/start
POST   /api/tenant/staff-customer-interactions/{id}/end
POST   /api/tenant/voice-commands/interpret
POST   /api/tenant/voice-commands/apply-category-interest

GET    /api/tenant/customers/{customerId}/journeys
GET    /api/tenant/customers/{customerId}/dwell-summary
GET    /api/tenant/visits/{visitId}/journey
GET    /api/tenant/reports/staff-performance
GET    /api/tenant/reports/customer-dwell
GET    /api/tenant/reports/customer-journeys
GET    /api/tenant/reports/voice-command-audit
```

For ordinary staff/Tenant Admin, StoreId and CustomerId must still be validated against authenticated tenant/store scope.

---

3.57 New SignalR / WebSocket Events

Add tenant/store-scoped events:

```text
StaffShiftStarted
StaffShiftEnded
StaffEntered
StaffExited
StaffCustomerInteractionStarted
StaffCustomerInteractionEnded
StaffCustomerInteractionUpdated
CustomerInterestTagged
CustomerPreferenceUpdated
StaffInsightReady
StoreVoiceKeywordChanged
VoiceCommandAccepted
VoiceCommandNeedsConfirmation
VoiceCommandRejected
CustomerDwellThresholdReached
CustomerJourneyUpdated
StaffAssistedConversionCreated
StaffPerformanceUpdated
TrackingSettingsChanged
```

Groups remain server-authorized:

```text
tenant:{tenantId}
tenant:{tenantId}:store:{storeId}
user:{userId}
```

A store keyword change should notify only authorized users/devices for that tenant/store.

---

3.58 Tracking / Voice Privacy and Reliability Rules

- Voice commands should preferably be push-to-talk or staff-device initiated rather than continuously recording ambient customer conversations.
- Do not keep raw staff/customer audio unless a separately justified, consented and retention-controlled feature requires it.
- Persist parsed command/audit metadata, not unnecessary background audio.
- Staff tracking must be transparent and governed by tenant workplace policy and applicable requirements.
- Customer recognition remains consent-based as already defined.
- Unknown visitors remain anonymous.
- CCTV dwell analysis indicates observed movement/dwell behavior, not sensitive personality traits or guaranteed purchase intent.
- Staff performance scores must expose the underlying counts/rules and should support manager review/correction.
- Do not use facial resemblance to establish family relationships.
- Keep customer and staff tracking events tenant/store scoped and retention controlled.

---

3.59 New Demo Mode Scenarios

Add Demo buttons:

```text
Simulate Staff Shift Start
Simulate Staff Entry
Simulate Customer Entry
Simulate Staff Starts Handling Customer
Simulate Dynamic Voice Command: "Aasha Add Banarasi Saree"
Simulate Ambiguous Customer Confirmation
Simulate Ambiguous Category Confirmation
Simulate Customer Zone Dwell
Simulate Customer Exit
Simulate Assisted Invoice Conversion
Simulate Monthly Staff Report
Simulate Store Keyword Change
```

All Demo flows must call the same application services as production.

---

3.60 Acceptance Scenarios — Staff / CCTV / Dynamic Aasha Keyword

### Scenario A — Dynamic keyword per shop

```text
Store A keyword = Aasha Add
Store B keyword = Magic Add

Staff in Store B says:
Aasha Add Banarasi Saree

Result:
Rejected as trigger mismatch for Store B

Staff says:
Magic Add Banarasi Saree

Result:
Accepted
```

### Scenario B — Staff handles customer and tags interest

```text
Ravi starts interaction with Priya
        ↓
Ravi shows Banarasi Saree
        ↓
Ravi says: Aasha Add Banarasi Saree
        ↓
Category resolved
        ↓
StaffVoiceTag preference signal created
        ↓
Customer interest recalculated
        ↓
Insight returned to Ravi
```

### Scenario C — Monthly staff conversion

```text
August
Ravi handled 186 customers
68 customers produced qualified assisted invoices
Conversion rate = 68 / 186 = 36.6%
```

Report must permit drill-down to interaction + visit + invoice evidence.

### Scenario D — Customer dwell analysis

```text
Customer enters 4:10 PM
Banarasi Saree Zone 18 min
Silk Saree Zone 11 min
Staff interaction 26 min
Invoice ₹8,500
Customer exits 5:02 PM
Total dwell = 52 min
```

### Scenario E — Family/group setting ON

```text
4 people enter together
MultiPersonTrackingEnabled = true
VisitPartyDetectionEnabled = true
FamilyGroupTrackingEnabled = true
        ↓
Create/suggest VisitParty
        ↓
Recognized members mapped to verified Household only if already linked
        ↓
Unknown members remain AnonymousVisitor
```


4. SQL Server Configuration
Database:
CustSearch_AI
SQL Server:
KRUTARTH-BHAVSA
Connection string:
Data Source=KRUTARTH-BHAVSA;
Initial Catalog=CustSearch_AI;
Integrated Security=True;
Persist Security Info=False;
Pooling=False;
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=True;
Application Name=CustSearch_AI;
Command Timeout=0;
Recommended ASP.NET configuration:
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=KRUTARTH-BHAVSA;Initial Catalog=CustSearch_AI;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=CustSearch_AI;"
  },
  "DatabaseSettings": {
    "CommandTimeout": 0
  }
}

5. EF Core + Dapper Strategy
Use Entity Framework Core For
    • Entities
    • DbContext
    • Relationships
    • Fluent Entity Configurations
    • Normal CRUD
    • LINQ Queries
    • Includes
    • AsNoTracking()
    • Transactions where appropriate
    • Normal application data access
Example:
public sealed class CustSearchDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<CustomerVisit> CustomerVisits => Set<CustomerVisit>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Camera> Cameras => Set<Camera>();
}
Use Dapper For
    • Stored Procedures
    • Dashboard queries
    • Reports
    • Complex customer searches
    • Invoice processing
    • Webhook queues
    • Heavy read queries
    • AI event ingestion
    • Bulk operations
    • Multi-result queries
Architecture:
Controller
    ↓
Application Service
    ↓
Repository
    ├── EF Core
    └── Dapper
           ↓
      Stored Procedure
           ↓
      SQL Server 2022

6. No EF Migrations
Never create:
Migrations/
Never run:
Add-Migration
Update-Database
Database.Migrate()
EnsureCreated()
Database changes only through:
database/*.sql
Create table:
DatabaseVersions
Example versions:
1.0.0 Initial Database
1.1.0 Household Module
1.2.0 Customer Preferences
1.3.0 Webhooks
1.4.0 AI Recognition
Application startup may validate database connectivity/version.
Application startup must never automatically change schema.

7. Main Feature 1 — Multiple People Enter Shop
Example:
Krutarth
Wife
Mother
Father

4 people enter together.
Camera flow:
Entrance Camera
       ↓
4 Persons Detected
       ↓
Multi-Person Tracking
       ↓
4 Face Observations
       ↓
4 Visitor/Customer Sessions
       ↓
Same Co-Visit / Party Session
Create:
PARTY-000125
Party members:
Person 1
Person 2
Person 3
Person 4
Important:
AI may determine:
These people entered together.
AI must NOT claim:
Person 2 is wife
Person 3 is mother
Person 4 is father
Family relationship must come from verified/customer-provided/admin-confirmed information. Important identity-document rule:
    • Do not use CCTV/face recognition to discover, infer or look up Aadhaar/PAN details.
    • Do not automatically extract/store Aadhaar or PAN for ordinary retail recognition.
    • If a legally justified workflow separately requires customer-submitted identity documents, it must be an explicit, consented, access-controlled KYC workflow with masking, encryption, retention limits and audit logs. It must remain separate from face-recognition matching.

8. Visit Party
Create:
VisitParty
Fields:
    • Id
    • PartyCode
    • StoreId
    • EntryCameraId
    • EntryTimeUtc
    • ExitTimeUtc
    • DetectedMemberCount
    • Status
    • CreatedUtc
Create:
VisitPartyMember
Fields:
    • Id
    • VisitPartyId
    • CustomerId nullable
    • AnonymousVisitorId nullable
    • CustomerVisitId
    • TrackingId
    • JoinedUtc
    • LeftUtc
Example:
PARTY-000100

Member 1 → CUST-000001
Member 2 → CUST-000020
Member 3 → VIS-000101
Member 4 → VIS-000102

9. Household / Family Management
Create:
Household
Fields:
    • Id
    • HouseholdCode
    • HouseholdName
    • PrimaryCustomerId
    • StoreId
    • Notes
    • IsActive
    • CreatedUtc
    • UpdatedUtc
Example:
HH-000001
Bhavsar Household
Create:
HouseholdMember
Fields:
    • Id
    • HouseholdId
    • CustomerId
    • RelationshipType
    • RelationshipLabel
    • IsPrimary
    • IsVerified
    • VerifiedBy
    • VerifiedUtc
    • CreatedUtc
Relationship types:
    • Self
    • Spouse
    • Parent
    • Child
    • Sibling
    • Relative
    • Friend
    • Companion
    • Other
AI can suggest:
Possible Frequent Co-Visitor Group
Admin can:
    • Create Household
    • Add Existing Customer
    • Convert Visitor
    • Confirm Relationship

10. First-Time Family Visit
Suppose 4 new people enter.
Create:
VIS-000001
VIS-000002
VIS-000003
VIS-000004
Do NOT automatically create verified named customers.
Live Monitoring:
New Group Detected

Party:
PARTY-000001

Members:
4

Entrance:
Main Gate

Time:
4:35 PM

[View Group]
After registration + consent:
VIS-000001 → CUST-000001
VIS-000002 → CUST-000002
VIS-000003 → CUST-000003
VIS-000004 → CUST-000004
Then optionally create:
HH-000001
and link all four customers.

11. Known Family Visit
Example:
Father + Mother enter together.
Recognition:
Father → CUST-000003
Mother → CUST-000004
Both already linked to:
HH-000001
System shows:
Household:
HH-000001

2 Known Household Members Currently Inside

12. Main Feature 2 — ₹5,000 Shopping Attribution
Scenario:
4 family members enter the store.
Krutarth pays ₹5,000 cash.
Correct invoice handling:
Invoice:
INV-000100

Household:
HH-000001

VisitParty:
PARTY-000001

Billing Customer:
Krutarth

Payer:
Krutarth

Amount:
₹5,000

Payment:
Cash
Individual spend:
Krutarth Personal Spend += ₹5,000
Wife Personal Spend += ₹0
Father Personal Spend += ₹0
Mother Personal Spend += ₹0
Household spend:
HH-000001 Household Spend += ₹5,000
Do NOT add ₹5,000 to every member.
Otherwise fake revenue becomes:
4 × ₹5,000 = ₹20,000

13. Invoice Model
Create:
Invoice
Fields:
    • Id
    • InvoiceNumber
    • StoreId
    • CustomerId
    • PayerCustomerId
    • HouseholdId nullable
    • VisitId nullable
    • VisitPartyId nullable
    • Subtotal
    • DiscountAmount
    • TaxAmount
    • FinalAmount
    • PaymentMethod
    • PaymentReference
    • InvoiceDateUtc
    • CreatedUtc

14. Invoice Participants
Create:
InvoiceParticipant
Fields:
    • Id
    • InvoiceId
    • CustomerId
    • ParticipationType
    • AttributedAmount
    • CreatedUtc
Participation types:
    • Purchaser
    • Payer
    • Companion
    • Recipient
    • Shared
Default rule:
Payer receives full spend attribution.
Companion receives ₹0.
Admin can explicitly split:
Krutarth → ₹3,000
Wife     → ₹2,000
Never split automatically.

15. Invoice Payment
Create:
InvoicePayment
Fields:
    • Id
    • InvoiceId
    • PayerCustomerId
    • PaymentMethod
    • Amount
    • TransactionReference
    • PaidUtc
    • CreatedUtc
Payment methods:
    • Cash
    • UPI
    • Card
    • NetBanking
    • Wallet
    • Other

16. Customer Spending
Maintain separately:
PersonalSpend
HouseholdSpend
Example customer screen:
Krutarth

Personal Spending:
₹52,500

Household:
Bhavsar Household

Household Spending:
₹1,25,000
Never mix personal and household spending.

17. Main Feature 3 — Customer Preferences
Create:
CustomerPreferenceProfile
Track explainable preferences:
    • Favorite Categories
    • Favorite Brands
    • Frequently Purchased Products
    • Average Order Value
    • Preferred Price Range
    • Purchase Frequency
    • Last Purchased Categories
    • Repeat Products
    • Preferred Store Sections
    • Preferred Payment Method

18. Customer Preference Signals
Create:
CustomerPreferenceSignal
Fields:
    • Id
    • CustomerId
    • PreferenceType
    • ReferenceType
    • ReferenceId
    • Score
    • Source
    • Reason
    • LastUpdatedUtc
Sources:
    • Purchase
    • RepeatPurchase
    • ManualStaffTag
    • StoreZoneInterest
    • ExplicitCustomerPreference
Example:
Customer:
CUST-000001

Category:
Men Shirts

Score:
92

Reason:
Purchased 5 times during last 6 months

19. CCTV-Based Interest
Optional AI store zones:
    • Men's Wear
    • Women's Wear
    • Kids Wear
    • Footwear
    • Billing
    • Entrance
    • Exit
Track:
Customer spent:
8 minutes → Formal Shirts Zone
2 minutes → Shoes
1 minute → Accessories
Create:
CustomerZoneInterest
CCTV dwell-time should remain a low-confidence preference signal.
Purchase history should carry higher weight.

20. Preference Scoring
Example configurable weights:
Actual Purchase             50%
Repeat Purchase             25%
Explicit Preference         15%
Store Zone Interest          5%
Staff Tag                    5%
Keep weights configurable from Admin.

21. Household Preferences
Create:
HouseholdPreferenceProfile
Aggregate:
    • Top Categories
    • Top Brands
    • Top Products
    • Average Household Spend
    • Recent Household Purchases
    • Purchase Frequency
    • Favorite Categories
Example:
Bhavsar Household

Top Categories:
1. Men's Formal Wear
2. Women's Ethnic Wear
3. Footwear

Last Purchase:
₹5,000

Last Payer:
Krutarth

22. Personal vs Household Preferences
Clearly separate:
PERSONAL PREFERENCES
and:
HOUSEHOLD PREFERENCES
Example:
Krutarth

Personal:
Formal Shirts
Black Shoes

Household:
Ethnic Wear
Kids Wear
Footwear

23. Main Feature 4 — Next Visit Smart Recognition
Example:
Last visit:
Household:
HH-000001

Krutarth paid:
₹5,000

Purchased:
Formal Shirt
Shoes
Next visit: Wife enters alone.
Flow:
Wife Face Detected
      ↓
CUST-000002
      ↓
Household Member
      ↓
HH-000001
      ↓
Load Household Summary
      ↓
Load Wife Personal Preferences
      ↓
Load Household Purchases
      ↓
Evaluate Notification Rules
Shopkeeper view:
RETURNING HOUSEHOLD MEMBER

Customer:
CUST-000002

Household:
Bhavsar Household

Household Last Purchase:
₹5,000

Last Payer:
Krutarth

Household Interests:
Formal Wear
Footwear

Customer Interests:
Ethnic Wear

Last Visit:
10 Aug 2026

24. Shopkeeper Notifications
Support configurable notification channels:
    • In-App
    • WhatsApp
    • SMS
    • Email
Example rule:
IF returning household member enters
AND household previous purchase >= ₹5,000
THEN notify shopkeeper

25. Notification Example
Returning Customer Detected

Customer:
Priya Bhavsar

Household:
Bhavsar Household

Household Last Purchase:
₹5,000

Last Payer:
Krutarth Bhavsar

Personal Interest:
Ethnic Wear

Household Interest:
Formal Wear, Footwear

Last Visit:
10 Aug 2026
Do not expose unnecessary sensitive data.

26. Notification Configuration
Admin:
Settings
→ Notifications
Options:
    • In-App Enabled
    • Email Enabled
    • Recipient Email
    • WhatsApp Enabled
    • Recipient Number
    • SMS Enabled
    • Recipient Number

27. Alert Rules
Admin page:
/admin/alert-rules
Rules:
    • Returning Customer
    • Returning Household Member
    • Previous Personal Spend >= X
    • Previous Household Spend >= X
    • VIP Customer Entered
    • VIP Household Member Entered
    • Specific Product Interest
    • Specific Category Interest
    • Customer Returned After X Days
    • Unknown Visitor
    • High Confidence Recognition
    • Recognition Review Required
    • vip customer Manually add by admin

27.1 Customer-Side VIP Campaign Notifications
    • Admin can create targeted campaigns/offers for eligible VIP customers, subject to consent and notification preferences.
28. Notification Cooldown
Prevent duplicate alerts.
Create setting:
NotificationCooldownMinutes
Example:
60
One rule/visit should not send repeated notifications every frame.

29. Main Feature 5 — Existing Shop Software Integration
Existing shop may already use:
    • POS
    • Billing Software
    • ERP
    • CRM
    • Custom Software
CustSearch AI must support:
Inbound Integration
+
Outbound Webhooks

30. Outbound Webhook Events
Events:
customer.created
customer.updated

household.created
household.member.added
household.member.removed

visit.started
visit.ended

invoice.created
invoice.updated

payment.created

preference.updated

alert.created

customer.recognized

31. Customer Created Webhook
Example:
{
  "event": "customer.created",
  "version": "1.0",
  "eventId": "evt_10025",
  "occurredUtc": "2026-08-15T10:00:00Z",
  "data": {
    "customerId": 125,
    "customerCode": "CUST-000125",
    "firstName": "Krutarth",
    "mobileNumber": "XXXXXXXXXX"
  }
}

32. Invoice Webhook
{
  "event": "invoice.created",
  "version": "1.0",
  "data": {
    "invoiceNumber": "INV-000125",
    "customerCode": "CUST-000001",
    "householdCode": "HH-000001",
    "amount": 5000,
    "paymentMethod": "Cash"
  }
}

33. Webhook Admin Pages
Create:
/admin/integrations

/admin/webhooks

/admin/webhooks/create

/admin/webhooks/deliveries

/admin/sync-logs
Webhook configuration:
    • Webhook Name
    • Destination URL
    • Secret
    • Enabled Events
    • Status
    • Retry Enabled
    • Timeout
    • Store

34. Webhook Security
Use:
HMAC-SHA256
Headers:
X-CustSearch-Event
X-CustSearch-Event-Id
X-CustSearch-Delivery-Id
X-CustSearch-Signature
X-CustSearch-Version

35. Webhook Retry
If external shop software is unavailable:
Attempt 1 → Failed
Attempt 2 → Retry
Attempt 3 → Retry
...
Create:
WebhookDelivery
Fields:
    • Id
    • WebhookEndpointId
    • EventId
    • EventType
    • Payload
    • AttemptCount
    • Status
    • LastHttpStatus
    • LastError
    • NextRetryUtc
    • DeliveredUtc
    • CreatedUtc
Worker Service handles retries.

36. Webhook Idempotency
Every webhook event must have unique:
EventId
Inbound APIs should support:
Idempotency-Key
Avoid duplicate customer/invoice creation.

36.1 Webhook Logs and Reports
    • Maintain complete webhook delivery/retry/error logs and expose searchable reports in the Angular Admin.
37. Inbound Integration APIs
Existing billing/POS software can call:
POST /api/integrations/inbound/customers
POST /api/integrations/inbound/invoices
POST /api/integrations/inbound/payments
POST /api/integrations/inbound/products
Flow:
Existing Billing Software
       ↓
Invoice ₹5,000
       ↓
Webhook/API
       ↓
CustSearch AI
       ↓
Customer Purchase History
       ↓
Preference Engine
       ↓
Household Summary

38. External Customer Mapping
Create:
ExternalCustomerMapping
Fields:
    • Id
    • IntegrationId
    • ExternalCustomerId
    • CustomerId
    • ExternalSystem
    • CreatedUtc
Example:
POS Customer:
POS-4588

CustSearch:
CUST-000125

39. External Invoice Mapping
Create:
ExternalInvoiceMapping
Fields:
    • ExternalInvoiceId
    • InvoiceId
    • IntegrationId
Prevent duplicate invoice imports.

40. Python AI Project — Complete Feature List
Project:
src/CustSearch.AI
Modules:
CameraManager
RTSPClient
CameraHealthMonitor

PersonDetector
MultiObjectTracker

FaceDetector
FaceQualityService

FaceEmbeddingService
FaceMatcher

RecognitionService

VisitPartyDetector
CoVisitGroupService

ZoneTracker
DwellTimeService

DuplicateDetectionService

SnapshotService

EventPublisher

ASP.NETApiClient

OfflineEventQueue

HealthApi

ConfigurationService

41. Python Project Structure
CustSearch.AI/
│
├── app/
│   ├── main.py
│   │
│   ├── api/
│   ├── camera/
│   ├── detection/
│   ├── tracking/
│   ├── recognition/
│   ├── parties/
│   ├── zones/
│   ├── integrations/
│   ├── models/
│   └── core/
│
├── models/
├── tests/
├── requirements.txt
├── Dockerfile
└── README.md

42. Python Camera Manager
Responsibilities:
    • Connect CCTV
    • Disconnect camera
    • Automatic reconnect
    • Multiple cameras
    • Camera heartbeat
    • Frame sampling
    • AI FPS control
    • Connection failures
    • Resource cleanup

43. Person Detection
Detect:
Person 1
Person 2
Person 3
Person 4
Generate track IDs:
TRACK-001
TRACK-002
TRACK-003
TRACK-004

44. Person Tracking
Track the same person across frames.
Avoid:
1 person standing for 10 seconds
=
300 new visitors
Use multi-object tracking and cooldown/session logic.

45. Visit Party / Co-Visit Detection
If people:
    • enter through same entrance
    • enter within configured window
    • move together
AI can create:
Possible Visit Party
Config:
PartyDetectionWindowSeconds = 15
Important:
Visit Party != Family
Family/Household must be verified separately.

46. Face Detection
Validate:
    • Face present
    • Minimum size
    • Blur
    • Lighting
    • Angle
    • Occlusion
Quality:
Good
Fair
Poor
Only appropriate quality frames proceed to recognition.

47. Consent-Based Face Recognition
Only match:
Customers with active FaceRecognition consent
Recognition result:
Matched
ReviewRequired
Unknown
Example configurable thresholds:
>= 0.85     Matched
0.70-0.849  ReviewRequired
< 0.70      Unknown

48. Household Recognition
Do NOT determine household by facial resemblance.
Correct flow:
Face
 ↓
Customer Match
 ↓
ASP.NET API
 ↓
HouseholdMember
 ↓
Household
Example:
Face → CUST-000004
             ↓
        HH-000001

49. Zone Tracking
Configure store zones:
    • Men Wear
    • Women Wear
    • Kids Wear
    • Footwear
    • Billing
    • Entrance
    • Exit
Track:
    • Zone Entered
    • Zone Left
    • Dwell Time
    • Repeat Zone Visits
Python emits events.
.NET Application layer calculates preference score.

50. Python Offline Event Queue
If ASP.NET API is temporarily unavailable:
    • Queue detection events locally
    • Retry safely
    • Use delivery acknowledgement
    • Do not indefinitely store biometric data
    • Use retention rules

51. Angular Admin Sidebar / Pages

Important: the Angular application has two navigation modes based on authenticated scope: Platform Admin and Customer/Tenant Admin.
Platform Admin sees cross-tenant management pages. Customer/Tenant Admin sees only its own tenant pages.

Platform Admin pages:
Dashboard
Tenants / Clients
Tenant Details
Subscription Plans
Platform Billing Invoices
Platform Payments
Platform Reports
System Health
Audit Logs
Settings

Customer/Tenant Admin operational pages:
Dashboard

Live Monitoring

Current Visitors

Visit Parties

Visitors

Customers

Households / Families

Customer Preferences

Household Preferences

Visits

Products

Invoices

Payments

Cameras

Camera Health

Camera Events

Recognition Review

Alerts

Alert Rules

Notifications

Consents

Biometric Profiles

Integrations

Inbound APIs

Webhooks

Webhook Deliveries

Sync Logs

Reports

Users

Roles & Permissions

Audit Logs

Settings

System Health

Demo Tools

52. Dashboard
Cards:
    • People Inside
    • Known Customers
    • Unknown Visitors
    • Families / Households Inside
    • Today's New Customers
    • Returning Customers
    • Today's Sales
    • Household Sales
    • High Value Customers
    • Active Cameras
    • Offline Cameras
    • Recognition Reviews
    • Alerts
    • Webhook Failures
Charts:
    • Hourly Visitors
    • Visitors by Day
    • Sales by Day
    • New vs Returning
    • Known vs Unknown
    • Household Activity
    • Camera Detection Activity
    • Purchase Distribution
    • Recognition Accuracy

53. Live Monitoring
Use SignalR on the ASP.NET Core backend and @microsoft/signalr in Angular. The page must reconnect automatically and reload authoritative state from REST APIs when required.
Example known customer card:
KNOWN CUSTOMER

Krutarth Bhavsar
CUST-000001

Household:
HH-000001

Personal Spend:
₹52,500

Household Spend:
₹1,25,000

Last Purchase:
₹5,000

Interest:
Formal Shirts
Shoes

[View Customer]
[View Household]
Example unknown visitor:
UNKNOWN VISITOR

VIS-000122

Camera:
Main Entrance

Face Quality:
Good

[Review]
[Create Customer]
[Link Existing Customer]

54. Visit Parties Page
Show:
PARTY-000025

4 Members

Known:
2

Unknown:
2

Possible Existing Household:
HH-000001
Actions:
    • View
    • Create Household
    • Link Household
    • Convert Member
    • Add Existing Customer

55. Household Details Page
Tabs:
    • Overview
    • Members
    • Current Visit
    • Visit History
    • Purchase History
    • Household Preferences
    • Invoices
    • Alerts
    • Activity Timeline
Summary:
Members:
4

Total Household Spend:
₹1,25,000

Average Order:
₹7,500

Last Purchase:
₹5,000

Last Payer:
Krutarth

Top Categories:
Formal Wear
Ethnic Wear
Footwear

56. Customer Details Page
Tabs:
    • Overview
    • Personal Preferences
    • Household
    • Visits
    • Purchases
    • Invoices
    • Payments
    • Recognition History
    • Consents
    • Alerts
    • Audit

57. Customer Preference Admin
Display:
Preference
Score
Reason
Source
Last Updated
Example:
Formal Shirts
92%
Purchased 5 times

Black Shoes
84%
Purchased 3 times

Accessories
45%
Zone Interaction Only

58. Admin Roles
Create platform roles:
PlatformSuperAdmin
PlatformOperationsAdmin
PlatformBillingAdmin
PlatformSupportAdmin
PlatformAuditor

Create tenant roles:
TenantAdmin
StoreAdmin
Manager
CRMStaff
BillingStaff
CameraOperator
IntegrationAdmin
Auditor

PlatformSuperAdmin
Full platform access across tenants.

PlatformOperationsAdmin
Tenant lifecycle/health/usage operations without unrestricted billing or security configuration unless granted.

PlatformBillingAdmin
Subscription plans, PlatformInvoice and PlatformPayment operations.

PlatformSupportAdmin
Limited support access; any tenant data access must be permissioned and audited.

PlatformAuditor
Read-only platform and tenant audit/report access according to policy.

TenantAdmin
Full access inside its own tenant, subject to platform restrictions and subscription limits. Cannot access other tenants or platform-level configuration.

StoreAdmin
Everything for assigned stores inside the same tenant except tenant/global system-level settings.
Manager
Pages:
    • Dashboard
    • Live Monitoring
    • Customers
    • Households
    • Visits
    • Invoices
    • Alerts
    • Reports
    • Preferences
CRMStaff
Pages:
    • Customers
    • Household Management
    • Visitor Conversion
    • Preferences
    • Consents
    • Customer History
BillingStaff
Pages:
    • Customer Search
    • Invoice
    • Payment
    • Products
    • Purchase History
CameraOperator
Pages:
    • Live Monitoring
    • Cameras
    • Camera Health
    • Camera Events
    • Visitors
    • Recognition Review
    • Visit Parties
IntegrationAdmin
Pages:
    • Integrations
    • Webhooks
    • Webhook Deliveries
    • Inbound API
    • Sync Logs
    • Integration Settings
Auditor
Read-only:
    • Customers
    • Households
    • Visits
    • Invoices
    • Alerts
    • Consents
    • Reports
    • Audit Logs
    • Webhook Logs

59. Permission System
Use granular permissions. Platform permissions:
Tenants.View
Tenants.Create
Tenants.Edit
Tenants.Activate
Tenants.Suspend
Tenants.ViewUsage
Tenants.ViewOperationalSummary
PlatformBilling.View
PlatformBilling.Manage
SubscriptionPlans.View
SubscriptionPlans.Manage
PlatformReports.View
PlatformReports.Export
PlatformAudit.View
PlatformSupport.AccessTenant

Tenant administration permissions:
TenantDashboard.View
TenantUsers.View
TenantUsers.Create
TenantUsers.Edit
TenantUsers.Deactivate
TenantUsers.AssignRoles
TenantStores.View
TenantStores.Create
TenantStores.Edit
TenantBilling.View
TenantReports.View
TenantReports.Export
TenantAudit.View

Operational permissions:
Customers.View
Customers.Create
Customers.Edit

Visitors.View
Visitors.Convert

Households.View
Households.Create
Households.Edit
Households.ManageMembers

Visits.View
Visits.Edit

Invoices.View
Invoices.Create
Invoices.Edit

Payments.View
Payments.Create

Cameras.View
Cameras.Manage
Cameras.Control

Recognition.View
Recognition.Review

Preferences.View
Preferences.Manage

Alerts.View
Alerts.Acknowledge
Alerts.Configure

Consents.View
Consents.Manage

Integrations.View
Integrations.Manage

Webhooks.View
Webhooks.Manage

Reports.View
Reports.Export

Users.View
Users.Manage

Staff.View
Staff.Manage
StaffTracking.View
StaffPerformance.View
StaffPerformance.Export
StaffCustomerInteractions.View

StoreCategories.View
StoreCategories.Manage

VoiceCommands.Use
VoiceCommands.View
VoiceCommands.Configure
VoiceCommands.Audit

CustomerJourneys.View
DwellAnalytics.View

Roles.Manage

Settings.View
Settings.Manage

AuditLogs.View

60. Main Database Tables
Create at minimum:

Platform / multi-tenant tables:
Tenants
SubscriptionPlans
TenantSubscriptions
TenantUsageSnapshots
PlatformInvoices
PlatformInvoiceItems
PlatformPayments
ReportExportJobs
OutboxMessages

Tenant-owned operational tables:
Stores

Users
Roles
Permissions
UserRoles
RolePermissions

StaffProfiles
StaffShifts
StaffPresenceSessions
StaffTrackingSessions
StaffZoneVisits
StaffCustomerInteractions
StaffCustomerInteractionEvents
StaffAssistedConversions

StoreVoiceCommandSettings
StoreVoiceCommandAliases
StaffVoiceCommands
ProductCategoryAliases

PersonTrackingSessions
CustomerDwellSessions

Customers

CustomerConsents
CustomerBiometricProfiles

AnonymousVisitors

Households
HouseholdMembers

VisitParties
VisitPartyMembers

CustomerVisits

FaceDetectionEvents
RecognitionAttempts

StoreZones
CustomerZoneVisits

CustomerPreferenceProfiles
CustomerPreferenceSignals

HouseholdPreferenceProfiles

Products
ProductCategories

Invoices
InvoiceItems
InvoiceParticipants
InvoicePayments

Cameras
CameraEvents

Alerts
AlertRules

Notifications

Integrations
ExternalCustomerMappings
ExternalInvoiceMappings

WebhookEndpoints
WebhookEvents
WebhookDeliveries

AuditLogs
SystemSettings
DatabaseVersions

Mandatory schema rule:
Add TenantId to all tenant-owned root tables and create appropriate indexes. Existing data migration/backfill must be handled only through versioned SQL scripts. No EF migrations.

61. Important Stored Procedures
Use EF Core for normal CRUD, but keep Dapper + SP for complex operations.
Create:
Tenant_Search
Tenant_GetDashboardSummary
Tenant_GetUsageSummary
Tenant_GetOperationalSummary
Platform_GetDashboardSummary
PlatformInvoice_Search
PlatformInvoice_Create
ReportExport_CreateJob
ReportExport_GetPendingJobs

Customer_Search
Customer_GetSmartProfile

Household_GetDetails
Household_GetSmartProfile
Household_AddMember

Visitor_ConvertToCustomer

Visit_TryCreateEntry
Visit_CreateParty
Visit_AddPartyMember
Visit_CompleteExit

Invoice_Create
Invoice_CreatePayment
Invoice_GetPurchaseContext

Preference_GetCustomerProfile
Preference_GetHouseholdProfile

Alert_GetCustomerContext
Alert_Create

Webhook_QueueEvent
Webhook_GetPendingDeliveries
Webhook_MarkDelivered
Webhook_MarkFailed

Dashboard_GetSummary
Dashboard_GetCharts
TenantDashboard_GetSummary

Staff_Search
Staff_GetDetails
Staff_StartShift
Staff_EndShift
Staff_StartPresence
Staff_EndPresence
Staff_StartCustomerInteraction
Staff_EndCustomerInteraction
Staff_GetMonthlyPerformance
Staff_GetConversionSummary
Staff_GetHandledCustomers

StoreVoiceCommand_GetSettings
StoreVoiceCommand_SaveSettings
StoreVoiceCommand_ResolveCategory
StoreVoiceCommand_LogCommand
Customer_AddPreferenceSignalFromStaff

CustomerJourney_GetVisitTimeline
CustomerDwell_GetSummary
CustomerDwell_GetZoneBreakdown
CustomerInterest_Recalculate
StaffAssistedConversion_TryCreate
TenantDashboard_GetCharts
PlatformDashboard_GetSummary
PlatformDashboard_GetCharts

Tenant-owned stored procedure rule:
Every tenant-owned procedure must accept and enforce @TenantId (or resolve it through a verified parent) and must not return cross-tenant rows to Tenant Admin.

62. Stored Procedure Comment Standard
Every stored procedure must have a header:
/*
==============================================================
Procedure     : dbo.Customer_Search
Purpose       : Searches customers using multiple identifiers.

Used By       : CustomerRepository / Global Admin Search

Parameters:
 @SearchText
 @PageNumber
 @PageSize
 @StoreId

Returns:
 Customer results + pagination information.
==============================================================
*/

63. C# Comment Standard
Every important class/interface/service/repository/controller should contain useful XML comments.
Example:
/// <summary>
/// Handles customer profile, search, preference and household
/// related business operations.
/// </summary>
/// <remarks>
/// Uses EF Core for normal CRUD and Dapper/Stored Procedures
/// for complex reads and transactional operations.
/// </remarks>
public sealed class CustomerService
{
}
Do not add useless comments line-by-line.
Comments should explain:
    • What the component does
    • Where it is used
    • Important business rules
    • Security/privacy requirements

64. Smart Customer Profile API
Create:
GET /api/customers/{id}/smart-profile
Returns:
{
  "customer": {},
  "household": {},
  "personalSpend": 52500,
  "householdSpend": 125000,
  "lastPurchase": {},
  "personalPreferences": [],
  "householdPreferences": [],
  "activeAlerts": []
}

65. Household Smart Profile API
Create:
GET /api/households/{id}/smart-profile
Returns:
    • Members
    • Total Spend
    • Recent Purchases
    • Last Payer
    • Preferences
    • Recent Visits
    • Current Members Inside
    • Alerts

66. Customer Entry Production Flow
Customer Enters
     ↓
Person Detected
     ↓
Face Detected
     ↓
Tracking ID
     ↓
Face Quality Check
     ↓
Consent-Based Recognition
     ↓
Customer Found?
   ↙       ↘
 YES       NO
 ↓          ↓
Customer   Anonymous Visitor
 ↓          ↓
Household? Visit Party
 ↓
Load:
Personal History
Household History
Preferences
Last Purchase
 ↓
Create Visit
 ↓
Evaluate Alert Rules
 ↓
SignalR
 ↓
Shopkeeper Notification

67. Four-Member Family Example
Scenario:
Krutarth
Wife
Father
Mother
All enter together.
System:
PARTY-000001

CUST-000001
CUST-000002
CUST-000003
CUST-000004

Household:
HH-000001
Shopping:
Invoice:
₹5,000

Payer:
Krutarth

Payment:
Cash
Result:
Krutarth Personal Spend
+ ₹5,000

Wife Personal Spend
+ ₹0

Father Personal Spend
+ ₹0

Mother Personal Spend
+ ₹0

HH-000001 Household Spend
+ ₹5,000

68. Next Family Member Visit
Example: Mother enters alone next week.
Flow:
Mother Face
 ↓
CUST-000004
 ↓
HH-000001
 ↓
Last Household Purchase ₹5,000
 ↓
Last Payer Krutarth
 ↓
Household Preferences
 ↓
Mother Personal Preferences
 ↓
Rule Match
 ↓
Notify Shopkeeper
Message:
Returning Household Member Detected

Customer:
CUST-000004

Household:
HH-000001

Last Household Purchase:
₹5,000

Last Payer:
Krutarth

Customer Interests:
Ethnic Wear

Household Interests:
Formal Wear, Footwear

69. Authentication / Security
Implement:
    • JWT Authentication
    • Refresh Tokens
    • Role-Based Authorization
    • Permission-Based Authorization
    • Password Hashing
    • Rate Limiting
    • Data Protection
    • Camera Credential Encryption
    • Biometric Data Protection
    • Global Exception Handling
    • Audit Logs
    • Correlation IDs
    • Secure Secrets Management
Never log:
    • Passwords
    • JWT tokens
    • Camera passwords
    • Face embeddings
    • API secrets

70. Consent / Privacy Rules
Required:
    • Explicit Consent
    • Face Recognition Consent
    • Consent Timestamp
    • Consent Purpose
    • Consent Withdrawal
    • Data Retention
    • Audit Trail
    • Access Control
    • Data Minimization
Do NOT implement:
Face → Aadhaar
Face → Instagram
Face → Facebook
Face → LinkedIn
Face → Unknown Person Identity Lookup
Unknown detected people remain:
Anonymous Visitor
until authorized identification is provided.

71. SignalR Events
Create:
VisitorEntered
CustomerEntered
CustomerExited
ReturningCustomerDetected
ReturningHouseholdMemberDetected
VipCustomerDetected
HighValueCustomerDetected
UnknownVisitorDetected
VisitPartyDetected
RecognitionReviewRequired
CameraOnline
CameraOffline
NewAlert
InvoiceCreated
WebhookFailed
DashboardUpdated

72. Background Worker
Create:
CustSearch.Worker
Jobs:
    • Notification dispatch
    • Webhook delivery
    • Webhook retry
    • Camera health
    • Retention cleanup
    • Consent expiry
    • Anonymous visitor cleanup
    • Alert processing
    • Preference recalculation
    • System health monitoring

73. Redis
Use Redis for:
    • Dashboard cache
    • Customer Smart Profile cache
    • Household Smart Profile cache
    • Permission cache
    • Camera status
    • Short-lived AI state
    • Event cooldown
    • Distributed cache
Application should degrade gracefully if Redis is unavailable in development.

74. Reports
Reports are tenant-aware and permission-aware. The detailed Platform vs Tenant report catalog is defined in section 3.25.
Create operational reports:
    • Daily Visitor Report
    • Current Visitors Report
    • Returning Customer Report
    • Returning Household Report
    • New Customer Report
    • VIP Customer Report
    • Unknown Visitor Report
    • Sales Report
    • Personal Spend Report
    • Household Spend Report
    • Purchase Category Report
    • Customer Preference Report
    • Household Preference Report
    • Camera Health Report
    • Recognition Report
    • Alert Report
    • Webhook Delivery Report
    • Integration Sync Report
    • Staff Master Report
    • Staff Attendance / Shift Report
    • Staff Customer Handling Report
    • Staff Monthly Performance Report
    • Staff Assisted Conversion Report
    • Staff Assisted Revenue Report
    • Staff Category Assistance Report
    • Staff Zone Activity Report
    • Customer Dwell Time Report
    • Customer Journey Report
    • Customer Zone Interest Report
    • Store Category Interest Report
    • Voice Command Usage Report
    • Voice Command Audit / Failure Report
    • Dynamic Keyword Change Audit Report
    • Family / Visit Party Tracking Report
Exports:
    • CSV
    • Excel
    • PDF optional

75. System Settings
Create admin settings for:
RecognitionThreshold
ReviewThreshold
FaceQualityThreshold
PersonDetectionThreshold
AIProcessingFPS
SamePersonCooldownSeconds
PartyDetectionWindowSeconds
NotificationCooldownMinutes
HighValueThreshold
SnapshotEnabled
SnapshotRetentionDays
AnonymousVisitorRetentionDays
WebhookRetryCount
WebhookTimeoutSeconds
DemoMode

StaffTrackingEnabled
StaffZoneTrackingEnabled
StaffCustomerInteractionTrackingEnabled
CustomerDwellTrackingEnabled
CustomerJourneyTrackingEnabled
StaffAssistedConversionEnabled
AssistedConversionWindowMinutes

MultiPersonTrackingEnabled = true
VisitPartyDetectionEnabled = true
VerifiedHouseholdContextEnabled = true
FamilyGroupTrackingEnabled = true
AutoSuggestFrequentCoVisitorsEnabled = true
AutoLinkHouseholdFromFaceSimilarity = false

VoiceCommandEnabled
VoiceCommandDefaultLanguageCode
VoiceCommandConfirmationMode
VoiceCommandSessionTimeoutSeconds
AllowVoiceCategoryCreate = false

Important configuration precedence:
Platform Default → Tenant Default → Store Override.
Store voice trigger keyword is always resolved store-wise when a Store override exists.

76. System Health
Admin:
/admin/system-health
Show:
    • ASP.NET API
    • SQL Server
    • Redis
    • Python AI
    • Worker
    • Cameras
    • Webhook Queue
    • SignalR Hub
    • Active WebSocket Connections
    • WebSocket Reconnect/Failure Rate
    • Redis SignalR Backplane when enabled
Statuses:
Healthy
Warning
Offline

77. Demo Mode
System must work without physical CCTV.
Admin Demo buttons:
    • Simulate Known Customer
    • Simulate Unknown Visitor
    • Simulate Family / Household Visit
    • Simulate High Value Customer
    • Simulate VIP Customer
    • Simulate Customer Exit
    • Simulate Camera Offline
    • Simulate Invoice
    • Simulate Webhook Failure
Demo must use the same application services/business logic as production.

78. Database Script Structure
Use:
database/
├── 01_Database/
├── 02_Tables/
├── 03_Indexes/
├── 04_Types/
├── 05_Functions/
├── 06_Views/
├── 07_StoredProcedures/
├── 08_Seed/
├── 09_Upgrade/
└── 10_TestData/
Create:
database/01_Database/001_CreateDatabase.sql
Use safe script:
IF DB_ID(N'CustSearch_AI') IS NULL
BEGIN
    CREATE DATABASE CustSearch_AI;
END
GO
Never automatically drop the database.

79. Database Versioning
Create table:
DatabaseVersions
Fields:
    • VersionId
    • VersionNumber
    • Description
    • AppliedUtc
    • AppliedBy
Upgrade scripts example:
V1.0.1_AddHouseholds.sql
V1.0.2_AddCustomerPreferences.sql
V1.0.3_AddWebhookIntegration.sql
V1.0.4_AddVisitParties.sql

80. Build Priority
Phase 1 — Foundation
Solution
.NET backend projects
Angular Admin workspace
SQL Server
DbContext
Dapper Infrastructure
Database Scripts
DatabaseVersions
Structured Serilog + correlation logging foundation

Phase 2 — Multi-Tenant Foundation
Tenants
TenantId ownership model
CurrentTenant / TenantContext
Tenant-aware repositories
Tenant-aware stored procedures
Tenant isolation integration tests
Platform Admin authentication
Tenant Admin authentication
Refresh Token Flow

Phase 3 — Authorization + Admin Shells
Platform roles/permissions
Tenant roles/permissions
Angular Platform Admin navigation
Angular Customer Admin navigation
Auth guards
Permission guards
Tenant-aware API clients
Tenant suspension/session rules

Phase 4 — Platform Tenant Management
Platform Dashboard
Tenant list/create/edit
Activate/suspend tenant
Subscription plans
Tenant quotas
Tenant usage
Tenant detail summary
Platform audit

Phase 5 — Tenant Users + Stores + Shop Owner / Staff
Tenant user CRUD
Tenant user roles
ShopOwner / TenantOwner role
Staff profile CRUD
Staff store assignments
Staff shifts / presence sessions
Store assignments
Store CRUD
Store quotas
Store canonical address, coordinates, geofence, time zone and location verification
Store Category taxonomy
Dynamic Store Voice Command settings
Customer Admin Dashboard base

Phase 6 — Shopper Customers
Customer APIs
Anonymous Visitor APIs
Customer Search APIs
Angular Customers Feature
Angular Visitors Feature
Customer smart profile
Tenant isolation validation

Phase 7 — Household / Visits
Households
Household Members
Visit Parties
Visits

Phase 8 — Products / Retail Billing
Products
Retail Invoices
Invoice Items
Payments
Invoice Participants
Spend Attribution
Tenant-wise invoice search/report

Phase 9 — Platform Billing
PlatformInvoices
PlatformInvoiceItems
PlatformPayments
Tenant Plan & Billing page
Platform Billing Admin

Phase 10 — Preferences + Staff Voice Interest Tagging
Customer Preferences
Household Preferences
Staff manual preference tags
Dynamic voice trigger parser
Store category alias resolution
Customer interest recalculation
Interest response notification
Voice command audit

Phase 11 — Alerts / Real-Time
Alerts
Notifications
ASP.NET Core SignalR Hub
WebSocket configuration
Angular RealtimeService
Tenant/store/user groups
Reconnect + state recovery
Event de-duplication
Outbox publisher for critical events
Live Monitoring Real-Time UI
WebSocket health metrics

Phase 12 — Integrations
Webhooks
Integrations
Inbound APIs
Tenant-aware signing/logging/retry

Phase 13 — Cameras / Python CCTV / Customer + Staff Tracking
Cameras
Camera Zones
Python CCTV
Person Tracking
Customer tracking sessions
Staff tracking sessions
Customer dwell-time tracking
Staff zone tracking
Staff/customer proximity-assisted interaction evidence
Face Detection
Visit Party Detection
Verified family/household context when enabled

Phase 14 — Recognition
Consent-Based Recognition
Recognition Review
Face Enrollment

Phase 15 — Reports
Platform Reports
Tenant Reports
Staff Monthly Performance
Staff Customer Handling
Staff Assisted Conversion / Revenue
Customer Dwell Time / Journey
Zone / Category Interest
Voice Command Usage / Audit
Family / Visit Party Tracking
ReportExportJob
Async CSV/Excel/PDF exports
ReportExportProgress WebSocket events

Phase 16 — Operational Platform
Audit
Worker
Redis
SignalR Redis backplane readiness
Settings
System Health
Retention

Phase 17 — Quality / Deployment
.NET Tests
Python Tests
Angular Unit/Component Tests
Tenant isolation security tests
Playwright Platform Admin E2E
Playwright Customer Admin E2E
WebSocket reconnect E2E
Swagger
Postman
Documentation
Angular Production Build
IIS SPA Rewrite
IIS WebSocket Validation
Deployment

81. First Working Milestone — Platform + Customer Admin
PlatformSuperAdmin Login
 ↓
Create Tenant TEN-000001
 ↓
Create TenantAdmin User
 ↓
TenantAdmin Login
 ↓
Customer Admin Dashboard
 ↓
Create Store
 ↓
Create Tenant Staff User
 ↓
Verify staff cannot access another tenant
 ↓
Audit all actions

82. Second Working Milestone — Customer / Invoice / Report
TenantAdmin Login
 ↓
Customer Search
 ↓
Create Shopper Customer
 ↓
Create Household
 ↓
Create Retail Invoice ₹5,000
 ↓
Payer Attribution
 ↓
Customer Smart Profile
 ↓
Tenant Retail Sales Report
 ↓
Platform Admin Tenant Summary reflects tenant totals

83. Third Working Milestone — WebSocket / SignalR
TenantAdmin Login
 ↓
Open /hubs/realtime WebSocket
 ↓
Server joins tenant:{tenantId}
 ↓
Simulate Customer Entry
 ↓
CustomerEntered event received
 ↓
Dashboard + Live Monitoring update
 ↓
Disconnect network
 ↓
Automatic reconnect
 ↓
Authorized groups restored
 ↓
REST state recovery
 ↓
No duplicate event after EventId de-duplication

84. Fourth Working Milestone — CCTV + Integration
Real RTSP CCTV
 ↓
OpenCV / ONNX
 ↓
Person Detection / Tracking
 ↓
Face Detection
 ↓
Consent-Based Recognition
 ↓
Customer / Anonymous Visitor
 ↓
Visit / Party
 ↓
Household Context
 ↓
Alert
 ↓
SignalR/WebSocket Tenant Notification
 ↓
External POS Invoice Import
 ↓
Tenant Retail Invoice
 ↓
Spend Attribution
 ↓
Preference Recalculation
 ↓
Tenant Report Update

84.1 Fifth Working Milestone — Platform Billing + Async Reports
Platform Admin creates/assigns plan
 ↓
PlatformInvoice issued to Tenant
 ↓
Tenant Admin views Plan & Billing
 ↓
Tenant Admin requests large report export
 ↓
ReportExportJob queued
 ↓
Worker creates file
 ↓
ReportExportProgress events
 ↓
ReportExportReady event
 ↓
Authorized download

85. Critical Codex Instructions
Use these rules exactly:
    1. Use Entity Framework Core for entities, DbContext, relationships and normal CRUD.
    2. DO NOT use EF Core Migrations.
    3. Do not call Database.Migrate().
    4. Do not call EnsureCreated().
    5. Database schema changes only through versioned SQL scripts.
    6. Use Dapper for stored procedures, complex reads, reports, invoice transactions, webhook queues and AI event ingestion.
    7. Keep all .NET projects inside CustSearch_AI.sln.
    8. Keep Python CustSearch.AI inside the same repository.
    9. src/CustSearch.Admin is an Angular project and is not added to CustSearch_AI.sln as a .csproj.
    10. Use Angular SPA for both Platform Admin and Customer/Tenant Admin experiences.
    11. CustSearch.API remains the only backend entry point for Admin business operations.
    12. Do not use React, Next.js, Vue, Razor Views or jQuery for the Admin UI.
    13. Angular must call ASP.NET Core only through typed REST APIs and SignalR hubs; it must never access SQL Server/Redis directly.
    14. Use Angular route guards and UI permission checks, but always enforce authorization again in ASP.NET Core.
    15. Keep access tokens short-lived and use a secure refresh-token design; do not place long-lived refresh tokens in browser localStorage.
    16. Use Angular Reactive Forms and keep authoritative validation/business rules on the backend.
    17. Use lazy-loaded feature routes and feature-first Angular folders.
    18. Use Angular Signals/RxJS for UI state and real-time streams; avoid unnecessary global state complexity.
    19. Implement multi-tenancy with Tenant as the platform client/business boundary.
    20. Do not confuse Tenant/Client with Shopper Customer.
    21. Platform Admin may work across tenants only with explicit platform permissions.
    22. Tenant Admin and tenant staff must never access another TenantId.
    23. Every tenant-owned API, repository and stored procedure must enforce tenant scope on the server.
    24. Do not trust TenantId supplied by Angular for Tenant Admin authorization; resolve tenant from authenticated context.
    25. Add TenantId to tenant-owned root tables and tenant-specific unique indexes.
    26. Customer Admin must manage its own users, stores, shopper customers, retail invoices, reports, cameras, alerts, integrations, audit and settings according to permissions.
    27. Platform Admin must have tenant-wise summary/reporting for users, stores, shopper customers, visits, retail invoices, cameras, health and integrations.
    28. Separate Retail Invoice from Platform Billing Invoice.
    29. Unknown detected people must become AnonymousVisitor first.
    30. Do not infer family relationship from facial resemblance.
    31. AI may create/suggest a co-visit party, but Household/family linking requires verified/customer-provided relationship.
    32. Every detected member must have a separate visitor/customer identity.
    33. Invoice spend must be attributed only to the actual payer/billing customer unless an explicit split is supplied.
    34. Household spend is an aggregate metric and must not overwrite individual customer spend.
    35. Maintain separate Personal Preferences and Household Preferences.
    36. Face recognition must only use customers with active biometric consent.
    37. Implement outbound webhooks including customer.created and invoice.created.
    38. Implement inbound APIs for existing POS/ERP/shop software.
    39. Sign webhook calls using HMAC-SHA256.
    40. Support webhook retry, delivery logging and idempotency.
    41. Use SignalR with WebSocket preferred transport in production and enable WebSocket in IIS.
    42. WebSocket/SignalR group membership must be assigned only by the authorized server using tenant/store/user claims.
    43. Angular must handle reconnect, reconnect state, authorized group restoration, EventId de-duplication and REST state recovery.
    44. Never use SignalR as the only source of truth; REST/database remain authoritative.
    45. Critical event publication should use Outbox/retry semantics where event loss after database commit is unacceptable.
    46. Large report exports must run asynchronously through Worker and report progress/readiness over SignalR/WebSocket.
    47. All Platform Admin cross-tenant access and support/impersonation behavior must be audited.
    48. Add tenant-isolation automated tests that intentionally attempt cross-tenant reads/writes and must fail.
    49. Add WebSocket authenticated-connect, reconnect, duplicate-event and unauthorized-group-join tests.
    50. Implement ShopOwner/TenantOwner and tenant Staff as permission-based tenant users; never create a parallel unaudited identity system.
    51. Track customer and staff journeys with separate tracking identities and sessions; do not merge StaffId and CustomerId semantics.
    52. Staff tracking must be enabled by tenant/store policy, role permission and applicable notice/consent/workplace policy; do not implement hidden employee surveillance.
    53. Dynamic voice trigger keyword must be configurable per Store. Default example may be `Aasha Add`, but code must never hard-code it.
    54. Resolve voice commands from authenticated Staff + Store + active interaction context; never let a spoken command arbitrarily mutate an unrelated customer.
    55. If customer or category resolution is ambiguous, require confirmation instead of silently writing data.
    56. Voice category tagging creates an explainable CustomerPreferenceSignal with source `StaffVoiceTag`; it does not create a purchase or invoice.
    57. Unknown category names must resolve through the store category/alias taxonomy. Auto-create from voice is OFF by default and, if enabled, requires separate permission/audit.
    58. Assisted conversion attribution must use the same visit/customer/store and a configurable conversion window; keep confidence/evidence and allow audit/review.
    59. CCTV dwell-time/zone behavior is an interest signal, not proof of intent; purchase and explicit preference signals remain stronger.
    60. Multi-person / visit-party tracking is ON by configurable setting by default, but family relationships must still come only from verified/customer-provided links.
    61. AutoLinkHouseholdFromFaceSimilarity must remain false.
    62. Add Staff/Voice/Dwell/Conversion reports and tenant/store/month filters.
    63. Add real-time events for staff interactions, voice tags, dwell thresholds and assisted conversions.
    64. Add useful comments to every important class, interface, service, repository, controller, SQL table, stored procedure and Python service.
    65. Comments must explain what the component does, where it is used and important business/security behavior.
    66. Do not add meaningless line-by-line comments.
    67. Build and test after every phase.
    68. If physical CCTV or ONNX model is unavailable, continue in Demo Mode.
    69. Production IIS must support Angular SPA deep-link rewrites and SignalR WebSockets.
    70. Build/test Angular together with .NET and Python before completing a milestone.
    71. Do not stop after scaffolding; actually create all project files and production flows.
    72. Implement structured logging and correlation propagation across API, Worker, database/integration calls and Python service boundaries; keep audit events separate from diagnostic logs.
    73. Store location must support tenant-scoped canonical address, optional validated coordinates/geofence, time zone and audited verification/changes.

86. Build Validation
.NET:
dotnet restore
dotnet build
dotnet test
Angular Admin:
cd src/CustSearch.Admin
npm ci
npm run lint
npm test -- --watch=false
npm run build -- --configuration production
Playwright E2E (milestone/CI):
cd tests/CustSearch.Admin.E2E
npm ci
npx playwright test

Required E2E/security suites:
- Platform Admin tenant create/manage
- Tenant Admin own-data access
- Cross-tenant API access denied
- Tenant user role/store restriction
- Retail invoice tenant isolation
- Platform billing invoice visibility
- Tenant report filtering/export
- WebSocket authenticated connect
- WebSocket automatic reconnect
- WebSocket unauthorized tenant group denied
- ReportExportProgress/Ready event flow

Python:
python -m pip install -r requirements.txt
python -m pytest
Fix all compilation, TypeScript, lint, unit, integration and milestone E2E errors before moving to the next phase.

87. Final Architecture

                         CustSearch AI Platform
                                  │
               ┌──────────────────┴──────────────────┐
               │                                     │
        Platform Admin                         Customer/Tenant Admin
         Angular SPA                              Angular SPA
               │                                     │
               └──────────── REST + WSS/SignalR ─────┘
                                  │
                          CustSearch.API
                         ASP.NET Core 8
                                  │
          ┌───────────────────────┼────────────────────────┐
          │                       │                        │
 Tenant/Auth Context        Application Layer        SignalR Hub
          │                  EF Core + Dapper       tenant/store/user groups
          │                       │                        │
          └───────────────────────┼────────────────────────┘
                                  │
                          SQL Server 2022
                    TenantId-isolated business data
                                  │
          ┌───────────────────────┼────────────────────────┐
          │                       │                        │
       Redis                 Worker Service          Outbox / Events
  Cache + SignalR           Reports/Webhooks       Reliable dispatch
    backplane                  Notifications

CCTV Cameras
     ↓
Python FastAPI / OpenCV / ONNX / Tracking
     ↓
CustSearch.API
     ↓
Tenant-scoped Customer/Visit/Alert
     ↓
SignalR over WebSocket
     ↓
Only authorized tenant/store Admin clients

External POS / ERP
       ↕
Tenant-aware Inbound APIs + Outbound Webhooks

Data hierarchy:
Platform
  ↓
Tenant / Client Organization
  ↓
Stores
  ↓
Tenant Users + Shopper Customers + Cameras + Integrations
  ↓
Visits / Households / Retail Invoices / Payments / Reports / Alerts

Platform billing hierarchy:
Tenant
  ↓
TenantSubscription
  ↓
PlatformInvoice
  ↓
PlatformPayment

Critical separation:
Retail Invoice = shopper purchase inside tenant store.
Platform Invoice = CustSearch AI subscription/license invoice billed to tenant.

88. Start Implementation Now
Create in this order:
1. CustSearch_AI.sln
2. All .NET backend projects
3. .NET project references
4. Angular Admin workspace (`src/CustSearch.Admin`)
5. SQL Server 2022 scripts
6. DatabaseVersions
7. Tenants + TenantId schema foundation
8. Tenant-aware indexes and constraints
9. CustSearchDbContext
10. Dapper infrastructure
11. CurrentTenant / TenantScope services
12. Authentication APIs + refresh-token flow
13. Platform roles and permissions
14. Tenant roles and permissions
15. Angular core/shared architecture
16. Platform Admin shell/routes
17. Customer Admin shell/routes
18. Platform Tenant Management
19. Tenant create/edit/activate/suspend
20. Tenant Admin invitation/user creation
21. Tenant Stores
22. Tenant Users + store assignment
23. Tenant Dashboard
24. Shopper Customers APIs + Angular feature
25. Anonymous Visitors APIs + Angular feature
26. Customer Search
27. Households
28. Household Members
29. Visit Parties
30. Products
31. Retail Invoices
32. Retail Payments
33. Spend Attribution
34. Platform Subscription Plans
35. TenantSubscriptions
36. Platform Billing Invoices
37. Platform Payments
38. Customer Preferences
39. Household Preferences
40. Alerts
41. Notifications
42. ASP.NET SignalR Hub
43. WebSocket authorization + tenant/store/user groups
44. Angular RealtimeService
45. Automatic reconnect + REST state recovery
46. EventId de-duplication
47. Outbox/event dispatcher
48. Live Monitoring
49. Integrations
50. Webhooks
51. Cameras
52. Python CCTV AI
53. Person Detection
54. Person Tracking
55. Face Detection
56. Consent-Based Recognition
57. Platform Reports
58. Tenant Reports
59. ReportExportJob + Worker
60. ReportExportProgress/Ready WebSocket events
61. Audit
62. Redis
63. SignalR Redis backplane readiness
64. System Health + WebSocket metrics
65. Angular unit/component tests
66. Tenant isolation integration tests
67. Playwright Platform Admin E2E
68. Playwright Customer Admin E2E
69. Playwright WebSocket reconnect tests
70. .NET/Python tests
71. Swagger
72. Postman
73. IIS Angular SPA + WebSocket deployment configuration
74. Documentation

Implementation must create the actual backend, Angular Admin, multi-tenant database scripts, Customer Admin behavior, platform/tenant reports, retail/platform invoices, SignalR/WebSocket handling, Python AI and test projects. Do not stop after scaffolding or planning only.
