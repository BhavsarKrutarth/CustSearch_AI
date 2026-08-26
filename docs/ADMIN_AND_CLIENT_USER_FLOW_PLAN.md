# Admin and Client User Flow Plan

Last verified: 2026-08-26  
Applies to: Angular Admin + ASP.NET Core API on the current Phase 16/17 audit branch

## 1. Purpose and user definitions

This document defines the intended end-to-end journeys after the API and Angular Admin are running.
It is based on the current Angular routes, API controllers, permission catalog, role matrix and master
planning document—not only on planned screen names.

In this project, **Client User** means a tenant-side authenticated user:

- Tenant Owner / Tenant Admin / Shop Owner
- Store Admin / Store Manager
- Sales Staff / CRM Staff / Billing Staff
- Camera Operator / Integration Admin / Auditor

A shopper/customer does not currently sign in to this Admin application. Customers are business
records managed by authorized tenant users.

## 2. Running application entry points

| Application | Local URL | Purpose |
|---|---|---|
| Angular Admin | `http://localhost:4200` | User-facing administration and operations |
| ASP.NET API | `https://localhost:7277` | Authentication, authorization and business APIs |
| Swagger | `https://localhost:7277/swagger` | Development-only API inspection |
| API liveness | `https://localhost:7277/health/live` | Process liveness |
| API readiness | `https://localhost:7277/health/ready` | SQL/Redis dependency readiness |

Angular calls relative `/api/*` routes through its development proxy. It never connects directly to
SQL Server. The API derives TenantId from the authenticated server-side identity.

## 3. Login behavior

Open `http://localhost:4200/login`.

### Platform Admin login

| Field | Value |
|---|---|
| Tenant Code | Leave empty |
| Username | Platform username—not email unless username was explicitly created as an email |
| Password | Current local/bootstrap password |

Successful platform login redirects to `/admin/dashboard` through the `/platform-admin` redirect.

### Client/Tenant login

| Field | Value |
|---|---|
| Tenant Code | Exact tenant code, for example `SMOKE-TENANT-001` |
| Username | Tenant username, for example `smoke.tenantadmin` |
| Password | Password assigned to that tenant user |

Successful tenant login redirects to `/customer-admin/dashboard`. Authentication loads roles,
permissions and assigned store IDs from the API. Hidden Angular navigation is convenience only; every
API operation rechecks the permission and tenant/store boundary.

## 4. Platform Admin user flow

### 4.1 First platform setup

1. Sign in without a Tenant Code.
2. Open `/admin/dashboard` and verify tenant operational summary.
3. Change the bootstrap password through the approved local/bootstrap workflow.
4. Open `/admin/subscription-plans` and create or verify an active subscription plan.
5. Open `/admin/tenants/new` and create a tenant with its unique tenant code.
6. Open the tenant detail page and assign an active subscription and configured quotas.
7. Activate the tenant; do not use browser-provided TenantId as an authorization source.
8. Create the first Tenant Owner/Admin account through the tenant-management workflow.
9. Communicate the Tenant Code, username and temporary password out of band.
10. Verify tenant usage and audit entries from the tenant detail page.

### 4.2 Ongoing platform administration

| Activity | Angular route | Expected authority |
|---|---|---|
| Platform dashboard | `/admin/dashboard` | `Tenants.ViewOperationalSummary` |
| Tenant list/detail | `/admin/tenants` | `Tenants.View` |
| Create tenant | `/admin/tenants/new` | `Tenants.Create` |
| Edit tenant | `/admin/tenants/{tenantId}/edit` | `Tenants.Edit` |
| Subscription plans | `/admin/subscription-plans` | Plan view/manage permission |
| Billing plans | `/platform-admin/billing/plans` | Platform billing plan permission |
| Tenant subscriptions | `/platform-admin/billing/subscriptions` | Subscription permission |
| Platform invoices | `/platform-admin/billing/invoices` | Invoice permission |
| Platform payments | `/platform-admin/billing/payments` | Payment permission |
| Worker/system operations | `/admin/operations` | Platform operations permission |

### 4.3 Platform operations flow

1. Open `/admin/operations`.
2. Verify SQL and configured Redis readiness.
3. Inspect Worker heartbeats and leases.
4. Pause/resume a Worker only with `PlatformOperations.Manage` and a recorded reason.
5. Review dead letters before bounded retry; do not repeatedly retry unknown failures.
6. Review retention policy in dry-run/safe mode before execution.
7. Store only secret references; raw credentials must not be written into application settings or Git.

