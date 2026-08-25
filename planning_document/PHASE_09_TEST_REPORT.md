# Phase 9 — Platform Billing Test Report

**Result:** PASS / GREEN  
**Branch:** `phase9-platform-billing`  
**Validated head:** `8bed1c81c301bca562a68fc6ee281d928f03eb08`  
**Phase 9 workflow:** `Phase 9 Validate` run `32678432020`  
**Database version:** `V1.8.0`  
**Database target:** SQL Server 2022

## Scope validated

Phase 9 implements CustSearch **Platform Billing** only: subscription plans, authoritative tenant subscriptions, separate platform invoices/items/payments and tenant billing read pages. It remains separate from Phase 8 Retail Billing, which represents shop-customer purchases.

## Automated validation matrix

| Gate | Result | Evidence |
|---|---:|---|
| .NET Release build | PASS | 0 warnings / 0 errors |
| .NET unit tests | PASS | 62/62 |
| .NET integration tests | PASS | 117/117 |
| Angular pinned version check | PASS | Angular 21.2.20 / CLI 21.2.21 |
| Angular lint | PASS | all files pass |
| Angular unit tests | PASS | 24 files, 54/54 tests |
| Angular production build | PASS | production bundle generated |
| Playwright Phase 5–9 regression | PASS | 27/27 |
| Python Ruff | PASS | all checks passed |
| Python pytest | PASS | 3/3 |
| Platform-vs-retail static boundary | PASS | no cross-domain service/controller references |
| V1.8.0 versioned upgrade | PASS | executed twice on SQL Server 2022 |
| `database/run-phase9.sql` | PASS | executed twice on SQL Server 2022 |
| V1.8.0 ledger | PASS | exactly one row |
| Phase 9 tables/procedures | PASS | verified on SQL Server 2022 |
| Canonical fresh install | PASS | V1.7.0 + V1.8.0 and both retail/platform domains verified |

## SQL-only independent validation

A separate database-only gate was added so the manual installer can be proven independently of .NET/Angular execution.

- Workflow: `Phase 9 SQL Only Validate`
- Successful run: `32655635075`
- `database/run-phase9.sql` first execution: PASS
- `database/run-phase9.sql` second execution: PASS
- all Phase 9 read stored procedures were executed as smoke tests
- `DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS`: PASS
- final marker: `PHASE9_SQL_ONLY_VALIDATION_GREEN`

A read-only local/server verification script is also provided at `database/verify-phase9.sql`.

## Database objects

### New tables

- `dbo.PlatformInvoices`
- `dbo.PlatformInvoiceItems`
- `dbo.PlatformPayments`

### Extended baseline objects

- `dbo.SubscriptionPlans`: description, currency, trial days, staff quota, feature-limit JSON and display order
- `dbo.Tenants`: staff quota
- `dbo.TenantSubscriptions`: trial/current-period/cancellation lifecycle fields

### Stored procedures

- `dbo.PlatformBilling_Plan_List`
- `dbo.PlatformBilling_Subscription_List`
- `dbo.PlatformBilling_Invoice_List`
- `dbo.PlatformBilling_Invoice_Get`
- `dbo.PlatformBilling_Payment_List`
- `dbo.TenantPlatformBilling_Summary_Get`

## Security and domain-boundary validation

- Tenant-facing billing requests do not accept browser `TenantId`.
- Tenant billing endpoints are read-only and use tenant-scope `TenantPlatformBilling.*` permissions.
- Platform Admin mutation endpoints use platform-scope `PlatformBilling.*` permissions.
- Platform invoice/payment entities and SQL do not reference `RetailInvoices` or `RetailInvoicePayments`.
- Phase 8 retail services/controllers do not reference Phase 9 platform billing entities.
- Cross-tenant service tests remain part of the integration suite.
- Plan quota enforcement includes staff count.
- payment `TransactionReference` is the callback idempotency boundary.

## Defects found and corrected during validation

1. SQL Server first-batch `MaxStaff` compilation failure (`Msg 207`) was fixed by moving references to newly added columns behind dynamic SQL where required.
2. Tenant/platform permission names were separated globally to prevent scope ambiguity.
3. .NET analyzer range guards were updated to analyzer-safe .NET 8 guard APIs.
4. Payment refund transition was corrected so successful paid amount is reversed safely when a successful payment becomes refunded.
5. Staff quota enforcement was added to staff creation/reactivation paths.
6. Tenant Platform Billing API contract tests were aligned to the new tenant-specific permission names without weakening authorization.
7. Tenant billing Playwright navigation was changed to verified SPA navigation and a dashboard Platform Billing entry was added; all 27 Phase 5–9 scenarios then passed.

## Canonical database

`database/CustSearchAi.sql` contains the validated `V1.8.0` Phase 9 block. The Phase 9 canonical persistence commit was generated only after the validation workflow reached the SQL/canonical gates.

## Local database limitation

The private SQL Server instance `KRUTARTH-BHAVSA` is not network-accessible from GitHub Actions or this environment, so this report does **not** claim direct execution against that private machine. The exact SQL Server 2022 schema is proven in CI. For the user's machine, run `database/run-phase9.sql` and then `database/verify-phase9.sql`; the verifier performs no schema mutation and reports missing Phase 9 objects if any.

## Final verdict

Phase 9 technical implementation and validation are **PASS / GREEN**. It is safe to use as the baseline for Phase 10 after the final validated corrective commits are merged into `AIMainBranch` and the branch head is verified.
