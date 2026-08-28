# Camera Motion, Tenant Storage — Work Progress / Handover

This is the single execution record for `Camera_Motion_CustSearch_AI_Tenant_Storage_Planning.md`. A successor AI must read the requirement file and this file before changing code, must not repeat completed phases, and must continue from **Current continuation point**.

## Current continuation point

- **Overall status:** In progress
- **Active phase:** Phase D — Optional Zones
- **Next action:** Add camera-level optional detection-zone mode, OFF/full-frame and ON/active-zone validation, expanded zone catalog, API/UI controls and tests.
- **Blocking issues:** None
- **Working branch:** `camera-motion-tenant-storage`
- **Baseline commit:** `31a78ada3344d604e16ecd6808eb81da5d6ee598`
- **Baseline branch:** `phase18-retail-security`
- **Baseline worktree:** Clean at start
- **Last updated:** 2026-08-28 (Asia/Calcutta)

## Phase status

| Phase | Scope | Status | Git commit |
|---|---|---|---|
| A | Camera quota enforcement | **Completed** | `03983d6` |
| B | Common live monitoring | **Completed** | `f5ca24d` |
| C | Motion rule engine | **Completed** | `e70c1ed` |
| D | Optional zones | In progress | Pending |
| E | Evidence storage | Not started | — |
| F | 15-day retention worker | Not started | — |
| G | Retail security rules | Not started | — |
| Final | Full regression and handover reconciliation | Not started | — |

## Baseline architecture already available

- ASP.NET Core API, Application, Domain, Infrastructure, SQL Server upgrade scripts, Angular Admin, Python CCTV service, Worker and automated test projects already exist.
- Phase 13 already provides tenant/store-scoped cameras, versioned zones, person tracking, signed CCTV ingestion and secure server-mediated camera previews.
- Platform tenancy and billing already expose `Tenant.MaxCameras`; plan/override assignment materializes the effective camera limit into this field.
- Phase 18 already provides retail-security observations, incidents, evidence metadata, review flow and SignalR publication. Phase G must extend/reuse it, not create a parallel incident system.
- The browser camera API deliberately has no `TenantId` input. Preserve this server-derived tenant boundary in every phase.

## Global decisions and invariants

1. `Tenant.MaxCameras` is the runtime effective limit because existing subscription/override workflows materialize their result there.
2. Camera quota is tenant-wide. An active camera (`IsActive = true`) consumes one slot even when offline. The current schema has no camera `IsDeleted`; inactive cameras do not consume a slot.
3. UI quota controls are UX only; API/service enforcement is authoritative, including inactive-to-active reactivation.
4. RTSP credentials, physical storage paths and client-supplied `TenantId` must never reach the browser or be trusted from external requests.
5. Storage is event evidence (snapshots and short clips), not continuous recording.
6. Each completed phase must add its development/database/API/UI/tests/fixes/decisions/pending record below and receive a focused git commit.
7. Do not rewrite or remove unrelated user changes. Before each commit, inspect `git diff` and commit only the active phase plus this handover update.

## Phase A — Camera Quota Enforcement

### Development / database / API / UI / tests / fixes

- **Application/API:** Added `CameraQuotaView`, `ICameraTrackingService.GetCameraQuotaAsync` and `GET /api/tenant/cameras/quota` under `Cameras.View` authorization. The response is derived from the authenticated tenant and contains maximum, configured, active and available counts plus `canAddActiveCamera`.
- **Enforcement:** `SaveCameraAsync` now checks capacity for a new active camera and inactive-to-active transition. New inactive cameras and active-to-inactive edits remain valid. A full quota returns HTTP 409 with `Camera limit reached. Your current plan supports maximum N active cameras.`
- **Concurrency:** Camera writes run in a serializable transaction. SQL Server acquires an exclusive transaction-owned `sp_getapplock` keyed by immutable TenantId before capacity check/write, preventing concurrent requests from both consuming the last slot.
- **Database:** Added repeat-safe `database/09_Upgrade/V1.18.0_CameraQuotaEnforcement.sql` with prerequisite checks, tenant/active count index and `DatabaseVersions` registration. EF model also declares `(TenantId, IsActive)` index.
- **UI:** Camera Operations now loads the server quota, displays active/max, total configured and available slots, blocks new/reactivated active saves when full, disables the New Active Camera action and surfaces server 409 text. The browser still sends no TenantId.
- **Tests:** Added tenant-wide quota visibility for store-scoped users; five-active success, sixth denial, inactive camera allowance, reactivation denial and disabled-slot reuse; API permission contract; Angular tenant-relative quota request test.
- **Fixes during validation:** The normal API output directory was locked by the user's running `CustSearch.API`/Visual Studio process. Tests were rerun successfully with `--artifacts-path`; the running process was deliberately left untouched.

### Decisions