Platform Admin does not automatically become a tenant user. Support access or impersonation must be
explicitly authorized and audited.

## 5. Client Tenant Admin / Shop Owner setup flow

### 5.1 Tenant foundation

1. Sign in using the Tenant Code and tenant username.
2. Confirm `/customer-admin/dashboard` loads the expected tenant and assigned stores.
3. Open `/customer-admin/stores`; create the first store with code, address and time zone.
4. Add coordinates/geofence only when known, then use the authorized location verification action.
5. Open `/customer-admin/users`; create tenant users with temporary credentials.
6. Assign least-privilege roles and authoritative stores.
7. Open `/customer-admin/staff`; create the operational Staff profile linked to its tenant user.
8. Configure staff shifts/presence only as an operational signal—not sole payroll authority.

### 5.2 Store catalog and Aasha configuration

1. Open `/customer-admin/store-categories` and create the store category taxonomy.
2. Add category aliases used by staff speech or local language.
3. Open `/customer-admin/voice-commands`.
4. Select the store and configure its dynamic trigger keyword.
5. Keep ambiguity confirmation enabled and category auto-create disabled unless explicitly approved.
6. Grant `VoiceCommands.Use` only to staff who should use the workflow.
7. Use `/customer-admin/voice-command-audit` to review confirmed/rejected commands.

`Aasha Add` is only a default example. The trigger is store-configured and must never be hard-coded.
A store can use another phrase such as `Magic Add` without changing another store.

### 5.3 Customer operations

1. Open `/customer-admin/customers` to search or create customers.
2. Assign each customer only to authorized stores.
3. Open `/customer-admin/visitors` to manage anonymous visitors.
4. Convert a visitor only after an authorized staff decision; anonymous tracking does not assign identity.
5. Open `/customer-admin/households` to create an explicitly verified household.
6. Add members only from factual/user-confirmed relationships.
7. Use `/customer-admin/visits` for store visits.
8. Use `/customer-admin/visit-parties` for co-visits; a co-visit must not automatically create a household.

### 5.4 Retail operations

1. Open `/customer-admin/products`; create products and assign valid categories/stores.
2. Open `/customer-admin/retail/invoices/new`.
3. Select an authorized store, customer/party where factual, and invoice items.
4. Save the invoice, verify server-calculated totals, then finalize it.
5. Record payments through the authorized invoice workflow.
6. Record spend attribution only from factual billing/payment information.
7. Review `/customer-admin/retail/invoices` and `/customer-admin/retail/reports`.

Platform subscription billing and shop retail billing are separate financial domains and must never
be combined.

### 5.5 Camera, alerts and integrations

1. Open `/customer-admin/cameras` and register a camera under an authorized store.
2. Start with Demo Mode until RTSP credentials, zones, privacy and retention are reviewed.
3. Configure camera zones and verify anonymous tracking events.
4. Open `/customer-admin/alerts`; review, acknowledge or resolve alerts according to permission.
5. Open `/customer-admin/integrations`; configure credential/secret references, not raw secrets.
6. Execute a test webhook and inspect delivery/retry history.
7. Open `/customer-admin/recognition` only for consent-based enrollment and human review.
8. Open `/customer-admin/reports` for bounded reports and requester-bound asynchronous exports.

## 6. Store Staff daily flow

The exact menu is filtered by the staff member's server-issued permissions and assigned StoreIds.

1. Sign in with Tenant Code, username and password.
2. Verify only assigned stores are visible.
3. Search the customer or retain the person as an anonymous visitor.
4. Start/update a visit for the authorized store.
5. During an active customer interaction, use the configured voice trigger if permitted.
6. Confirm the customer/category when the match is ambiguous.
7. Verify the command adds an interest/preference signal only—it does not create a purchase.
8. Create/finalize an invoice or payment only if the staff role has billing permissions.
9. Review authorized alerts and customer context without viewing another store's restricted data.
10. Log out; refresh-token rotation and revocation remain API-controlled.

### Aasha command state flow

```text
Staff starts command session in assigned store
  -> API loads that store's active keyword and aliases
  -> staff speaks configured trigger + category/customer context
  -> API validates staff permission and store assignment
  -> unique match: show confirmation when configured
  -> ambiguous match: confirmation is mandatory
  -> confirmed: create factual preference signal + audit entry
  -> rejected/expired: no preference mutation
```

