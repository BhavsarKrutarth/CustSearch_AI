# Phase 7 Test Report — Households, Visits & Co-Visit Parties

**Project:** CustSearch AI  
**Repository:** `BhavsarKrutarth/CustSearch_AI`  
**Branch tested:** `phase7-households-visits`  
**Pull request:** #7 — Phase 7 — Households, Visits & Co-Visit Parties  
**Validation date:** 2026-08-23  
**Validated implementation commit:** `5cfae2ae38210906f849cea1bbd27508917dc675`  
**Canonical persistence commit:** `b633e2b02639ac3ad51843ea9704bd525b8e007e`  
**Phase 7 validation run:** GitHub Actions run `32645998288`  
**Phase 6 regression run:** GitHub Actions run `32645998282`  

## Final Result

**PASS — Phase 7 technical validation is green.**

The final Phase 7 validation run completed successfully across .NET, Angular, Playwright, Python, SQL structure validation, real SQL Server 2022 repeat-safety validation, the standalone Phase 7 SQL runner, tenant/store/privacy checks, and a fresh canonical database install.

The Phase 6 regression workflow also completed successfully on the same validated parent commit, confirming that Phase 7 did not break the existing Phase 5/6 validation baseline.

## Validation Matrix

| Area | Validation | Result |
|---|---|---|
| Canonical preparation | Portable canonical generated through V1.6.0 | PASS |
| .NET restore | `dotnet restore CustSearch_AI.sln` | PASS |
| .NET build | Release build | PASS — 0 warnings, 0 errors |
| .NET unit tests | `CustSearch.UnitTests` | PASS — 46/46 |
| .NET integration tests | `CustSearch.IntegrationTests` | PASS — 79/79 |
| Angular dependencies | Reproducible `npm ci` | PASS |
| Angular versions | Angular 21.2.20 / CLI 21.2.21 / Material+CDK 21.2.14 / TypeScript 5.9.3 | PASS |
| Angular lint | `npm run lint` | PASS |
| Angular tests | 22 test files | PASS — 47/47 |
| Angular production build | `npm run build:production` | PASS |
| Playwright | Full Phase 5/6/7 regression suite | PASS — 20/20 |
| Python lint | Ruff | PASS |
| Python tests | pytest | PASS — 3/3 |
| Phase 7 SQL structure | Tables/SPs/constraints/permissions/privacy scan | PASS |
| SQL Server 2022 upgrade | V1.6.0 upgrade applied twice | PASS |
| Standalone database runner | `database/run-phase7.sql` applied twice | PASS |
| Version ledger | Exactly one `V1.6.0` database-version row after repeat execution | PASS |
| Household search scope | `@TenantId` + authorized-store scope required | PASS |
| Visit Party privacy | VisitParty search/detail contain no `dbo.Households` or `dbo.HouseholdMembers` dependency | PASS |
| Canonical fresh install | Fresh SQL Server 2022 install with V1.4.0 + V1.5.0 + V1.6.0 | PASS |
| Phase 6 regression | Existing Phase 6 full validation workflow | PASS |

## Verified Test Counts

### .NET

- Unit tests: **46 passed, 0 failed, 0 skipped**
- Integration tests: **79 passed, 0 failed, 0 skipped**
- Build: **0 warnings, 0 errors**

### Angular

- Test files: **22 passed**
- Tests: **47 passed**
- Lint: **PASS**
- Production build: **PASS**

Validated frontend toolchain:

- Angular CLI: **21.2.21**
- Angular: **21.2.20**
- Angular Material: **21.2.14**
- Angular CDK: **21.2.14**
- TypeScript: **5.9.3**
- Node.js in CI: **24.15.0**
- npm project version: **11.9.0**

### Playwright E2E

**20/20 passed.**

The full E2E suite covers Phase 5, Phase 6 and Phase 7 together. Phase 7-specific scenarios include:

