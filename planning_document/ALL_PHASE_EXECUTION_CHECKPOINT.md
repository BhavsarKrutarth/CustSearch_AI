# All-Phase Execution Checkpoint

## Run CUSTSEARCH_SMOKE_20260825_001

- Last updated: 2026-08-26 09:00:00 +05:30
- Branch: `audit/all-phases-database-smoke`
- Commit SHA at start: `3905775e3556259494688998cab3875a86c66dcf`
- Source baseline: `origin/phase16-operations`
- Current phase: Phase 17 quality/deployment audit and hardening
- Completed sub-phases: repository/remote/ancestry audit; PR metadata audit; SQL connectivity/version audit; Phase 16 analyzer repair; complete local regression; live V1.15 runner twice/verifier/constraints; isolated canonical validation; unavailable Redis/SQL readiness coverage; actual Redis two-node SignalR backplane delivery
- Commands executed: repository audit commands; GitHub PR/check API queries; encrypted Windows-auth `sqlcmd`; Release restore/build/test; Angular lint/unit/production; Playwright Chromium; Ruff/pytest; Phase 16 SQL runner/verifier; canonical verifier
- Test counts: .NET unit 104/104; .NET integration 225/225; Angular 78/78; Playwright 49/49; Python 10/10
- Passed checks: source build; all local automated suites; live SQL runner/verifier/constraints; isolated canonical install; Redis disabled/unavailable behavior; Redis two-node backplane event delivery; SQL unavailable readiness; Phase 16 authorization, retention, leases, masking and audit coverage
- Failed checks: initial source-head CI analyzer failure and local file-lock failures were reproduced and fixed; current local failures: none
- Blocked checks: SQL Server 2022-specific validation (local engine is `17.0.1000.7`, compatibility 160, Docker is unavailable, and installing an isolated SQL 2022 instance requires administrator elevation)
- Database version before: live database already contains `V1.16.0`; branch canonical is expected to stop at `V1.15.0`
- Database version after: `V1.16.1`; corrective role upgrade applied twice and verified; canonical branch still ends at `V1.15.0` because the live Phase 18 V1.16 source remains on divergent ancestry
- Database records inserted: deterministic Phase 1–16 smoke graph for tenants `10019`/`10020`, stores `11`/`12`, users `10034`–`10037`, customers `5`/`6`, retail invoice `3`, camera `3`; no biometric template or Phase 18 incident
- Files changed: canonical SQL/build/verifier; readiness checks/tests; export stream sharing; disposable SQLite test configurations; Phase 16 report; this checkpoint
- Detected drift: live database contains a later Phase 18 foundation that is absent from the selected Phase 16 source branch; never downgrade or recreate the live database
- Phase 17 audit checkpoint: `fdc1e84d3e1150cbb44ff4660f215ad35c411d27`
- Phase 18 provenance correction: repeat-safe V1.16 SQL/verifier exist on divergent
  `origin/AIMainBranch` commit `055b052`; AIMain is 3 commits ahead while Phase16 is 15 commits ahead
  from merge base `b73704a`. No merge/cherry-pick was performed.
- Next exact action: set the authorized RTSP URL only in `CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP`, restart Python and require the authenticated dynamic probe to return `connected=true` and `frame_received=true`; then resume external SQL Server 2022/IIS gates
- Recommended next setup: keep PRs #10/#18/#19 open and unmerged; use the dedicated audit branch and close the exact SQL Server 2022 blocker before declaring Phase 16 universally verified

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
- Redis multi-node passed on 2026-08-26; IIS/WebSocket deployment and exact SQL Server 2022 remain environment-blocked.
- Post-change Release regression passed: 104 unit, 225 integration, 78 Angular and 49 Playwright tests; Ruff and 7 Python tests passed.
- Patched `@playwright/test` from 1.55.0 to 1.55.1 for GHSA-7mvr-c777-76hp; the npm audit is now clean and all 49 E2E tests still pass.
- NuGet vulnerable-package scan found no known direct or transitive vulnerability in all nine solution projects.

## Dynamic office-camera UAT addendum — 2026-08-26

- Live role-provisioning drift reproduced: CameraOperator rows existed with zero grants.
- Added/applied repeat-safe `V1.16.1` twice; both smoke tenants now have 13 tenant-only CameraOperator grants.
- Authenticated API created `office.camera.operator` in Tenant A/Store 11 and `random.no.camera` in Tenant B/Store 12.
- Tenant A sees camera 3; Tenant B sees zero cameras and receives 404 for camera 3 zones.
- Real headed Google Chrome passed Platform Admin, Office Camera Operator and Random No Camera User flows in independent contexts.
- Camera 3 stores only `env:CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP`; no LAN IP or RTSP credential is in source/SQL.
- Added authenticated Python dynamic one-frame probe. Ruff and pytest 10/10 passed; Python localhost health is Healthy.
- Physical frame is `BLOCKED`, not failed: authorized RTSP runtime secret is not configured. Continuous capture/detection/publishing remains pending.
- Phase 17 IIS/HTTPS/WebSocket execution plan now defines same-origin ARR routing, trusted TLS, WSS
  101, tenant isolation, reconnect/REST recovery, token-log review, evidence and rollback gates.
  Deployment remains blocked until a UAT host, certificate and service identities are provided.

## Authentication and user-flow addendum — 2026-08-26

- Live `KRUTARTH-BHAVSA/CustSearch_AI` account IDs, roles and store assignments were inventoried without
  reading or exposing password hashes.
- Added `POST /api/auth/change-password` and Angular `/account/change-password` for Platform/Tenant users.
- Added `PUT /api/tenant/users/{id}/password` and Tenant Users reset UI protected by `TenantUsers.Edit`,
  authoritative tenant/store visibility and a self-reset denial.
- Both flows use `IPasswordHasher<UserAccount>`, rotate `SecurityStamp`, revoke every refresh session and
  keep password material out of authentication/business audit payloads.
- Pinned SDK `8.0.424` isolated Release build: 0 warnings/errors. Unit 104/104 and Integration/API 230/230 PASS.
- Angular lint PASS, 82/82 PASS, production build PASS (existing style-budget warning: 151 bytes).
- No live account password was changed during automated validation; integration tests use isolated SQLite.
- Runtime note: the manually running API process must be restarted before the new endpoints appear in Swagger/UI.
