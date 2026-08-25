# Phase 8 — Products & Retail Billing

Status: Completed

## Source of Truth

- `planning_document/CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`
- `planning_document/PHASE_08_IMPLEMENTATION_PLAN.md`
- `planning_document/PHASE_08_TEST_REPORT.md`

## Objective

Deliver a tenant/store-safe retail catalog and factual billing foundation without changing the identity/privacy rules established in Phases 6 and 7. Retail purchase data is financial fact; Visit Party / Co-Visit evidence never creates Household/family truth and never creates spend attribution by itself.

## Completed Scope

### 8A — Product Catalog

Completed and validated.

- Reuses the Phase 5 `ProductCategories` taxonomy instead of introducing a duplicate category master.
- Tenant-owned products with SKU/barcode, brand, unit, sale/cost values and tax percentage.
- Optional explicit product/store availability.
- Historical products are deactivated rather than deleted from financial history.

### 8B — Retail Invoices

Completed and validated.

- Tenant/store-scoped invoice header.
- Server-generated invoice number.
- Optional explicit Customer, verified Household, CustomerVisit and VisitParty references remain separate fields.
- Draft/finalized/partially-paid/paid/cancelled lifecycle.
- Financial rows remain auditable instead of being hard-deleted.

### 8C — Invoice Item Snapshots

Completed and validated.

- Product/category name/code snapshots are persisted with each invoice line.
- Quantity, unit price, discount, tax and line totals are server calculated.
- Later catalog edits do not rewrite historic invoice item facts.

### 8D — Payments

Completed and validated.

- Append-only retail payment facts.
- Successful payments drive `PaidAmount` / `BalanceAmount`.
- Negative, duplicate-reference and excess payment protection.
- Paid invoices cannot be cancelled through the ordinary cancellation path without refund/void handling.

### 8E — Participants

Completed and validated.

- Explicit known-Customer participants only.
- One explicit payer constraint.
- AnonymousVisitor conversion remains the explicit Phase 6 flow.
- VisitParty membership is never automatically copied into invoice participants.

### 8F — Spend Attribution

Completed and validated.

- Explicit invoice-item/customer attribution.
- Attribution source and actor are retained for auditability.
- Item-level attributed amount cannot exceed the factual invoice-item total.
- Payer spend, participant linkage, explicit attributed spend and Household aggregation remain distinct concepts.

### 8G — Purchase History

Completed and validated.

- Customer purchase history separates payer spend and explicit attributed spend.
- Household purchase summary aggregates explicit attribution only through active verified `HouseholdMembers`.
- VisitParty / Co-Visit is not used as a Household substitute.

### 8H — Tenant Retail Reports

Completed and validated.

Dapper/stored-procedure read paths:

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

Version: `V1.7.0`

Files:

- `database/09_Upgrade/V1.7.0_Phase8_ProductsRetailBilling.sql`
- `database/run-phase8.sql`
- `database/CustSearchAi.sql`

Phase 8 tables:

- `Products`
- `ProductStoreAvailabilities`
- `RetailInvoices`
- `RetailInvoiceItems`
- `RetailInvoicePayments`
- `RetailInvoiceParticipants`
- `RetailInvoiceItemAttributions`

`database/run-phase8.sql` is a standalone SSMS/Azure Data Studio T-SQL runner. It verifies the Phase 7 V1.6.0 prerequisite, applies Phase 8 objects idempotently, validates required indexes/FKs/SPs/store predicates and requires exactly one V1.7.0 ledger entry.

## API / Angular

Implemented Phase 8 tenant APIs include product CRUD/store visibility, retail invoice search/detail/create/update/finalize/cancel, payments, participants, spend attribution, customer purchase history, Household purchase summary and retail reports. Request DTOs do not expose TenantId.

Angular routes:

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

## Final Validation Evidence

`Phase 8 Validate` run **#17** / **32651044113** completed successfully on validated head `c4a1599fff5d8f445e5a92299993beebbea545f4`.

- .NET Release: 0 warnings / 0 errors.
- Unit: 51/51.
- Integration: 91/91.
- Angular lint: green.
- Angular unit: 50/50.
- Angular production build: green.
- Playwright Phase 5–8: 23/23.
- Python Ruff: green.
- Python pytest: 3/3.
- SQL Server 2022 V1.7.0 upgrade: twice, green.
- `database/run-phase8.sql`: twice, green.
- Exactly one V1.7.0 DatabaseVersions row: green.
- Canonical fresh install through V1.7.0: green.

## Done Summary

Phase 8 is Completed and merged to `AIMainBranch`. It is the validated baseline for Phase 9. Retail Billing remains only the shop-customer purchase domain and must never be reused for Platform Billing subscriptions, platform invoices or platform payments.
