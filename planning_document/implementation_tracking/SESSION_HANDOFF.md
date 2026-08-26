# Session Handoff

- Last Updated: 2026-08-26 11:02 +05:30
- Current Branch: `audit/all-phases-database-smoke`
- Current Commit Before This Checkpoint: `6084d75`
- Current Phase: Phase 17 local hardening complete; IIS/HTTPS/WebSocket deployment gate remains
- Current Task: restart the manually running API, then browser-smoke the new password UI without exposing a credential
- Last Completed Phase: Phase 15 universally; Phase 16 locally passed with SQL Server 2022 certification pending

## Completed This Session

1. Verified live `KRUTARTH-BHAVSA/CustSearch_AI` admin/user IDs, scopes, tenant codes, roles and store assignments without selecting password hashes.
2. Created `docs/ADMIN_USER_DATABASE_LOGIN_AND_PASSWORD_FLOW.md` with table relationships, Platform/Tenant login flows, safe diagnostics and password rotation guidance.
3. Added authenticated self-service `POST /api/auth/change-password` for both Platform and Tenant identities.
4. Added permission-protected `PUT /api/tenant/users/{id}/password` for another visible/in-scope tenant user.
5. Added Angular `/account/change-password`, desktop/mobile account-security links, confirmation/policy validation and post-change sign-in message.
6. Added Tenant Users **Reset password** UI, hidden without `TenantUsers.Edit` and hidden for the current user's own row.
7. Added unit/integration/API/Angular regression tests and updated persistent tracking.

## Database Changes

1. None. Existing `dbo.Users`, `dbo.RefreshTokens`, `dbo.AuthenticationEvents` and `dbo.AuditLogs` schema supports this flow.
2. No live password, hash, refresh token or UAT record was changed during automated testing.

## Code Changes

1. Authentication application/service/controller contracts now verify current password, hash the new password, rotate `SecurityStamp`, revoke all refresh sessions and audit safely.
2. Tenant operations service/controller now performs authorized in-scope admin reset with self-reset denial and secret-free business audit.
3. Angular auth/account and Tenant Users screens now expose the two supported UI flows.
4. Auth support documentation records current non-secret live identities and explains why hashes cannot be decoded.

## Tests Passed

1. Pinned .NET SDK `8.0.424` isolated Release build: 0 warnings, 0 errors.
2. .NET unit: 104/104.
3. .NET integration/API: 230/230, including old-password/session rejection, new-password login, audit redaction and cross-store reset denial.
4. Angular lint: PASS.
5. Angular unit: 82/82.
6. Angular production build: PASS (non-blocking existing admin-shell style budget warning: 151 bytes).

## Tests Failed

1. First build used PATH SDK 10 because the pinned SDK is installed under the user-local dotnet path; rerun with exact SDK `8.0.424` passed.
2. Normal Release output was locked by the user's manually running API process PID 7560; isolated `OutDir` build passed without stopping that process.
3. First new Angular page test selected the AdminShell search input as a password field; selector was scoped to `.password-card` and full retest passed 82/82.

## Known Blockers

1. The currently running API process was started before these source changes and must be restarted before Swagger/UI can use the new endpoints.
2. Production forgot-password/first-Tenant-Admin onboarding still needs recipient-owned, short-lived, single-use invitation/reset tokens and verified notification delivery.
3. Authorized RTSP runtime credential, exact SQL Server 2022 certification and IIS/HTTPS/WebSocket deployment evidence remain pending.
4. Phase 18 source/application integration remains unresolved.

## Files Currently Being Worked On

1. `docs/ADMIN_USER_DATABASE_LOGIN_AND_PASSWORD_FLOW.md`
2. `src/CustSearch.Infrastructure/Security/AuthenticationService.cs`
3. `src/CustSearch.Infrastructure/TenantOperations/TenantOperationsService.cs`
4. `src/CustSearch.Admin/src/app/features/auth/change-password-page.ts`
5. `src/CustSearch.Admin/src/app/features/customer-admin/phase-five-management-page.ts`
6. authentication and Phase 5 regression tests plus tracking files

## Exact Next Step

1. Stop the old API with Ctrl+C in its terminal, restart it from this branch, refresh Angular, then confirm `/account/change-password` appears and Swagger lists both new password endpoints. Use a disposable/local test identity for any password-changing browser smoke.
2. After that local smoke, resume Gate A in `planning_document/PHASE_17_IIS_HTTPS_WEBSOCKET_DEPLOYMENT_TEST_PLAN.md`.

## Commands To Run Next

1. `$env:ConnectionStrings__CustSearchDatabase="Server=KRUTARTH-BHAVSA;Database=CustSearch_AI;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"`
2. `& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" run --project .\src\CustSearch.API\CustSearch.API.csproj --configuration Release`
3. Open `http://localhost:4200/account/change-password` after login and `https://localhost:7277/swagger` (use the actual configured ports printed by each process).

## Important Context For Next AI Session

- The existing manual API process was deliberately not stopped because it belongs to the user's interactive run.
- Password hashes are one-way and must never be decoded, displayed, copied between users or committed.
- Admin reset is only for another visible/in-scope tenant user; self-service change password verifies the old credential.
- Successful password change/reset invalidates every existing JWT on its next server validation and revokes every refresh token.
- The local smoke runner can rotate only deterministic `smoke.*` accounts using a runtime environment password; never record that password in Git.
- Preserve camera secret rules: no LAN IP, RTSP username/password or resolved URL in source, SQL, logs or screenshots.
