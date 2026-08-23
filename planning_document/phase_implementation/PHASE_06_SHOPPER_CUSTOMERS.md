# Phase 6 — Shopper Customers

Status: In Progress

## Source of Truth

Implementation follows `../CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md` and builds on the completed Phase 5 tenant/store authorization foundation in `AIMainBranch`.

## Scope

Shopper customers, anonymous visitors, tenant/store-safe customer search APIs/UI and factual smart-profile foundation with explicit privacy and authorization boundaries.

## Sub-Phase Status

| Sub-phase | Scope | Completed | Pending | Issues | Validation | Progress |
|---|---|---|---|---|---|---:|
| 6A | Customer Management | Customer entity, store assignments, CRUD service/API and audit design implemented | Full CI/SQL proof | None known | Domain/unit + relational service + SQL gates queued | 90% |
| 6B | Anonymous Visitors | Store-bound anonymous visitor model, list/detail, trusted create/touch service and explicit conversion implemented | Full CI/SQL proof | None known | Domain + conversion/isolation regressions queued | 90% |
| 6C | Customer Search | Typed search contracts, Dapper repository and tenant/store-filtered stored procedures implemented | SQL Server execution proof | None known | SP structure/idempotency/real SQL 2022 gate queued | 90% |
| 6D | Smart Customer Profile | Factual identity/contact/store/conversion profile implemented; future Phase 7/8/10 sections explicitly identified without fabricated data | UI/CI proof | None known | API/UI/E2E gate queued | 90% |
| 6E | Angular Customer UI | Lazy customer list/search/create/detail/edit/store-visibility UI and typed client/routes implemented | Angular lint/test/build + Playwright | None known | CI queued | 85% |
| 6F | Angular Visitor UI | Lazy visitor search/list and explicit existing/new-customer conversion UI implemented | Angular lint/test/build + Playwright | None known | CI queued | 85% |
| 6G | Tenant Isolation & Authorization | Server TenantId/StoreIds reuse, SQL pre-paging scope, composite tenant-safe DB FKs and service-boundary regressions implemented | Full regression proof | None known | Unit/integration/E2E/SQL gates queued | 90% |
| 6H | E2E, Database & Documentation | V1.5.0 upgrade, `run-phase6.ps1`, Phase 6 Playwright suite and full validation workflow implemented | Green CI, canonical persistence, final tracker update and merge | Current gate has not run yet | Full gate queued | 70% |

## Architecture / Privacy Decisions

- TenantId is never accepted from Angular customer/visitor forms or query payloads; it is resolved from the validated server session.
- Store-scoped users can see only customers associated with an authoritative allowed store and anonymous visitors from an allowed store.
- `CustomerStoreAssignments` is the Phase 6 visibility relation and preserves inaccessible assignments when a store-scoped administrator edits only their authorized slice.
- Unknown people stay `AnonymousVisitor`. Phase 6 does not store face embeddings, Aadhaar/PAN/social identity, or perform automatic identity lookup.
- Visitor-to-customer conversion is explicit, permission-controlled and audited. Creating a customer during conversion also requires `Customers.Create`; linking an existing customer requires `Customers.Edit`.
- Smart profile shows only data available by Phase 6. Households/visits, billing history and preferences are intentionally deferred to Phases 7, 8 and 10.
- No EF Core migration workflow is introduced. Schema changes use the idempotent V1.5.0 SQL script and canonical `database/CustSearchAi.sql` gate.

## Phase 6 Completion Gates

Phase 6 is `Completed` only when all of the following are green on the Phase 6 head:

1. .NET restore/build with zero build errors and full unit/integration tests.
2. Angular `npm ci`, lint, tests and production build.
3. Phase 5 regression Playwright plus Phase 6 customer/visitor Playwright E2E.
4. Python Ruff and baseline pytest remain green.
5. SQL Server 2022 applies completed Phase 5 state then V1.5.0 twice successfully.
6. V1.5.0 produces exactly one `DatabaseVersions` row and all tenant-safe tables/FKs/search procedures.
7. A fresh SQL Server 2022 install from final `database/CustSearchAi.sql` includes both V1.4.0 and V1.5.0 exactly once.
8. Canonical SQL and pinned E2E lock are persisted only after the validation gates pass.
9. Phase tracker/index are updated and the Phase 6 PR is merged into `AIMainBranch`.

## Done Summary

In progress. Current implementation branch: `phase6-shopper-customers`. After all gates pass, this section will be replaced with exact build/test/E2E/SQL evidence and Phase 7 will start automatically under the user's 2026-08-23 sequential-phase authorization.
