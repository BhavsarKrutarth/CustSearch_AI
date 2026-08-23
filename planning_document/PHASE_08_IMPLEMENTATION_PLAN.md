# Phase 8 — Products & Retail Billing Implementation Plan

**Status:** Completed  
**Database target:** SQL Server 2022 / V1.7.0  
**Branch:** `phase8-products-retail-billing`  
**Merged baseline:** `AIMainBranch` / `f13391bb1e924e5c6d06cd4a05ddf00538726bf9`  
**Source:** `planning_document/phase_implementation/PHASE_08_PRODUCTS_RETAIL_BILLING.md`  
**Final evidence:** `planning_document/PHASE_08_TEST_REPORT.md`

## Objective

Deliver a factual, tenant/store-safe retail product and billing foundation without changing completed Phase 5–7 semantics. Retail transactions remain separate from Phase 9 platform billing.

## Non-negotiable rules

- `TenantId` is derived from authenticated server context and is absent from browser request DTOs.
- Store authorization is server authoritative and is applied before search/report paging or aggregation.
- SQL deployment uses versioned scripts; no production EF Core migrations.
- Product/category changes never rewrite finalized invoice-line snapshots.
- Browser-calculated totals are advisory only; server/database calculations are authoritative.
- Visit Party / Co-Visit does not prove Household/family membership.
- Anonymous visitors are not invoice participants until explicitly converted to Customers.
- Spend attribution is explicit and auditable; no face/proximity-based attribution exists.

## Sub-phase tracker

| Sub-phase | Scope | Final status |
|---|---|---|
| 8A | Products + ProductCategories reuse + store availability | Completed |
| 8B | Retail invoice header/lifecycle | Completed |
| 8C | Immutable invoice item snapshots | Completed |
| 8D | Payments | Completed |
| 8E | Explicit invoice participants | Completed |
| 8F | Explicit spend attribution | Completed |
| 8G | Customer + verified-household purchase history | Completed |
| 8H | Tenant retail reports + deployment/E2E | Completed |

## Database objects

Validated V1.7.0 objects:

- `dbo.Products`
- `dbo.ProductStoreAvailabilities`
- `dbo.RetailInvoices`
- `dbo.RetailInvoiceItems`
- `dbo.RetailInvoicePayments`
- `dbo.RetailInvoiceParticipants`
- `dbo.RetailInvoiceItemAttributions`

Stored procedures:

- `dbo.Product_Search`
- `dbo.RetailInvoice_Search`
- `dbo.RetailInvoice_GetDetail`
- `dbo.CustomerPurchaseHistory_Get`
- `dbo.HouseholdPurchaseSummary_Get`
- `dbo.RetailSalesSummary_Get`
- `dbo.RetailSalesByProduct_Get`
- `dbo.RetailSalesByCategory_Get`
- `dbo.RetailPaymentSummary_Get`

Deployment files:

- `database/09_Upgrade/V1.7.0_Phase8_ProductsRetailBilling.sql`
- `database/run-phase8.sql`
- `database/CustSearchAi.sql`

## API scope

- Product list/detail/create/update/store assignment.
- Retail invoice list/detail/create/update/finalize/cancel.
- Payment recording.
- Explicit participant and item-attribution writes.
- Customer purchase history and verified-household purchase summary.
- Tenant/store-scoped retail report endpoints.

## Angular scope

Routes:

- `/customer-admin/products`
- `/customer-admin/retail/invoices`
- `/customer-admin/retail/invoices/new`
- `/customer-admin/retail/invoices/:id`
- `/customer-admin/retail/reports`

Customer Smart Profile and Household Detail are enriched with factual purchase sections. Payer spend, explicit attributed spend and verified-household attributed spend remain separate concepts.

## Security / privacy gates

Negative validation covers fake TenantId, unauthorized StoreId, cross-tenant Product/Customer/Household/VisitParty/Visit references, manipulated totals, invalid payments, unauthorized cancellation and cross-tenant attribution.

## Final validation evidence

Authoritative workflow: `Phase 8 Validate` run **#17**, run id **32651044113**, validated head `c4a1599fff5d8f445e5a92299993beebbea545f4`.

- Release .NET build: PASS — 0 warnings / 0 errors.
- Unit tests: PASS — 51/51.
- Integration tests: PASS — 91/91.
- Angular lint: PASS.
- Angular tests: PASS — 50/50.
- Angular production build: PASS.
- Full Phase 5–8 Playwright: PASS — 23/23.
- Python Ruff: PASS.
- Python pytest: PASS — 3/3.
- V1.7.0 upgrade executed twice on SQL Server 2022: PASS.
- `database/run-phase8.sql` executed twice from V1.6.0 baseline: PASS.
- Exactly one V1.7.0 version row: PASS.
- Final `database/CustSearchAi.sql` fresh install through V1.7.0: PASS.

The merged `AIMainBranch` integration commit has no file differences from the validated Phase 8 head.

## Completion summary

Phase 8 is Completed and is the validated integration baseline for Phase 9. Phase 8 Retail Billing remains strictly the shop-customer purchase domain; Phase 9 Platform Billing must use separate subscription, invoice, payment, report and permission models.
