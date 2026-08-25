# Phase 9 — Platform Billing Implementation Plan

**Status:** Completed  
**Branch:** `phase9-platform-billing`  
**Database target:** SQL Server 2022 / V1.8.0  
**Final evidence:** `planning_document/PHASE_09_TEST_REPORT.md`

## Phase boundary

Phase 9 is **Platform Billing**: a tenant/shop owner pays CustSearch for a subscription.

Phase 8 is **Retail Billing**: a shop customer buys products from a tenant/store.

These domains remain separate in persistence, APIs, permissions and tests. Phase 9 may read authoritative tenant resource usage for quota enforcement but does not use retail invoices/payments as platform billing facts.

## Baseline gate

Phase 8 was green before Phase 9 started:

- .NET build/tests green
- Angular/Playwright/Python regressions green
- SQL Server 2022 V1.7.0 upgrade and standalone runner double-run green
- canonical `database/CustSearchAi.sql` contained V1.7.0
- `planning_document/PHASE_08_TEST_REPORT.md` existed

## 9A — Subscription plan catalog — Completed

The existing `SubscriptionPlans` foundation was extended with:

- PlanCode / Name / Description
- MonthlyPrice / AnnualPrice
- Currency
- TrialDays
- MaxStores / MaxUsers / MaxStaff / MaxCameras
- explicit/extensible feature limits
- IsActive / DisplayOrder / CreatedUtc / UpdatedUtc

Only Platform Admin can manage plans.

## 9B — Tenant subscriptions — Completed

Existing `TenantSubscriptions` now supports:

- server-derived TenantId / PlanId
- StartUtc / TrialEndUtc
- CurrentPeriodStartUtc / CurrentPeriodEndUtc
- CancelAtPeriodEnd / CancelledUtc
- Trial / Active / PastDue / Suspended / Cancelled / Expired

Creation and plan changes validate authoritative store/user/staff/camera usage against plan quotas.

## 9C — Platform invoices — Completed

Separate:

- `PlatformInvoices`
- `PlatformInvoiceItems`

Commercial plan snapshots are preserved. No platform financial FK points to `RetailInvoices`.

## 9D — Platform payments — Completed

Separate `PlatformPayments` supports Pending / Successful / Failed / Refunded. `TransactionReference` provides idempotency and conflicting reuse is rejected. Refund transitions safely reverse successful paid amount.

## 9E — Angular UI — Completed

Platform Admin:

- `/platform-admin/billing/plans`
- `/platform-admin/billing/subscriptions`
- `/platform-admin/billing/invoices`
- `/platform-admin/billing/payments`

Tenant Admin:

- `/customer-admin/billing`
- `/customer-admin/billing/invoices`
- `/customer-admin/billing/subscription`

Tenant Angular requests never carry TenantId.

## 9F — Authorization — Completed

Platform permissions:

- `PlatformBilling.Plans.View`
- `PlatformBilling.Plans.Manage`
- `PlatformBilling.Subscriptions.View`
- `PlatformBilling.Subscriptions.Manage`
- `PlatformBilling.Invoices.View`
- `PlatformBilling.Payments.View`

Tenant read-only grants:

- `TenantPlatformBilling.Subscriptions.View`
- `TenantPlatformBilling.Invoices.View`
- `TenantPlatformBilling.Payments.View`

## 9G — Database and validation — Completed

Created and validated:

- `database/09_Upgrade/V1.8.0_Phase9_PlatformBilling.sql`
- `database/run-phase9.sql`
- `database/verify-phase9.sql`
- `.github/workflows/phase9-validate.yml`
- `.github/workflows/phase9-sql-validate.yml`
- `planning_document/PHASE_09_TEST_REPORT.md`

The canonical `database/CustSearchAi.sql` contains the validated V1.8.0 block.

## Final observed validation

`Phase 9 Validate` run `32678432020`:

- .NET Release: 0 warnings / 0 errors
- Unit: 62/62
- Integration: 117/117
- Angular lint: green
- Angular tests: 54/54
- Angular production build: green
- Playwright Phase 5–9: 27/27
- Python Ruff: green
- Python pytest: 3/3
- SQL Server 2022 V1.8.0 upgrade twice: green
- `database/run-phase9.sql` twice: green
- exactly one V1.8.0 row: green
- canonical fresh install through V1.8.0: green

Independent SQL-only validation also passed in run `32655635075`.

## Completion rule result

All required Phase 9 gates are green. Phase 9 is Completed and is the validated prerequisite for Phase 10 once its final corrective commits/documentation are present in `AIMainBranch`.
