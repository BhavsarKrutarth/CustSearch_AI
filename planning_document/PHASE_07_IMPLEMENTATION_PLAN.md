# Phase 7 — Households & Visits Implementation Plan

Created: 2026-08-23 (Asia/Kolkata)

Status: In Progress

Source of truth: `CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`

Detailed phase file: `phase_implementation/PHASE_07_HOUSEHOLDS_VISITS.md`

## Objective

Deliver the tenant/store-safe household and visit foundation on top of the completed Phase 6 shopper customer layer: verified household relationships, visit parties/co-visit evidence, factual customer visits, Angular management screens, Dapper search procedures, audit behavior, and repeat-safe SQL Server 2022 deployment.

Phase 7 must preserve the distinction between an observed visit party and a verified household. Co-visit evidence, facial resemblance, proximity, or repeated joint visits never prove a family relationship.

## Non-Negotiable Privacy & Identity Rules

- `Household` and `VisitParty` are different concepts.
- AI/CCTV may create or suggest a co-visit party, but it must not create a household relationship by itself.
- Household membership requires a customer-provided, staff-verified, admin-verified, or otherwise explicitly verified relationship source.
- Unknown people remain `AnonymousVisitor` until the explicit Phase 6 conversion workflow creates/links a `Customer`.
- Every person keeps a separate visitor/customer identity; multiple people are never merged into one customer record.
- No Aadhaar, PAN, social-network, or external identity lookup from face data.
- TenantId is resolved from the authenticated server context and is never accepted from Angular payloads.
- Store access uses the server-authoritative StoreIds from the authenticated session.

## Sub-Phase Plan

| Sub-phase | Scope | Completed | Pending | Issues | Validation | Progress |
|---|---|---|---|---|---|---:|
| 7A | Household Management | Plan and contracts defined | Domain/data/service/API/UI implementation | None known | Unit + integration + Angular + E2E | 10% |
| 7B | Household Members & Verified Relationships | Verification-source rules defined | Add/remove/update member workflows and constraints | None known | Relationship/duplicate/cross-tenant tests | 10% |
| 7C | Visit Parties / Co-Visit Evidence | Party != household rule defined | Party/member persistence, search/detail and UI | None known | Identity XOR, tenant/store and labeling tests | 10% |
| 7D | Customer Visits | Factual visit-history rules defined | Visit persistence/search/detail and UI | None known | Tenant/store/date/search tests | 10% |
| 7E | Household Angular UI | Routes/permission intent defined | Typed service, list/detail/create/edit/member UI | None known | Angular tests + Playwright | 5% |
| 7F | Visits / Visit Party Angular UI | Routes/labeling intent defined | Typed services/pages/search/filter/detail | None known | Angular tests + Playwright | 5% |
| 7G | Tenant Isolation, Store Authorization & Privacy | Server-derived tenant/store rules defined | End-to-end enforcement across every resource | None known | Unit/integration/E2E/SQL predicates | 10% |
| 7H | Database, E2E, Documentation & Completion | V1.6.0 design started | Upgrade/direct runner/canonical/workflow/full green evidence | None known | SQL double-run + canonical fresh install + full CI | 5% |

## Database Plan

Version: `V1.6.0`

Primary upgrade: `database/09_Upgrade/V1.6.0_Phase7_HouseholdsVisits.sql`

Direct SSMS runner: `database/run-phase7.sql`

Required Phase 7 tables:

- `dbo.Households`
- `dbo.HouseholdMembers`
- `dbo.VisitParties`
- `dbo.VisitPartyMembers`
- `dbo.CustomerVisits`

Database rules:

- No EF Core migrations or runtime schema creation.
- Repeat-safe `IF OBJECT_ID...IS NULL`, `IF NOT EXISTS`, and `CREATE OR ALTER` patterns.
- Tenant-safe relationships and direct `TenantId` on Phase 7 root/high-volume tables.
- `HouseholdMembers` references an existing `Customer`; it never silently converts an anonymous visitor.
- `VisitPartyMembers` uses an explicit identity type and exactly one of CustomerId/AnonymousVisitorId.
- Version ledger contains exactly one `V1.6.0` row after repeated deployment.

Planned stored procedures:

- `dbo.Household_Search`
- `dbo.Household_GetDetail`
- `dbo.CustomerVisit_Search`
- `dbo.VisitParty_Search`
- `dbo.VisitParty_GetDetail`

Every searchable stored procedure applies TenantId and allowed-store predicates before paging.

## API Plan

Tenant-scoped API groups:

- Households
- Household members
- Customer visits
- Visit parties

Permissions:

- `Households.View`
- `Households.Create`
- `Households.Edit`
- `Households.ManageMembers`
- `Visits.View`
- `Visits.Edit` only for trusted/manual visit writes if exposed
- `VisitParties.View`

Backend authorization remains authoritative even when Angular hides unavailable navigation/actions.

## Angular Plan

Customer Admin routes:

- `/customer-admin/households`
- `/customer-admin/households/:id`
- `/customer-admin/visits`
- `/customer-admin/visit-parties`

UI requirements:

- Typed API services; no direct API calls inside page components.
- Lazy-loaded routes and existing permission guards.
- Household create/edit/list/detail and verified member management.
- Visits list/search/filter/pagination.
- Visit Party list/detail labeled `Visit Party / Co-Visit`; never label an unverified detected party as `Family`.
- Existing light/dark/system theme compatibility.

## Test / Completion Gates

Phase 7 is `Completed` only when all of these are green on the final Phase 7 head:

1. Phase 6 regression remains green: .NET, Angular, Playwright, Python, SQL Server 2022 upgrade/fresh canonical checks.
2. `.NET` restore/build succeeds with zero build errors; all unit/integration tests pass.
3. Angular `npm ci`, lint, unit tests and production build pass.
4. Existing Phase 5/6 Playwright plus new Phase 7 household/visit/party tests pass.
5. Python Ruff and pytest remain green.
6. V1.6.0 applies twice on SQL Server 2022 without duplicates.
7. SQL validation proves all Phase 7 tables, tenant-safe FKs, indexes, permissions and stored procedures.
8. Fresh `database/CustSearchAi.sql` contains V1.4.0, V1.5.0 and V1.6.0 exactly once.
9. Cross-tenant/cross-store access, household verification rules, anonymous-visitor boundary and browser TenantId absence are tested.
10. `PROCESS_TRACKER.md`, phase index and detailed Phase 7 documentation contain actual—not fabricated—test evidence.
11. `phase7-households-visits` is merged into `AIMainBranch` only after all gates are green; the common branches are synchronized only after comparison proves no unique work will be lost.

## Phase 6 Final Gate Used To Start Phase 7

The final Phase 6 validation workflow on the Phase 6 implementation head passed the complete build/test/UI/Python/SQL Server 2022 validation matrix. Phase 7 therefore starts from the merged Phase 6 baseline; documentation-only commits after that validation do not alter runtime code.

## Done Summary

In progress. Do not mark completed until the Phase 7 workflow supplies exact final build/test/E2E/SQL evidence.