- See global decisions 1–3.
- Quota summary is tenant-wide even when a user can list cameras only in assigned stores, because licensing is a tenant resource. It exposes aggregate counts only, not unauthorized camera details.
- HTTP 409 represents capacity conflict. Offline still consumes a slot because `IsActive`, not health status, is the licensing state.
- SQL Server application locks provide the production concurrency boundary; the SQLite automated tests validate behavior but not `sp_getapplock` execution.

### Pending issues

- Deployment must apply `V1.18.0_CameraQuotaEnforcement.sql` to the target SQL Server. It was not applied automatically from this source-development session.
- Manual two-request SQL Server concurrency verification is recommended in deployment/UAT; automated SQLite tests cannot execute SQL Server application locks.

## Phase B — Common Live Monitoring

- **Status:** Completed.

### Development / database / API / UI / tests / fixes

- **UI:** Added lazy route `/customer-admin/live-monitoring`, navigation entry and `LiveCameraMonitoringPage`. It renders at most five active, server-visible cameras in a responsive 3+2 grid, shows tenant quota/online/offline/live summaries, auto-starts secure sessions, filters All/Online/Offline and supports per-tile full screen, retry and stop.
- **Failure isolation:** Each tile owns its polling subscription, short-lived preview session and browser object URL. One denied/offline/unavailable camera becomes an unavailable tile without stopping other streams.
- **Cleanup:** Filtering, refresh and component destruction unsubscribe frame polling, revoke object URLs and call the audited session-end endpoint. A start response arriving after a filter change is immediately ended.
- **Security:** Route requires both `Cameras.View` and `Cameras.Preview`; per-camera preview grants and authoritative tenant/store checks remain enforced by the existing backend. No RTSP value or camera credential is added to the browser contract.
- **Backend resource boundary:** Added validated `CctvPreview:MaximumConcurrentSessionsPerUser` (default/hard maximum 5). Session starts run in a serializable transaction and use a SQL Server transaction-owned application lock per tenant/user, so concurrent starts cannot exceed five active, unexpired sessions.
- **Database:** No schema change was needed. Existing `CameraPreviewSessions` supports the cap and audited lifecycle.
- **Tests:** Backend integration verifies five session success and sixth conflict. Playwright fixtures now cover five tiles, live frames, online/offline filters, permission denial and credential non-disclosure. Existing camera operations E2E was reconciled with the Phase A quota endpoint.

### Decisions

- The common grid is intentionally capped at five even if a future tenant plan allows more active cameras; it takes the first five active cameras from the already server-authorized/sorted camera list.
- Preview sessions stay independent per browser request/tab. They are not shared across tabs; the five-session cap is a resource/security boundary and avoids one tab ending another tab's token.
- `CameraStatus.Online` drives the Online filter; Offline, Degraded and Maintenance appear in the Offline/non-online filter until a richer health grouping is requested.
- Motion Active and Recent Motion filters remain Phase C outputs; Phase B does not fabricate motion state before the rule/event model exists.

### Pending issues

- Physical-camera visual/UAT still requires `CctvPreview.Enabled=true`, a valid server-side API key and real grants. Automated tests use deterministic JPEG fixtures.
- The existing `admin-shell.scss` production budget warning remains unrelated and unchanged.

## Phase C — Motion Rule Engine

- **Status:** Completed.

### Development / database / API / UI / tests / fixes

- Added `Camera.MotionRulesEnabled` master switch and full `CameraMotionRule` domain/configuration model: confidence, sensitivity, minimum duration, cooldown, optional UTC schedule window, days, snapshot/clip settings, pre/post seconds, severity, alert/realtime flags and optional/required zone.
- Added stable 26-code grouped catalog. Initial available MVP rules are `PERSON_DETECTED`, `ENTRY_CROSSED`, `EXIT_CROSSED`, `DWELL_THRESHOLD`, `RESTRICTED_ZONE_ENTRY` and `CAMERA_OFFLINE`; planned/advanced codes are visible but rejected server-side until their phase is implemented.
- Added authenticated catalog/settings/list/create/update APIs. TenantId is server-derived; camera and any selected active zone must match Tenant + Store + Camera scope. Rule code is immutable and one code per camera is enforced.
- Added `Cameras.ManageRules` backend/UI permission and least-privilege upgrade grants to tenant ownership/admin roles.
- Added grouped Angular rule editor reachable from Camera Operations, with master switch, planned-vs-available catalog, server-owned defaults/validation, zone selection and complete per-rule fields.
- Added repeat-safe SQL `V1.18.1_CameraMotionRules.sql`, camera master column, rule table/constraints/indexes/FKs, permission/grants and version ledger.
- Fixed validation rollback: a newly tracked rule that fails domain validation is detached, so a corrected retry in the same unit of work does not create a false duplicate conflict.

### Decisions

