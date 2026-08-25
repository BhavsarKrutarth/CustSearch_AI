# Database and Code Traceability

This catalog maps executable UI/API flows to their authorization and persistence boundaries.
TenantId always comes from the validated server identity. A supplied StoreId is accepted only after
checking authoritative assignments.

| Angular page | Angular service | API endpoint/controller | Application/infrastructure | SQL/query objects | Permission/scope | Test evidence |
|---|---|---|---|---|---|---|
| Login/session | auth/session services | `/api/auth/*` — `AuthController` | authentication/JWT/current-user services | Users, refresh tokens, auth events, authorization lookup | authenticated identity; platform or tenant scope | HTTP auth, expiry, rotation, reuse, logout |
| Platform tenants/dashboard | platform tenant client/pages | `/api/platform/*` — `PlatformTenantsController` | `PlatformTenantManagementService` | tenant detail/usage/provision procedures and platform tables | platform policy + exact permission | service/API/Angular/Playwright |
| Tenant setup/staff/stores | tenant operations client/pages | `/api/tenant/*` — `TenantOperationsController` | `TenantOperationsService` + security decorator | store/staff/user procedures and tables | tenant + assigned stores + permission | quota, scope, location and E2E |
| Customers/visitors | customer/visitor clients/pages | tenant customer/visitor routes — `ShopperCustomersController` | `ShopperCustomerService` + repository | `Customer_Search`, `AnonymousVisitor_Search` | tenant and allowed stores | conversion/isolation/UI/E2E |
| Households/visits | household/visit clients/pages | tenant household/visit routes — `HouseholdsVisitsController` | `HouseholdsVisitsService` | household/visit/party procedures | tenant/store + relationship permission | privacy, duplicate and scope tests |
| Retail products/invoices/reports | retail client/pages | tenant retail routes — `RetailBillingController` | `RetailBillingService` + Dapper repository | product/invoice/detail/report procedures | tenant/store + retail permission | totals, idempotency, attribution, E2E |
| Platform/tenant billing | billing client/pages | platform/tenant billing — billing controllers | `PlatformBillingService` | platform billing procedures/tables | platform permission or current tenant read-only | platform-vs-retail separation tests |
| Preferences/voice | preference/voice client/pages | tenant preferences routes — `PreferencesVoiceController` | `PreferencesVoiceService` | preference/alias/audit/weight procedures | tenant/store/staff category scope | ambiguity, confirmation, privacy tests |
| Alerts/notification center | alerts/realtime clients | `/api/tenant/alerts`, `/hubs/alerts` | `AlertsRealtimeService`, hub, outbox dispatcher | alert search/recovery/outbox procedures | tenant/store/user SignalR groups | authorization, dedupe, reconnect/E2E |
| Integration settings | integration client/page | tenant/inbound integration controllers | management, inbound and dispatcher services | integration search/claim/retry/log procedures | tenant + HMAC/service boundary | signature, replay, retry, isolation |
| Cameras/tracking | camera client/page | tenant camera + internal CCTV controllers | `CameraTrackingService` | camera/person-track procedures and CCTV tables | tenant/store + authenticated service | wrong-camera scope, Demo Mode, Python tests |
| Recognition review | recognition client/page | `/api/tenant/recognition` | `RecognitionService` + template protector | consent/candidate procedures and recognition tables | consent + tenant/store + recognition permission | withdrawal, ambiguity, encryption, IDOR |
| Reports/exports | report client/page | tenant/platform reports controllers | `ReportsExportsService`, Dapper reports, export worker | tenant/platform report and export job/event procedures | server tenant/store/requester binding | accuracy, isolation, expiry, download tests |
| Platform operations | operations client/page | `/api/platform/operations`, health endpoints | operational service, readiness checks, worker runtime gate | operational/retention/audit/heartbeat objects | platform scope + view/manage permission | 225 integration incl. dependency failures |
| Retail security | no source page | no source controller | no source service/worker | live-only `Security*` tables/procedures | permissions seeded live; not executable in source | MISSING — Phase 18 tests absent |

## Dapper versus EF boundary

- Dapper/stored procedures handle bounded search, reporting, queue claims and other heavy read/batch paths.
- EF Core handles explicitly planned transactional entity operations and relationship invariants.
- Angular never invokes SQL directly.
- No EF migrations or runtime schema creation are used for production database deployment.

## Detected drift

The live V1.16 security schema has no code consumer on this branch. It is intentionally shown as a
missing flow rather than falsely mapped to Phase 16 classes. Provenance/repeat-safe scripting is the
first Phase 18 database task after Phase 17.
