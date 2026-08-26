# Phase Status

Detailed per-requirement status is maintained in `../ALL_PHASE_IMPLEMENTATION_MATRIX.md`.

## Phases 1-15

- Status: COMPLETE
- Completed: database, backend, required frontend, security and automated validation are evidenced by
  phase reports and the current full regression.
- Pending: deployment-specific production controls remain operational prerequisites, not phase code gaps.

## Phase 16

- Planning file: `../phase_implementation/PHASE_16_OPERATIONS.md`
- Status: BLOCKED
- Completed: schema V1.15, API/Admin/Worker operations, leases, health, retention, masking, local tests,
  and actual two-API-node Redis backplane delivery.
- Pending: exact SQL Server 2022 execution.
- Next exact task: execute the documented external environment gates.

## Phase 17

- Planning file: `../phase_implementation/PHASE_17_QUALITY_DEPLOYMENT.md`
- Status: IN PROGRESS
- Completed: setup/catalog/runbook docs, security configuration hardening, comments, smoke automation,
  full local regression and dependency audits.
- Pending: deployed IIS/HTTPS/WebSocket smoke and external environment gates.
- Files changed: see `CODE_CHANGE_LOG.md` and Git diff.
- Next exact task: provision/identify the IIS UAT host, FQDN, trusted certificate and approved service
  identities; execute Gate A of `../PHASE_17_IIS_HTTPS_WEBSOCKET_DEPLOYMENT_TEST_PLAN.md`.

## Phase 13/17 Local Camera UAT Addendum

- Status: IMPLEMENTED — PHYSICAL FRAME TESTING PENDING
- Completed: dynamic camera metadata, opaque secret reference, CameraOperator users, JWT tenant/store
  isolation, authenticated Python one-frame probe, API negative test and real Chrome UAT.
- Pending: set the authorized RTSP URL in the Python server environment and observe one real frame;
  then implement continuous reconnect/detection/event publishing separately.
- Security: no camera IP, username, password or raw RTSP URL is stored in application source or SQL.

## Phase 18

- Planning file: `../phase_implementation/PHASE_18_RETAIL_SECURITY_THEFT_DETECTION.md`
- Status: BLOCKED
- Completed: planning read and live-object inventory only.
- Pending: review/integrate recovered V1.16 SQL, then implement application/worker/UI/Python flows.
- Known issue: SQL/verifier exist only at divergent AIMain commit `055b052`; no selected-chain app implementation.

## Phase 2/5/17 Authentication UI Hardening Addendum

- Status: COMPLETE_AND_VERIFIED locally.
- Completed: live auth/user table inventory; self-service current-password verification; Identity rehash;
  tenant-admin in-scope reset; security-stamp rotation; all-session revocation; safe auth/business audits;
  Angular account-security and tenant-user reset UI; detailed support documentation.
- Security: TenantId/UserId are server-derived, store-scoped administrators cannot reset users outside their
  store visibility, administrators cannot use reset to bypass current-password verification for themselves,
  and no hash/plaintext credential is returned or logged.
- Testing: pinned .NET 8 build 0 warnings/errors; 104/104 unit; 230/230 integration/API; Angular lint;
  82/82 Angular; production build PASS.
- Pending production enhancement: recipient-owned, one-time invitation/forgot-password delivery flow.
