# Phase 7 — Households & Visits

Status: In Progress

Started: 2026-08-23 (Asia/Kolkata)

Source of truth: `../CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`

Implementation plan: `../PHASE_07_IMPLEMENTATION_PLAN.md`

## Scope

Phase 7 adds verified household/family management, household members, visit parties/co-visit evidence, and factual customer visit history on top of the completed Phase 6 customer/anonymous-visitor foundation.

Core persistence:

- `Households`
- `HouseholdMembers`
- `VisitParties`
- `VisitPartyMembers`
- `CustomerVisits`

## Architecture & Privacy Rules

1. Household/family and Visit Party are separate domain concepts.
2. Co-visit evidence does not prove a family relationship.
3. Do not infer family from face similarity, facial resemblance, physical proximity, repeated co-visits, or CCTV alone.
4. AI may create/suggest Visit Party/co-visit evidence only.
5. Household membership requires an explicit verified/customer-provided relationship source.
6. Every detected person keeps a separate `AnonymousVisitor` or `Customer` identity.
7. Unknown people stay `AnonymousVisitor` until the explicit Phase 6 conversion workflow is used.
8. Household members reference customers; anonymous visitors are not silently linked as family members.
9. No Aadhaar/PAN/social/external identity lookup from face data.
10. TenantId is server-derived and never accepted from Angular payloads.
11. Store scope comes from the authenticated user's authoritative StoreIds.
12. Phase 7 visit history is factual operational history; purchases/invoices remain Phase 8+ concerns.

## Sub-Phase Status

| Sub-phase | Scope | Completed | Pending | Issues | Validation | Progress |
|---|---|---|---|---|---|---:|
| 7A | Household Management | Planning/contracts started | Domain, DB, service, API, Angular | None known | Unit/integration/UI/E2E | 10% |
| 7B | Household Members & Verified Relationships | Relationship/privacy rules defined | Member add/remove/update/verification | None known | Duplicate/verification/isolation tests | 10% |
| 7C | Visit Parties / Co-Visit Evidence | Party/family separation defined | Party/member model, search/detail, UI | None known | Identity XOR + store isolation | 10% |
| 7D | Customer Visits | Factual visit boundary defined | Visit model/search/API/UI | None known | Search/date/store tests | 10% |
| 7E | Household Angular UI | Route/permission plan defined | Screens/services/tests | None known | Angular + Playwright | 5% |
| 7F | Visits / Visit Party Angular UI | Route/label plan defined | Screens/services/tests | None known | Angular + Playwright | 5% |
| 7G | Tenant Isolation, Store Authorization & Privacy | Server authority rules defined | Full enforcement/regression | None known | .NET/E2E/SQL | 10% |
| 7H | Database, E2E, Documentation & Completion | V1.6.0 work started | SQL runner/canonical/workflow/full evidence | None known | SQL 2022 double-run + fresh install | 5% |

## Planned Database Objects

### Households

Tenant-owned household master with stable household code/name, optional notes, active state and UTC audit timestamps.

### HouseholdMembers

Explicit customer-to-household relationship with relationship type/source, verification state, verification actor/time and active state. It never stores a face-derived family assertion.

### VisitParties

Store-bound observed/co-visit grouping with party code, start/end timestamps, source/status and audit timestamps. A Visit Party is evidence that identities visited together, not evidence that they are family.

### VisitPartyMembers

Membership uses an explicit identity type and exactly one linked identity: CustomerId or AnonymousVisitorId. Database check constraints prevent dual/no-identity rows.

### CustomerVisits

Tenant/store/customer factual visit record with visit code, entered/exited timestamps, optional VisitPartyId, source/status and audit timestamps.

## Planned Stored Procedures

- `dbo.Household_Search`
- `dbo.Household_GetDetail`
- `dbo.CustomerVisit_Search`
- `dbo.VisitParty_Search`
- `dbo.VisitParty_GetDetail`

All searches enforce TenantId and allowed StoreIds before pagination.

## Planned API Surface

Households:

- `GET /api/tenant/households`
- `GET /api/tenant/households/{householdId}`
- `POST /api/tenant/households`
- `PUT /api/tenant/households/{householdId}`
- `POST /api/tenant/households/{householdId}/members`
- `PUT /api/tenant/households/{householdId}/members/{customerId}`
- `DELETE /api/tenant/households/{householdId}/members/{customerId}`

Visits:

- `GET /api/tenant/visits`
- `GET /api/tenant/visits/{visitId}`
- trusted/manual write route only if required by the implemented application boundary and `Visits.Edit` permission.

Visit parties:

- `GET /api/tenant/visit-parties`
- `GET /api/tenant/visit-parties/{partyId}`

## Permissions

Existing Phase 7-ready permission names are reused where present:

- `Households.View`
- `Households.Create`
- `Households.Edit`
- `Households.ManageMembers`
- `Visits.View`
- `Visits.Edit`

Add `VisitParties.View` only if a separate visit-party endpoint/UI needs an explicit permission boundary.

## Angular Routes

- `/customer-admin/households`
- `/customer-admin/households/:id`
- `/customer-admin/visits`
- `/customer-admin/visit-parties`

Visit-party UI must display `Visit Party / Co-Visit`; automatically detected parties must never be labeled `Family` unless a separately verified Household exists.

## Audit Requirements

Household create/edit and household member relationship changes record the authenticated actor and correlation context through the existing tenant audit mechanism. Trusted/manual visit-party or visit edits also use the same audit boundary.

## Test Matrix

- Household create/update/search/detail
- Member add/update/remove
- Duplicate member rejection
- Verification source/state validation
- AnonymousVisitor cannot directly become a household member
- Visit search/detail
- Visit Party search/detail
- VisitPartyMember identity XOR enforcement
- Tenant A cannot access Tenant B household/visit/party
- Store-scoped users cannot access resources outside assigned StoreIds
- Angular does not send TenantId
- Permission guards and API permissions
- Existing Phase 5/6 regression remains green
- V1.6.0 applies twice on SQL Server 2022
- Final canonical fresh install contains V1.4.0/V1.5.0/V1.6.0 exactly once

## Done Summary

In progress. Final test counts and commit evidence will be recorded only after the complete Phase 7 validation workflow passes.