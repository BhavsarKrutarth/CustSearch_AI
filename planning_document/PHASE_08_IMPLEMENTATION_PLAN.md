# Phase 8 — Products & Retail Billing Implementation Plan

**Status:** In Progress  
**Database target:** SQL Server 2022 / V1.7.0  
**Branch:** `phase8-products-retail-billing`  
**Source:** `planning_document/phase_implementation/PHASE_08_PRODUCTS_RETAIL_BILLING.md`

## Objective

Deliver a factual, tenant/store-safe retail product and billing foundation without changing completed Phase 5–7 semantics. Retail transactions remain separate from future Phase 9 platform billing.

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

| Sub-phase | Scope | Current status | Completion gate |
|---|---|---|---|
| 8A | Products + ProductCategories reuse + store availability | Implemented, validation pending | CRUD/search + tenant/store isolation + SQL green |
| 8B | Retail invoice header/lifecycle | Implemented, validation pending | calculations/lifecycle/concurrency tests green |
| 8C | Immutable invoice item snapshots | Implemented, validation pending | snapshot + arithmetic tests green |
| 8D | Payments | Implemented, validation pending | positive/idempotent/store-safe payment tests green |
| 8E | Explicit invoice participants | Implemented, validation pending | known-customer + single-payer tests green |
| 8F | Explicit spend attribution | Implemented, validation pending | attribution cap/privacy/isolation tests green |
| 8G | Customer + verified-household purchase history | Implemented, validation pending | factual separated spend views green |
| 8H | Tenant retail reports + deployment/E2E | In progress | Dapper/SP + Angular + SQL Server 2022 + full regression green |

## Database objects

Planned/implemented V1.7.0 objects:

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

Required negative tests include fake TenantId, unauthorized StoreId, cross-tenant Product/Customer/Household/VisitParty/Visit references, manipulated totals, invalid payments, unauthorized cancellation, and cross-tenant attribution.

## Validation gates

Phase 8 can become **Completed** only after actual evidence confirms:

1. Release .NET build green.
2. Unit and integration tests green.
3. Angular pinned versions, lint, unit tests and production build green.
4. Full Phase 5/6/7/8 Playwright suite green.
5. Python Ruff + pytest regression green.
6. V1.7.0 applies twice on SQL Server 2022.
7. `database/run-phase8.sql` applies twice from V1.6.0 baseline.
8. Exactly one V1.7.0 version row exists.
9. Final `database/CustSearchAi.sql` fresh-installs through V1.7.0.
10. Tenant/store, financial-integrity, participant and attribution privacy/security gates are green.
11. `planning_document/PHASE_08_TEST_REPORT.md` contains exact evidence.

No test counts in this document are claimed until the corresponding workflow has completed successfully.
