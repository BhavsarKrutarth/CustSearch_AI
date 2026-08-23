# Phase 9 — Platform Billing Implementation Plan

**Status:** In Progress  
**Branch:** `phase9-platform-billing`  
**Database target:** SQL Server 2022 / V1.8.0  
**Sources:**
- `planning_document/phase_implementation/PHASE_09_PLATFORM_BILLING.md`
- `planning_document/CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`
- validated Phase 8 baseline and `planning_document/PHASE_08_TEST_REPORT.md`

## Phase boundary

Phase 9 is **Platform Billing**: a tenant/shop owner pays CustSearch for a subscription.

Phase 8 is **Retail Billing**: a shop customer buys products from a tenant/store.

These domains must never share invoice/payment tables, report models or permissions. Phase 9 may read authoritative tenant resource usage for quota enforcement, but it must not read or write `RetailInvoices`, `RetailInvoiceItems`, `RetailInvoicePayments`, retail spend attribution or retail reports.

## Baseline gate

Phase 8 baseline is green before Phase 9 starts:
- .NET build and tests green.
- Angular, Playwright and Python regressions green.
- SQL Server 2022 V1.7.0 upgrade and standalone runner double-run green.
- canonical `database/CustSearchAi.sql` contains V1.7.0.
- `planning_document/PHASE_08_TEST_REPORT.md` exists.

Phase 9 branch was created from the latest `AIMainBranch` baseline after the Phase 8 report/status reconciliation.

## 9A — Subscription plan catalog

Extend the existing `SubscriptionPlans` foundation rather than create a duplicate plan master.

Required commercial fields:
- PlanCode / Name
- Description
- MonthlyPrice / AnnualPrice
- Currency
- TrialDays
- MaxStores / MaxUsers / MaxStaff / MaxCameras
- explicit feature-limit fields plus extensible FeatureLimitsJson
- IsActive / DisplayOrder / CreatedUtc / UpdatedUtc

Only Platform Admin may manage plans. Tenant users never receive a plan-management API.

## 9B — Tenant subscriptions

Extend the existing historical `TenantSubscriptions` foundation with server-authoritative Phase 9 lifecycle fields:
- TenantId / PlanId
- StartUtc / TrialEndUtc
- CurrentPeriodStartUtc / CurrentPeriodEndUtc
- CancelAtPeriodEnd / CancelledUtc
- Trial / Active / PastDue / Suspended / Cancelled / Expired

Platform-admin commands create, renew, change plans and cancel subscriptions. Client payloads do not choose arbitrary authoritative status transitions. Plan changes and creation reject quotas below current authoritative store/user/staff/camera usage.

## 9C — Platform invoices

Create separate:
- `PlatformInvoices`
- `PlatformInvoiceItems`

Invoice items preserve immutable commercial snapshots including plan name, description, quantity, rate, discount, tax, subtotal and total. No Phase 9 code may use `RetailInvoices`.

## 9D — Platform payments

Create separate `PlatformPayments` with Pending / Successful / Failed / Refunded state.

`TransactionReference` is an idempotency boundary. A repeated callback with the same factual values returns the existing payment; conflicting reuse is rejected. Gateway integration remains provider-neutral.

## 9E — Angular UI

Platform Admin routes:
- `/platform-admin/billing/plans`
- `/platform-admin/billing/subscriptions`
- `/platform-admin/billing/invoices`
- `/platform-admin/billing/payments`

Tenant Admin routes:
- `/customer-admin/billing`
- `/customer-admin/billing/invoices`
- `/customer-admin/billing/subscription`

Tenant Angular requests never carry TenantId.

## 9F — Authorization

Stable Phase 9 permissions:
- `PlatformBilling.Plans.View`
- `PlatformBilling.Plans.Manage`
- `PlatformBilling.Subscriptions.View`
- `PlatformBilling.Subscriptions.Manage`
- `PlatformBilling.Invoices.View`
- `PlatformBilling.Payments.View`

Platform-scope grants authorize cross-tenant platform operations. Tenant-scope grants are view-only and the tenant is derived from the authenticated server context.

## 9G — Database and validation

Create:
- `database/09_Upgrade/V1.8.0_Phase9_PlatformBilling.sql`
- `database/run-phase9.sql`
- `.github/workflows/phase9-validate.yml`
- `planning_document/PHASE_09_TEST_REPORT.md` only with final observed evidence

Canonical `database/CustSearchAi.sql` is persisted only after the complete V1.8.0 validation matrix is green.

Required test coverage:
- subscription creation and trial
- renewal
- plan change
- cancellation / cancel-at-period-end
- quota enforcement including staff
- invoice server calculation and immutable plan snapshots
- payment idempotency
- cross-tenant denial
- tenant cannot edit plan
- retail invoice cannot appear in platform billing
- platform invoice cannot appear in retail billing
- full Phase 5–9 regression
- SQL Server 2022 V1.8.0 upgrade twice
- standalone `database/run-phase9.sql` twice
- exactly one V1.8.0 DatabaseVersions row
- prospective canonical fresh install through V1.8.0

## Completion rule

Do **not** mark Phase 9 Completed until the final Phase 9 workflow is green and `planning_document/PHASE_09_TEST_REPORT.md` contains exact observed results.