Security rules:

- Browser TenantId is ignored/rejected; JWT/current-user context owns tenant scope.
- StoreId must be present in the staff member's authoritative assignments.
- Voice tagging never creates a purchase or household relationship.
- Dwell/AI signals remain weaker than explicit staff confirmation and factual purchase signals.
- No customer identity or family relationship is inferred from face similarity or co-location.

## 7. UI-to-API flow mapping

| User task | Angular route | API area |
|---|---|---|
| Login/session | `/login` | `/api/auth/login`, `/refresh`, `/me`, `/logout` |
| Platform tenants | `/admin/tenants` | `/api/platform/tenants` |
| Platform operations | `/admin/operations` | `/api/platform/operations` |
| Tenant dashboard/setup | `/customer-admin/dashboard` | `/api/tenant/dashboard/summary` |
| Stores/users/staff | corresponding customer-admin routes | `/api/tenant/stores`, `/users`, `/staff` |
| Customers/visitors | `/customer-admin/customers`, `/visitors` | `/api/tenant/customers`, `/visitors` |
| Household/visits | `/customer-admin/households`, `/visits` | `/api/tenant/households`, `/visits`, `/visit-parties` |
| Voice/Aasha | `/customer-admin/voice-commands` | `/api/tenant/stores/{storeId}/voice-command-runtime`, `/voice/commands/*` |
| Retail | `/customer-admin/products`, `/retail/invoices` | `/api/tenant/products`, `/retail/invoices` |
| Cameras | `/customer-admin/cameras` | `/api/tenant/cameras`; protected ingestion `/api/internal/cctv/events` |
| Alerts/realtime | `/customer-admin/alerts` | `/api/tenant/alerts`, `/hubs/alerts` |
| Recognition | `/customer-admin/recognition` | `/api/tenant/recognition` |
| Reports/exports | `/customer-admin/reports` | `/api/tenant/reports` |

The request path is always:

```text
Angular -> ASP.NET API -> authorization/service -> Dapper or approved EF operation -> SQL Server
```

## 8. Expected access results

| Scenario | Expected result |
|---|---|
| No valid session | `401 Unauthorized` or login redirect |
| Valid user without permission | `403 Forbidden` / access-denied page |
| Tenant/store resource outside authorized scope | `404 Not Found` where concealment is intended, otherwise `403` |
| Invalid request | `400 Bad Request` |
| Duplicate/idempotency conflict | `409 Conflict` where applicable |
| Valid authorized request | `200`, `201` or `204` according to operation |

## 9. UAT acceptance checklist

### Platform Admin

- [ ] Login works with blank Tenant Code.
- [ ] Tenant plan, tenant, subscription and Tenant Admin can be created in order.
- [ ] Tenant activation/suspension is audited.
- [ ] Platform billing stays separate from retail billing.
- [ ] Worker health and controls show only to authorized platform roles.

### Tenant Admin / Shop Owner

- [ ] Login requires correct Tenant Code.
- [ ] Store, user, role, store assignment and Staff profile can be configured.
- [ ] Dynamic voice trigger is store-specific and ambiguity requires confirmation.
- [ ] Customer, visitor conversion, verified household and visit flows work.
- [ ] Product, invoice, payment and factual attribution flows work.
- [ ] Camera Demo Mode, alerts, reports and exports work within tenant/store scope.

### Staff and isolation

- [ ] Staff sees only assigned stores and granted navigation.
- [ ] Tenant A cannot read or modify Tenant B data.
- [ ] Store A staff cannot use Store B in a request payload.
- [ ] Voice commands cannot create purchases or unverified family relationships.
- [ ] Recognition remains consent-based and requires human review.

## 10. Current implementation boundary

The selected source branch implements the documented Phase 1-16 application flows and local Phase 17
hardening. Phase 18 suspected-unpaid-exit/security incident application screens, API, Worker and Python
workflow are not complete on this branch. Live Phase 18 database objects must not be edited manually as
a substitute for the missing authorized workflow.

For terminal-by-terminal startup commands, read `PROJECT_WISE_MANUAL_RUN_GUIDE.md`. For initial tenant
setup order, also read `POST_LOGIN_SETUP_GUIDE.md`; for precise capability boundaries, read
`ROLE_PERMISSION_MATRIX.md`.
