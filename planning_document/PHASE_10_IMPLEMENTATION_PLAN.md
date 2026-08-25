# Phase 10 — Preferences & Staff Voice Tagging Implementation Plan

**Status:** Completed  
**Branch:** `phase10-preferences-voice`  
**Validated baseline:** `AIMainBranch` at `856e19a159c4d38f9e8c469e81d9ee332c58b47e`  
**Database target:** SQL Server 2022 / V1.9.0

## Source of truth

- `planning_document/phase_implementation/PHASE_10_PREFERENCES_VOICE.md`
- `planning_document/CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`
- completed Phase 5–9 implementation and validation evidence

## Baseline gate

Phase 10 starts only after Phase 9 full validation and merge. The prerequisite is satisfied by Phase 9 Validate run `32678655391` plus the required Phase 6 regression run `32678655390`, both green, and Phase 9 corrective merge commit `856e19a159c4d38f9e8c469e81d9ee332c58b47e` on `AIMainBranch`.

## Architecture rules

1. Factual preference signals are stored separately from derived/recalculated scores.
2. Phase 8 finalized factual retail billing is the only purchase source; Phase 10 never invents purchases.
3. Household preference summaries aggregate only active **verified** `HouseholdMembers`.
4. `VisitParty` / Co-Visit never creates Household truth or a Household preference.
5. Shared Household tags are explicit operator/customer records, not CCTV/co-visit inference.
6. Existing Phase 5 `StoreVoiceCommandSetting` and `StoreVoiceCommandAlias` are extended/reused; no duplicate voice-settings master is created.
7. `Aasha Add` is an example/default only. Runtime trigger/aliases are store-specific and server-authorized.
8. Voice recognition produces a proposal. A CRM write is performed only after confirmation when confirmation is configured/required.
9. TenantId is derived from the authenticated server context and never accepted from Angular DTOs.
10. Store authorization is checked from authenticated `StoreIds` before customer/tag/voice operations.
11. Existing tenant `AuditLog` is reused for manual tags, settings/alias changes, voice commands, recalculation and overrides.
12. No EF production migrations; all schema deployment is versioned/idempotent T-SQL.

## 10A — Customer preferences

Create factual `CustomerPreferenceSignals` with CustomerId, type, reference/value, source, optional signal score/confidence, first/last observed timestamps, active state, store/actor/reason metadata. Create separate `CustomerPreferenceScores` for derived scores and calculation-version evidence.

Supported initial preference types include Category, Product, Brand, PriceRange and Tag. Supported factual sources include ManualStaff, Purchase, CategoryInteraction and VoiceConfirmed; future CCTV dwell evidence can be introduced by the later CCTV phase without changing the identity boundary.

## 10B — Household preferences

Add read model for:
- verified-member customer preferences
- deterministic aggregated Household scores
- explicit shared Household tags

Create `HouseholdPreferenceTags` only for explicit shared tags. Household calculations must join `HouseholdMembers` with `IsVerified=1` and `IsActive=1`; no VisitParty joins are permitted.

## 10C — Manual staff tagging

Authorized staff/admin can add/update factual Customer tags/interests for a customer visible in their authorized store scope. Record actor, StoreId, CustomerId, type/reference/value, reason and UTC timestamp. Manual changes never overwrite historical purchase/AI signals.

## 10D — Dynamic store voice command

Extend Phase 5 StoreVoiceCommandSettings with:
- language code
- confirmation requirement
- listening timeout seconds
- minimum recognition confidence

Keep TriggerKeyword, enabled state, existing ambiguity confirmation and aliases. Trigger matching is store-specific and normalized; `Aasha Add` is not hard-coded in the parser.

## 10E — Voice command flow

Persist `VoiceCommandSessions` containing tenant/store/staff/customer, matched trigger, recognized phrase/confidence, proposed preference/category, status, expiration and resolution timestamps.

Flow:
1. start against an authorized Store + Customer
2. validate configured trigger/alias and confidence
3. interpret recognized factual category/tag proposal
4. return confirmation-required state
5. Confirm creates the preference signal and audit record; Reject/Expire creates no preference write

## 10F — Recalculation engine

Create versioned `PreferenceWeightVersions`. Recalculation is deterministic for the same signals + weight version. Initial sources may include explicit/manual tags, confirmed voice signals and factual Phase 8 purchase/category evidence. Derived score output is stored separately in `CustomerPreferenceScores`.

## 10G — Audit and security

Reuse `AuditLogs` and enforce:
- customer tenant/store visibility
- household tenant scope and verified membership
- voice StoreId authorization
- actor/staff server identity
- no browser TenantId
- no VisitParty-family inference

## 10H — Angular

Add:
- Customer Preferences section and manual tag controls
- Household Preferences section with verified-member label
- Voice Trigger Settings enhancements and aliases
- Voice command confirmation UI
- recalculation control where `Preferences.Manage` is granted
- preference/voice audit history

## 10I — Validation

Required automated coverage:
- dynamic per-store trigger
- aliases
- confirmation required
- rejection performs no write
- customer tenant/store isolation
- Household verified members only
- VisitParty not used as family/preference truth
- unauthorized staff/customer rejected
- Angular TenantId absence
- deterministic recalculation
- audit records
- Phase 5–10 Playwright regression
- .NET/Angular/Python regression
- V1.9.0 upgrade twice on SQL Server 2022
- standalone `database/run-phase10.sql` twice
- exactly one V1.9.0 ledger row
- final canonical fresh install through V1.9.0

## Completion rule

Do not mark Phase 10 Completed and do not create a green test report until every required build/test/database gate has actual passing evidence.
