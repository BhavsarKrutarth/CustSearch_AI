# Phase 9 — Platform Billing

Status: Completed

## Source of Truth

- `../CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`
- `../PHASE_09_IMPLEMENTATION_PLAN.md`
- `../PHASE_09_TEST_REPORT.md`

## Scope

Phase 9 implements CustSearch platform subscriptions, platform invoices/items/payments and tenant billing pages. It is intentionally separate from Phase 8 Retail Billing.

- **Retail Billing:** shop customer purchases from a tenant/store.
- **Platform Billing:** tenant/shop owner pays CustSearch for the platform subscription.

The two domains do not share invoice/payment tables, APIs, permissions or report models.

## Completed scope

### 9A — Subscription plan catalog

The existing `SubscriptionPlans` foundation was extended rather than duplicated. It now supports description, monthly/annual pricing, currency, trial days, store/user/staff/camera quotas, feature limits, active state and display order. Only platform-scope administrators can manage plans.

### 9B — Tenant subscriptions

The existing `TenantSubscriptions` foundation now supports server-authoritative Trial / Active / PastDue / Suspended / Cancelled / Expired lifecycle, trial end, current billing period, cancel-at-period-end and cancellation timestamps. Plan assignment/change validates current authoritative tenant resource usage including staff quota.

### 9C — Platform invoices

Separate financial tables:

- `PlatformInvoices`
- `PlatformInvoiceItems`

Invoice items preserve plan/commercial snapshots. Phase 9 does not use `RetailInvoices`.

### 9D — Platform payments

Separate `PlatformPayments` records support Pending / Successful / Failed / Refunded state. `TransactionReference` is the callback idempotency boundary. Refund transitions adjust successful paid amount without mixing retail payment facts.

### 9E — Angular billing UI

Platform Admin:

- `/platform-admin/billing/plans`
- `/platform-admin/billing/subscriptions`
- `/platform-admin/billing/invoices`
- `/platform-admin/billing/payments`

Tenant Admin:

- `/customer-admin/billing`
- `/customer-admin/billing/invoices`
- `/customer-admin/billing/subscription`

Tenant billing is read-only and clearly states that shop-customer retail purchases are not shown there.

### 9F — Authorization

Platform permissions:

- `PlatformBilling.Plans.View`
- `PlatformBilling.Plans.Manage`
- `PlatformBilling.Subscriptions.View`
- `PlatformBilling.Subscriptions.Manage`
- `PlatformBilling.Invoices.View`
- `PlatformBilling.Payments.View`

Tenant read-only permissions use globally distinct names:

- `TenantPlatformBilling.Subscriptions.View`
- `TenantPlatformBilling.Invoices.View`
- `TenantPlatformBilling.Payments.View`

TenantId is derived from authenticated server context and is not accepted from Angular tenant billing requests.

## Database

Version: `V1.8.0`

Files:

- `database/09_Upgrade/V1.8.0_Phase9_PlatformBilling.sql`
- `database/run-phase9.sql`
- `database/verify-phase9.sql`
- `database/CustSearchAi.sql`

Stored procedures:

- `dbo.PlatformBilling_Plan_List`
- `dbo.PlatformBilling_Subscription_List`
- `dbo.PlatformBilling_Invoice_List`
- `dbo.PlatformBilling_Invoice_Get`
- `dbo.PlatformBilling_Payment_List`
- `dbo.TenantPlatformBilling_Summary_Get`

## Final validation evidence

Authoritative full validation:

- Workflow: `Phase 9 Validate`
- Run: `32678432020`
- Validated feature head: `8bed1c81c301bca562a68fc6ee281d928f03eb08`
- .NET Release: 0 warnings / 0 errors
- Unit: 62/62
- Integration: 117/117
- Angular lint: green
- Angular unit: 54/54
- Angular production build: green
- Playwright Phase 5–9: 27/27
- Python Ruff: green
- Python pytest: 3/3
- V1.8.0 upgrade twice on SQL Server 2022: green
- standalone `database/run-phase9.sql` twice: green
- exactly one V1.8.0 ledger row: green
- canonical fresh install through V1.8.0: green

Independent SQL-only validation also passed (`32655635075`), including stored-procedure smoke execution and `DBCC CHECKCONSTRAINTS`.

## Done Summary

Phase 9 is Completed and fully validated. The final corrective billing navigation/E2E commits must be present in `AIMainBranch` before Phase 10 branches from it. Full evidence is recorded in `../PHASE_09_TEST_REPORT.md`.