- Rule codes are stable strings instead of a closed database enum, allowing phased catalog expansion without data remapping.
- Entry/Exit/Restricted rules inherently require an active semantic zone. Person, Dwell and Camera Offline allow full-frame operation. Phase D adds the separate camera-level optional detection-area switch.
- This phase is the authoritative rule configuration engine. Actual computer-vision inference remains in the existing signed Python CCTV boundary; advanced detectors are not simulated by the .NET/UI layer.

### Pending issues

- Apply `V1.18.1_CameraMotionRules.sql` to target SQL Server after V1.18.0.
- Production Python deployments must consume/evaluate only the available, enabled server configuration; no Python direct database access was introduced.

## Phase D — Optional Zones

- Not started. Read Phase D in the requirement after Phase C is committed.

## Phase E — Evidence Storage

- Not started. Read Phase E in the requirement after Phase D is committed.

## Phase F — 15-Day Retention Worker

- Not started. Read Phase F in the requirement after Phase E is committed.

## Phase G — Retail Security Rules

- Not started. Read Phase G in the requirement after Phase F is committed.

## Validation ledger

| When | Scope | Command / test | Result |
|---|---|---|---|
| 2026-08-28 | Baseline | `git status --short --branch` | Clean on `phase18-retail-security` |
| 2026-08-28 | Phase A targeted .NET | `dotnet test ... --filter PhaseThirteen... --artifacts-path artifacts/phase-a-dotnet` | **PASS — 17/17** |
| 2026-08-28 | Phase A full .NET | `dotnet test CustSearch_AI.sln --artifacts-path artifacts/phase-a-full` | **PASS — Unit 118/118, Integration 237/237 (355 total)** |
| 2026-08-28 | Phase A Angular API | `npm test -- --watch=false --include=src/app/features/cameras/cameras-api.service.spec.ts` | **PASS — 6/6** |
| 2026-08-28 | Phase A Angular build | `npm run build` | **PASS — existing admin-shell.scss budget warning only** |
| 2026-08-28 | Phase A diff | `git diff --check` | **PASS — line-ending notices only** |
| 2026-08-28 | Phase B targeted .NET | `dotnet test ... --filter CameraPreviewAuthorizationTests --artifacts-path artifacts/phase-b-dotnet-rerun` | **PASS — 4/4** |
| 2026-08-28 | Phase B full .NET | `dotnet test CustSearch_AI.sln --artifacts-path artifacts/phase-b-full` | **PASS — Unit 118/118, Integration 238/238 (356 total)** |
| 2026-08-28 | Phase B Angular full | `npm test -- --watch=false` | **PASS — 88/88** |
| 2026-08-28 | Phase B Angular build | `npm run build` | **PASS — existing admin-shell.scss budget warning only** |
| 2026-08-28 | Phase B Playwright | `npx playwright test tests/phase13-cameras-tracking.spec.ts` | **PASS — Chromium 5/5** |
| 2026-08-28 | Phase B diff | `git diff --check` | **PASS — line-ending notices only** |
| 2026-08-28 | Phase C targeted .NET | `dotnet test ... --filter CameraMotionRuleServiceTests|PhaseThirteenApiContractTests --artifacts-path artifacts/phase-c-targeted2` | **PASS — 20/20** |
| 2026-08-28 | Phase C full .NET | `dotnet test CustSearch_AI.sln --artifacts-path artifacts/phase-c-full` | **PASS — Unit 118/118, Integration 247/247 (365 total)** |
| 2026-08-28 | Phase C Angular full | `npm test -- --watch=false` | **PASS — 89/89** |
| 2026-08-28 | Phase C Angular build | `npm run build` | **PASS — existing admin-shell.scss budget warning only** |
| 2026-08-28 | Phase C diff | `git diff --check` | **PASS — line-ending notices only** |

## Git ledger

| Commit | Phase | Summary |
|---|---|---|
| `31a78ada3344d604e16ecd6808eb81da5d6ee598` | Baseline | Starting point supplied by user |
| `03983d6` | Phase A | Tenant-wide active camera quota API, enforcement, SQL upgrade, UI and tests |
| `91c6643` | Phase A checkpoint | Single handover file initialized and Phase B continuation point recorded |
| `f5ca24d` | Phase B | Secure five-camera monitoring grid, session cap, cleanup and tests |
| `dac0bd8` | Phase B checkpoint | Handover advanced to Phase C |
| `e70c1ed` | Phase C | Motion rule catalog, domain, SQL, APIs, permissions, Angular editor and tests |

## Known pending work

- Complete Phases D–G in order. Phases A–C must not be repeated.
- Run the full .NET, Angular, Python and E2E regression suites after phase-specific tests are stable.
- Apply SQL upgrade scripts to a real SQL Server test database when one is available; automated SQLite/model tests do not replace SQL Server deployment verification.
