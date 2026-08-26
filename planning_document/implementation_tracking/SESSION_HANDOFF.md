# Session Handoff

- Last Updated: 2026-08-26 09:00 +05:30
- Current Branch: `audit/all-phases-database-smoke`
- Current Commit Before This Checkpoint: `ffef36f`
- Current Phase: Phase 17 local UAT / Phase 13 physical-camera boundary
- Current Task: obtain the authorized RTSP stream credential through local secret configuration and execute the dynamic one-frame probe
- Last Completed Phase: Phase 15 universally; Phase 16 locally passed with SQL Server 2022 certification pending

## Completed This Session

1. Fixed the SQL Server `tinyint` enum mappings that caused the live Camera API to throw `InvalidCastException`.
2. Detected and repaired live `Tenant_ProvisionDefaultRoles` drift; repeat-safe V1.16.1 ran twice.
3. Created Office Camera Operator (Tenant A/Store 11) and Random No Camera User (Tenant B/Store 12) through authenticated tenant APIs.
4. Registered the office camera dynamically with an opaque `env:` reference; no IP/user/password was stored in source or SQL.
5. Proved own-tenant camera count 1, isolation-tenant camera count 0, and direct cross-tenant camera access 404.
6. Ran real Google Chrome in three isolated contexts: Platform Admin, Office Camera Operator and Random No Camera User.
7. Added an authenticated Python dynamic RTSP one-frame probe with allow-listed environment resolution and secret-safe output.

## Database Changes

1. Added one `V1.16.1` ledger row and backfilled complete CameraOperator tenant-only grants for all existing tenants.
2. Created test users 10038/10039 with CameraOperator roles and authoritative store assignments.
3. Updated camera 3 to use `env:CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP`; status remains Offline until a valid server runtime secret/frame probe succeeds.

## Code Changes

1. Added dynamic Python camera source resolver/probe and three tests.
2. Corrected future role provisioning, smoke role materialization and CameraOperator dashboard access.
3. Added SQL Server enum-to-`tinyint` mappings and environment-driven headed-Chrome UAT automation.
4. Updated camera/Python/manual-run documentation; LAN IP and credentials are not hard-coded.

## Tests Passed

1. SQL V1.16.1 execution twice plus tenant-only permission verifier.
2. .NET Release build: 0 warnings/errors; Integration/API: 225/225.
3. Python Ruff PASS and pytest 10/10.
4. Authenticated API camera isolation and headed Chrome three-context UAT PASS.
5. API readiness 200; Python health Healthy.

## Tests Failed

1. First headed-Chrome run stopped on a duplicate-heading strict locator; selector fixed and retest PASS.
2. First .NET invocation selected machine-wide SDK 10 instead of pinned 8.0.424; explicit local .NET 8 path fixed it.
3. First clean build was blocked by the running Release API DLL lock; exact workspace process was restarted and clean build passed.

## Known Blockers

1. Authorized RTSP username/password/stream path is not configured in `CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP`; physical frame capture is therefore not claimed.
2. Continuous RTSP reconnect/detection/HMAC publishing remains pending; current dynamic probe reads at most one frame.
3. Exact SQL Server 2022 and IIS deployment environments remain unavailable.
4. Phase 18 source/application integration remains unresolved.

## Files Currently Being Worked On

1. `src/CustSearch.AI/app/camera_source.py`
2. `database/09_Upgrade/V1.16.1_CameraOperatorRoleGrants.sql`
3. camera/manual-run guides and implementation tracking files

## Exact Next Step

1. In the Python server terminal, set `CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP` to the authorized RTSP URL, restart Uvicorn, and call `POST /v1/cctv/cameras/probe`; require `connected=true` and `frame_received=true` without exposing the URL.

## Commands To Run Next

1. `$env:CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP="rtsp://<authorized-user>:<password>@<camera-ip>:554/<stream-path>"`
2. Follow the authenticated probe command in `docs/CAMERA_CONNECTION_AND_RTSP_GUIDE.md`.

## Important Context For Next AI Session

- API is listening on localhost 7277, Angular on 4200 and Python on 8000 at handoff time.
- Test camera is database configuration, not hard-coded application behavior. Camera access is derived from JWT tenant and store assignments.
- Test screenshots are under ignored `tests/CustSearch.Admin.E2E/artifacts/manual-server-camera-access/`.
- Preserve user-owned `src/CustSearch.Admin/angular.json` and `docs/environmentSetup.md` changes.
- Never guess/bypass camera credentials or log the resolved RTSP URL.