1. Household search/create without browser-supplied `TenantId`.
2. Explicit verified household-member linking/removal without anonymous-visitor identity being treated as family identity.
3. Factual visit history and explicit **Visit Party / Co-Visit** labeling.
4. Household permission denial when `Households.View` is absent.
5. Store-scoped Phase 7 pages using server-authorized results without requesting another tenant.

Phase 6 regression scenarios also remained green, including customer search/create, smart customer profile, visitor conversion, permission guard and store-scoped customer visibility.

### Python

- Ruff: **PASS**
- pytest: **3/3 passed**

## Database Validation

Phase 7 database version: **V1.6.0**.

Validated SQL objects include:

- `dbo.Households`
- `dbo.HouseholdMembers`
- `dbo.VisitParties`
- `dbo.VisitPartyMembers`
- `dbo.CustomerVisits`
- `dbo.Household_Search`
- `dbo.Household_GetDetail`
- `dbo.CustomerVisit_Search`
- `dbo.VisitParty_Search`
- `dbo.VisitParty_GetDetail`

Important constraints and security rules verified:

- Household member relationships use explicit verified relationship sources.
- `CK_HouseholdMembers_RelationshipSource` limits relationship sources to the approved explicit source range.
- `CK_VisitPartyMembers_IdentityXor` requires exactly one party-member identity source.
- Tenant-safe composite foreign keys are present for household/customer and visit-party/store relationships.
- `Household_Search` exposes the authorized-store CSV predicate required by store-scoped access.
- `VisitParty_Search` and `VisitParty_GetDetail` do not reference `dbo.Households` or `dbo.HouseholdMembers`.
- Co-visit evidence therefore remains separate from verified household/family truth at the database-query boundary.
- The Phase 7 executable SQL contains no forbidden inferred-family relationship source.

## SQL Server 2022 Repeat-Safety Evidence

The Phase 7 workflow started an actual `mcr.microsoft.com/mssql/server:2022-latest` container and performed both upgrade paths.

### Versioned upgrade path

1. Install the completed Phase 6 canonical baseline.
2. Apply `V1.6.0_Phase7_HouseholdsVisits.sql`.
3. Apply the same V1.6.0 upgrade again.
4. Confirm exactly one `V1.6.0` database-version row.
5. Confirm required Phase 7 objects and security predicates exist.

**Result: PASS.**

### Standalone runner path

1. Recreate the completed Phase 6 baseline.
2. Execute `database/run-phase7.sql`.
3. Execute `database/run-phase7.sql` again.

**Result: PASS.**

This confirms the user-facing Phase 7 database runner is standalone T-SQL and repeat-safe in the SQL Server 2022 CI environment.

## Canonical Database

The successful workflow generated and validated the complete canonical database through V1.6.0. The push workflow then persisted the generated canonical SQL and E2E lock in commit:

`b633e2b02639ac3ad51843ea9704bd525b8e007e`

The committed `database/CustSearchAi.sql` now contains the Phase 7 V1.6.0 block after the completed Phase 6 V1.5.0 block.

Fresh-install verification on SQL Server 2022 confirmed:

- exactly one V1.4.0 row,
- exactly one V1.5.0 row,
- exactly one V1.6.0 row,
- Phase 7 tables exist,
- Phase 7 stored procedures exist.

**Result: PASS.**

## Tenant / Store / Privacy Validation

Phase 7 preserves the established server-authoritative tenancy model:

- Browser request models do not expose a writable `TenantId`.
- Tenant identity comes from the authenticated server context.
- Store-scoped users remain limited to their authoritative `StoreIds`.
- Household visibility for store-scoped users derives from visible customer/store assignments.
- Customer visits are store-bound and tenant-bound.
- Visit Parties are store-bound co-visit evidence.
- A Visit Party is **not** a Household and is not used as automatic evidence of a family relationship.
- Anonymous visitors are not automatically promoted into household membership.
- Household relationships require explicit customer records and verified relationship metadata.

