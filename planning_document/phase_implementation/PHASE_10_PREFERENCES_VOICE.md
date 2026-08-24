# Phase 10 — Preferences & Staff Voice Tagging

Status: In Progress

## Source of Truth

- `../CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`
- `../PHASE_10_IMPLEMENTATION_PLAN.md`

## Scope

Customer/household preferences, manual staff tagging, dynamic store-configured voice triggers/aliases, confirmation-controlled voice actions, deterministic recalculation and tenant/store-safe audit.

## Non-negotiable boundaries

- Factual preference signals and inferred/recalculated scores remain separate.
- Phase 8 retail billing is the factual purchase source; Phase 10 does not fabricate purchases.
- Household preference aggregation uses active verified `HouseholdMembers` only.
- Visit Party / Co-Visit does not mean Household/family and cannot create Household preference truth.
- Phase 5 `StoreVoiceCommandSettings` / aliases are reused and extended.
- `Aasha Add` is an example/default only; each Store has its own trigger/aliases.
- Voice recognition creates a proposal, not an automatic CRM write. Confirmation rules are enforced server-side.
- TenantId is server-derived and StoreId/customer access is checked against authenticated scope.
- Existing tenant `AuditLog` is reused for settings, tags, voice actions, recalculation and overrides.
- Database deployment is V1.9.0 idempotent SQL; no EF production migrations.

## Planned database additions

- `CustomerPreferenceSignals`
- `CustomerPreferenceScores`
- `HouseholdPreferenceTags`
- `PreferenceWeightVersions`
- `VoiceCommandSessions`
- Phase 5 `StoreVoiceCommandSettings` extension for language, confirmation, timeout and minimum recognition confidence

## Validation gates

- .NET restore/build/unit/integration
- Angular lint/unit/production build
- Playwright full Phase 5–10 regression
- Python Ruff/pytest
- SQL Server 2022 V1.9.0 upgrade twice
- standalone `database/run-phase10.sql` twice
- exactly one V1.9.0 version row
- final canonical fresh install
- explicit privacy/security tests for verified Household membership, VisitParty separation, store authorization and confirmation/no-write behavior

## Done Summary

In progress. Exact test counts and final completion evidence will be added only after the full Phase 10 validation matrix is green.
