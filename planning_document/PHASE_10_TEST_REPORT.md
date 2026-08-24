# Phase 10 — Preferences & Staff Voice Tagging Test Report

**Status:** PASS / GREEN  
**Validated:** 2026-08-24  
**Branch:** `phase10-preferences-voice`  
**Validated code commit:** `114d201323630eccb965b1f37f00e1a16add706c`  
**Canonical SQL commit:** `0e6841c4f15c57b526334f7b2f2f27f43cd12ad7`  
**Phase 10 workflow:** `Phase 10 Validate` run `32740932609`

## Automated validation

| Gate | Result | Evidence |
|---|---|---|
| .NET restore/build | PASS | Release build completed |
| .NET unit tests | PASS | 71 passed, 0 failed |
| .NET integration tests | PASS | 141 passed, 0 failed |
| Angular lint | PASS | ESLint clean |
| Angular tests | PASS | 59 passed across 25 files |
| Angular production build | PASS | Angular 21.2.21 production bundle completed |
| Playwright regression | PASS | 32 passed, including Phase 5–10 flows |
| Python | PASS | Ruff clean; 3 pytest tests passed |
| Phase 10 security/SQL structure | PASS | tenant/store scope, verified household, server category resolution and standalone-script guards passed |
| V1.9.0 versioned upgrade | PASS | SQL Server 2022 upgrade executed twice; one V1.9.0 ledger row |
| Standalone installer and verifier | PASS | `run-phase10.sql` executed twice, then `verify-phase10.sql` passed |
| Canonical fresh install | PASS | prospective `database/CustSearchAi.sql` installed through V1.9.0 with constraints clean |

Workflow evidence: <https://github.com/BhavsarKrutarth/CustSearch_AI/actions/runs/32740932609>

## Phase 10 regression and security outcomes

- Store voice triggers and aliases are runtime-configured; no parser behavior depends on a hard-coded `Aasha Add` phrase.
- Ambiguous categories require selection from server-resolved candidates and configured confirmation before a CRM signal is written.
- Unknown categories and rejected sessions create no preference signal.
- Browser contracts do not accept or send `TenantId`; tenant, actor and store authorization are server-derived.
- Store-scoped audit reads cannot expose another authorized tenant store when the caller omits a store filter.
- Household aggregation uses active verified household members and does not treat VisitParty/co-visit evidence as family truth.
- Factual signals remain separate from deterministic, versioned derived scores.
- Existing Phase 5 voice behavior and the full earlier Playwright regression remain green.

## Database diagnosis and deployment order

The supplied `CustSearchAi.sql` database export contains versions V1.0.0 through V1.8.0 and the Phase 5–9 schema, but none of the V1.9.0 Phase 10 objects. Therefore error `52200` from `verify-phase10.sql` is expected when the verifier is run before the Phase 10 installer.

For that existing `CustSearch_AI` database, execute in this order:

1. `database/run-phase10.sql` against `CustSearch_AI`.
2. `database/verify-phase10.sql` against `CustSearch_AI`.

The installer is repeat-safe. The verifier intentionally performs no schema mutation and requires exactly one `V1.9.0` row. The canonical `database/CustSearchAi.sql` is separately validated for a fresh installation through V1.9.0.

## Release decision

All Phase 10 implementation, regression, security and database gates are green. The tested branch is eligible to merge into `AIMainBranch` after a final head-status verification; history must be preserved with a normal merge commit.