## Issues Found During Testing and Fixes Applied

Testing found real regressions and CI validation defects; none were bypassed by weakening production security assertions.

### 1. .NET analyzer failures

The first Phase 7 build detected two `CA1512` analyzer errors for explicit `ArgumentOutOfRangeException` usage.

**Fix:** converted the date-range guards to the .NET 8 `ArgumentOutOfRangeException.ThrowIfLessThan` form.

### 2. Phase 6 Playwright navigation ambiguity after Phase 7 navigation was added

The Phase 7 shell introduced additional navigation links, making old broad Phase 6 selectors ambiguous.

**Fix:** Phase 6 E2E navigation now scopes link selection to the Customer Admin dashboard content navigation. Business assertions remained unchanged.

### 3. Phase 7 deep-link session bootstrap failure

Deep-link/reload E2E scenarios triggered the real Angular `/api/auth/me` session bootstrap. The Phase 7 mock originally covered login/refresh but not `/me`, causing the test server to attempt a real backend proxy connection.

**Fix:** added an authoritative `/api/auth/me` mock using the production response shape `{ user, accessTokenExpiresUtc }`. Deep-link and permission assertions were retained.

### 4. SQL inferred-family safety scanner false positive

The SQL file intentionally contained a documentation comment explaining that an inferred-family source does not exist. The initial scanner searched raw SQL text and therefore flagged the comment itself.

**Fix:** the structural safety scan strips SQL comments before checking executable SQL for the forbidden source name.

### 5. SQL Server Visit Party household-independence assertion false positive

`OBJECT_DEFINITION(dbo.VisitParty_Search)` includes procedure comments. The original validation searched for the generic text `Households`, which matched the procedure comment stating that Visit Party queries never infer Households.

**Fix:** SQL Server validation now checks the actual database object names `dbo.Households` and `dbo.HouseholdMembers` in both `VisitParty_Search` and `VisitParty_GetDetail`. The procedures themselves contain no such dependency, and the corrected real SQL Server 2022 gate passed.

## Regression Result

The Phase 6 validation workflow on the same validated parent commit completed successfully, including:

- .NET restore/build/tests,
- Angular lint/tests/build,
- Phase 5/6 Playwright regression,
- Python baseline,
- Phase 6 SQL structure,
- Phase 6 upgrade twice on SQL Server 2022,
- tenant/store predicates,
- canonical fresh install.

**Result: PASS.**

## Scope Limitation

The SQL database validation in this report was executed against a real **SQL Server 2022 Docker container in GitHub Actions**.

The private user-local SQL Server instance `KRUTARTH-BHAVSA` is not reachable from the GitHub/ChatGPT execution environment and therefore was **not** executed as part of this report. Local-machine acceptance should use the committed Phase 7 SQL runner against that instance if local-instance-specific confirmation is required.

This limitation does not change the CI result: the versioned upgrade, standalone runner, repeat safety and canonical fresh install all passed against SQL Server 2022.

## Final Verdict

**Phase 7 validation status: PASS / GREEN.**

Validated technical gates:

- .NET: GREEN
- Unit: 46/46 GREEN
- Integration: 79/79 GREEN
- Angular lint: GREEN
- Angular tests: 47/47 GREEN
- Angular production build: GREEN
- Playwright: 20/20 GREEN
- Python Ruff: GREEN
- Python pytest: 3/3 GREEN
- SQL structure: GREEN
- SQL Server 2022 V1.6 upgrade twice: GREEN
- standalone `run-phase7.sql` twice: GREEN
- tenant/store/privacy database assertions: GREEN
- final canonical fresh install: GREEN
- Phase 6 regression: GREEN
- canonical V1.6 persistence: COMPLETE

The Phase 7 implementation is technically validated for the tested CI scope and is ready for the next repository workflow decision (review/merge or local-instance acceptance), without claiming execution against the private local SQL Server machine.
