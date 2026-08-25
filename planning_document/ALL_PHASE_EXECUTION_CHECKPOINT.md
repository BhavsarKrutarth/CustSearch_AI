# All-Phase Execution Checkpoint

## Run CUSTSEARCH_SMOKE_20260825_001

- Last updated: 2026-08-26 03:30:00 +05:30
- Branch: `audit/all-phases-database-smoke`
- Commit SHA at start: `3905775e3556259494688998cab3875a86c66dcf`
- Source baseline: `origin/phase16-operations`
- Current phase: Phase 17 quality/deployment audit and hardening
- Completed sub-phases: repository/remote/ancestry audit; PR metadata audit; SQL connectivity/version audit; Phase 16 analyzer repair; complete local regression; live V1.15 runner twice/verifier/constraints; isolated canonical validation; unavailable Redis/SQL readiness coverage
- Commands executed: repository audit commands; GitHub PR/check API queries; encrypted Windows-auth `sqlcmd`; Release restore/build/test; Angular lint/unit/production; Playwright Chromium; Ruff/pytest; Phase 16 SQL runner/verifier; canonical verifier
- Test counts: .NET unit 104/104; .NET integration 225/225; Angular 78/78; Playwright 49/49; Python 7/7
- Passed checks: source build; all local automated suites; live SQL runner/verifier/constraints; isolated canonical install; Redis disabled/unavailable behavior; SQL unavailable readiness; Phase 16 authorization, retention, leases, masking and audit coverage
- Failed checks: initial source-head CI analyzer failure and local file-lock failures were reproduced and fixed; current local failures: none
- Blocked checks: SQL Server 2022-specific validation (local engine is `17.0.1000.7`, compatibility 160 and Docker is unavailable); Redis multi-node/backplane environment validation
- Database version before: live database already contains `V1.16.0`; branch canonical is expected to stop at `V1.15.0`
- Database version after: unchanged (`V1.16.0` remains the latest live ledger row); canonical branch synchronized through `V1.15.0`
- Database records inserted: deterministic Phase 1–16 smoke graph for tenants `10019`/`10020`, stores `11`/`12`, users `10034`–`10037`, customers `5`/`6`, retail invoice `3`, camera `3`; no biometric template or Phase 18 incident
- Files changed: canonical SQL/build/verifier; readiness checks/tests; export stream sharing; disposable SQLite test configurations; Phase 16 report; this checkpoint
- Detected drift: live database contains a later Phase 18 foundation that is absent from the selected Phase 16 source branch; never downgrade or recreate the live database
- Next exact action: review the complete diff, rerun final SQL verifier/build gates, then commit and push the evidence-backed Phase 17 checkpoint
- Recommended next setup: keep PRs #10/#18/#19 open and unmerged; use the dedicated audit branch and close external SQL Server 2022/Redis topology blockers before declaring Phase 16 universally verified

## Verified repository facts

- `origin/master` is an ancestor of all later phase branches and currently points to Phase 7 level work.
- Phase chain is linear: `phase14-consent-recognition` → `phase15-reports-exports` → `phase16-operations`.
- PR #18 is draft/open and mergeable-clean. PR #19 is draft/open and unstable because validation failed. PR #10 is open/unstable.
- Phase 17 and Phase 18 remote branches do not exist at this checkpoint.

## Phase 17 observed progress

- Phase 16 repair checkpoint committed as `4fcb4c6`.
- Created the all-phase matrix and project/database/security/code traceability catalogs.
- Replaced stale Phase 2 README and removed workstation SQL connections from committed runtime settings.
- Added allowlisted optional forwarded headers and production HSTS; wildcard AllowedHosts removed.
- Added two-tenant smoke seed/verifier/cleanup; reruns and `DBCC CHECKCONSTRAINTS` passed.
- Live API invalid login, Tenant Admin login/`me`/refresh/logout, Platform Admin and Staff login passed.
- Fixed real-SQL refresh rotation to run inside the SQL retry execution strategy.
- Tenant A and Staff A received 404 for Tenant B customer; Tenant B received 200.
- IIS/WebSocket deployment, SQL Server 2022 and Redis multi-node tests remain environment-blocked.
- Post-change Release regression passed: 104 unit, 225 integration, 78 Angular and 49 Playwright tests; Ruff and 7 Python tests passed.
- Patched `@playwright/test` from 1.55.0 to 1.55.1 for GHSA-7mvr-c777-76hp; the npm audit is now clean and all 49 E2E tests still pass.
- NuGet vulnerable-package scan found no known direct or transitive vulnerability in all nine solution projects.
