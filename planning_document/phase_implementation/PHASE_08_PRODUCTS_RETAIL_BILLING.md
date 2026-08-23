# Phase 8 — Products & Retail Billing

Status: In Progress

## Source of Truth

- `planning_document/CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`
- `planning_document/PHASE_08_IMPLEMENTATION_PLAN.md`

## Objective

Deliver a tenant/store-safe retail catalog and factual billing foundation without changing the identity/privacy rules established in Phases 6 and 7. Retail purchase data is financial fact; Visit Party / Co-Visit evidence never creates Household/family truth and never creates spend attribution by itself.

## Scope

### 8A — Product Catalog

Implemented, validation in progress.

- Reuses the Phase 5 `ProductCategories` taxonomy instead of introducing a duplicate category master.
- Tenant-owned products with SKU/barcode, brand, unit, sale/cost values and tax percentage.
- Optional explicit product/store availability.
- Historical products are deactivated rather than deleted from financial history.

### 8B — Retail Invoices

Implemented, validation in progress.

- Tenant/store-scoped invoice header.
- Server-generated invoice number.
- Optional explicit Customer, verified Household, CustomerVisit and VisitParty references remain separate fields.
- Draft/finalized/partially-paid/paid/cancelled lifecycle.
- Financial rows remain auditable instead of being hard-deleted.

### 8C — Invoice Item Snapshots

Implemented, validation in progress.

- Product/category name/code snapshots are persisted with each invoice line.
- Quantity, unit price, discount, tax and line totals are server calculated.
- Later catalog edits do not rewrite historic invoice item facts.

### 8D — Payments

Implemented, validation in progress.

- Append-only retail payment facts.
- Successful payments drive `PaidAmount` / `BalanceAmount`.
- Negative, duplicate-reference and excess payment protection.
- Paid invoices cannot be cancelled through the ordinary cancellation path without refund/void handling.

### 8E — Participants

Implemented, validation in progress.

- Explicit known-Customer participants only.
- One explicit payer constraint.
- AnonymousVisitor conversion remains the explicit Phase 6 flow.
- VisitParty membership is never automatically copied into invoice participants.

### 8F — Spend Attribution

Implemented, validation in progress.

- Explicit invoice-item/customer attribution.
- Attribution source and actor are retained for auditability.
- Item-level attributed amount cannot exceed the factual invoice-item total.
- Payer spend, participant linkage, explicit attributed spend and Household aggregation remain distinct concepts.

### 8G — Purchase History

Implemented, validation in progress.

- Customer purchase history separates payer spend and explicit attributed spend.
- Household purchase summary aggregates explicit attribution only through active verified `HouseholdMembers`.
- VisitParty / Co-Visit is not used as a Household substitute.

### 8H — Tenant Retail Reports

Implemented, validation in progress.

Dapper/stored-procedure read paths include:

- `dbo.Product_Search`
- `dbo.RetailInvoice_Search`
- `dbo.RetailInvoice_GetDetail`
- `dbo.CustomerPurchaseHistory_Get`
- `dbo.HouseholdPurchaseSummary_Get`
- `dbo.RetailSalesSummary_Get`
- `dbo.RetailSalesByProduct_Get`
- `dbo.RetailSalesByCategory_Get`
- `dbo.RetailPaymentSummary_Get`

TenantId and authorized StoreIds are supplied from the authenticated server context and applied before paging/aggregation.

## Database

Version target: `V1.7.0`

Files:

- `database/09_Upgrade/V1.7.0_Phase8_ProductsRetailBilling.sql`
- `database/run-phase8.sql`
- `database/CustSearchAi.sql` — Phase 8 block must be persisted only after full V1.7.0 validation is green.

Phase 8 tables:

- `Products`
- `ProductStoreAvailabilities`
- `RetailInvoices`
- `RetailInvoiceItems`
- `RetailInvoicePayments`
- `RetailInvoiceParticipants`
- `RetailInvoiceItemAttributions`

`database/run-phase8.sql` is a standalone SSMS/Azure Data Studio T-SQL runner. It verifies the Phase 7 V1.6.0 prerequisite, creates/updates Phase 8 objects idempotently, validates required indexes/FKs/SPs/store predicates and requires exactly one V1.7.0 ledger entry.

## API / Angular

Implemented Phase 8 tenant APIs include product CRUD/store visibility, retail invoice search/detail/create/update/finalize/cancel, payments, participants, spend attribution, customer purchase history, Household purchase summary and retail reports. Request DTOs do not expose TenantId.

Angular routes include:

- `/customer-admin/products`
- `/customer-admin/retail/invoices`
- `/customer-admin/retail/invoices/new`
- `/customer-admin/retail/invoices/:id`
- `/customer-admin/retail/reports`

Customer Smart Profile and Household Detail have factual purchase sections; payer, attributed and verified-Household figures remain separately labelled.

## Security / Privacy Gates

- TenantId is server-derived and never trusted from browser payloads.
- Store-scoped access uses authoritative `StoreIds` from the authenticated context.
- Cross-tenant/store Product, Customer, Household, Visit and VisitParty links are rejected.
- Browser-calculated totals are not accounting authority.
- Co-visit does not mean family.
- VisitParty does not create Household membership.
- Face/proximity tracking does not create invoice participants or spend attribution.
- Anonymous visitors require explicit Customer conversion before Customer-only billing associations.

## Current Validation Evidence

Current Phase 8 testing is still running. Evidence already observed on the Phase 8 branch:

- .NET Release build: green after correcting a CA1512 range-guard analyzer issue.
- .NET Unit tests: 51 passed / 0 failed on the tested Phase 8 head.
- .NET Integration tests: 91 passed / 0 failed on the tested Phase 8 head.
- Angular lint initially exposed one unused import in `retail-api.service.ts`; the production source was corrected and a full rerun is in progress.
- SQL Server 2022 V1.7.0 double-run, standalone runner double-run, Playwright, Python and final canonical install remain required completion gates until the current final-head workflow proves them green.

## Completion Gate

Phase 8 must not be marked Completed until all are green on the final implementation head:

1. .NET Release build with 0 errors.
2. Full Unit + Integration suites.
3. Angular pinned-version validation, lint, tests and production build.
4. Full Phase 5/6/7/8 Playwright regression.
5. Python Ruff + pytest regression.
6. V1.7.0 upgrade executed twice on SQL Server 2022.
7. `database/run-phase8.sql` executed twice on SQL Server 2022.
8. Exactly one V1.7.0 `DatabaseVersions` row.
9. Tenant/store/security/privacy and financial-integrity assertions.
10. Final canonical fresh install through V1.7.0.
11. `planning_document/PHASE_08_TEST_REPORT.md` containing exact final evidence.

## Done Summary

Implementation is substantially present; validation and final canonical persistence are still in progress. Do not merge PR #8 or mark this phase Completed until the final Phase 8 validation matrix is green.
