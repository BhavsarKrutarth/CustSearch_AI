# Phase 8 — Products & Retail Billing Test Report

**Status:** GREEN / Completed  
**Validated head:** `c4a1599fff5d8f445e5a92299993beebbea545f4`  
**Merged integration baseline:** `f13391bb1e924e5c6d06cd4a05ddf00538726bf9` (`AIMainBranch`)  
**Validation workflow:** `Phase 8 Validate` run **#17**, run id **32651044113**  
**Validation date:** 2026-08-23  
**Database:** SQL Server 2022 / `V1.7.0`

## Baseline integrity

- Phase 8 PR #8 was merged to `AIMainBranch`.
- `c4a1599fff5d8f445e5a92299993beebbea545f4` and merged integration commit `f13391bb1e924e5c6d06cd4a05ddf00538726bf9` have no file differences; the merge commit only advances history.
- Canonical `database/CustSearchAi.sql` contains the persisted Phase 8 / V1.7.0 schema after validation.

## Final validation matrix

| Gate | Result |
|---|---|
| .NET Release build | PASS — 0 warnings, 0 errors |
| .NET Unit tests | PASS — 51/51 |
| .NET Integration tests | PASS — 91/91 |
| Angular lint | PASS |
| Angular unit tests | PASS — 50/50 |
| Angular production build | PASS |
| Playwright Phase 5–8 regression | PASS — 23/23 |
| Python Ruff | PASS |
| Python pytest | PASS — 3/3 |
| Phase 8 SQL structure checks | PASS |
| V1.7.0 upgrade on SQL Server 2022 — run 1 | PASS |
| V1.7.0 upgrade on SQL Server 2022 — run 2 | PASS |
| `database/run-phase8.sql` — run 1 | PASS |
| `database/run-phase8.sql` — run 2 | PASS |
| Exactly one V1.7.0 `DatabaseVersions` row | PASS |
| Final canonical fresh install through V1.7.0 | PASS |

## SQL Server 2022 evidence

The final workflow used `mcr.microsoft.com/mssql/server:2022-latest` and validated the following Phase 8 tables:

- `Products`
- `ProductStoreAvailabilities`
- `RetailInvoices`
- `RetailInvoiceItems`
- `RetailInvoicePayments`
- `RetailInvoiceParticipants`
- `RetailInvoiceItemAttributions`

Validated stored procedures:

- `Product_Search`
- `RetailInvoice_Search`
- `RetailInvoice_GetDetail`
- `CustomerPurchaseHistory_Get`
- `HouseholdPurchaseSummary_Get`
- `RetailSalesSummary_Get`
- `RetailSalesByProduct_Get`
- `RetailSalesByCategory_Get`
- `RetailPaymentSummary_Get`

The standalone runner printed `Phase 8 SQL Server script completed and validated successfully.` on both executions and preserved exactly one `V1.7.0` ledger row.

## Security / financial integrity coverage

Validation retained the Phase 8 rules that:

- `TenantId` is server-derived and not trusted from Angular payloads.
- Store scope is server-authoritative.
- Browser totals are not financial authority.
- Retail invoice item snapshots remain factual historical records.
- Visit Party / Co-Visit does not establish Household/family truth.
- Participants and spend attribution require explicit factual linkage.
- Cross-tenant/store billing access remains denied.

## Completion decision

All Phase 8 completion gates are green. Phase 8 is a validated integration baseline for Phase 9.

**Important domain boundary for the next phase:** Phase 8 **Retail Billing** represents shop-customer purchases. Phase 9 **Platform Billing** represents tenant/shop-owner subscription payments to CustSearch. Their tables, invoices, payments, reports and permissions must remain separate.
