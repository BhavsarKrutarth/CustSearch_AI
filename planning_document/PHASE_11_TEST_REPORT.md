# Phase 11 — Alerts & Real-Time Test Report

**Result:** Green  
**Date:** 2026-08-24  
**Branch:** `phase11-alerts-realtime`  
**Validated code commit:** `2228180a19397bf9794ec8b7a18184ad84e57d37`  
**Validated canonical SQL commit:** `23ae4cb374a2462c29463bd300408e4630573a6c`  
**Authoritative workflow:** Phase 11 Validate run `32746620273`, job `97493587673`

## Baseline validation

- `AIMainBranch` was verified at Phase 10 merge commit `a66721e64b56f7e7a5175ab5b56519661cff6ad7` before Phase 11 implementation began.
- Phase 10 pull request 12 and its implementation report were green.
- The Phase 11 workflow requires exactly one V1.9.0 ledger entry before applying V1.10.0.

## Full regression results

| Gate | Result | Evidence |
|---|---:|---|
| .NET restore and Release build | Green | 0 build errors; analyzer rules enforced as errors |
| .NET unit tests | Green | 75 passed, 0 failed, 0 skipped |
| .NET integration/API tests | Green | 157 passed, 0 failed, 0 skipped |
| Angular dependency install | Green | Reproducible `npm ci` from the committed lock file |
| Angular lint | Green | `npm run lint` passed |
| Angular unit tests | Green | 27 files, 64 tests passed |
| Angular production build | Green | Production bundle generated successfully |
| Playwright full regression | Green | 34 passed |
| Python lint and tests | Green | Ruff passed; 3 pytest tests passed |
| Phase 11 security/static checks | Green | TenantId rejection, server groups, transactional outbox and credential exclusions verified |

## Phase 11 behavior coverage

| Requirement | Passing evidence |
|---|---|
| Tenant SignalR isolation | Tenant-wide messages route only to `tenant:{TenantId}` and payload tenant metadata is verified |
| Store SignalR isolation | Store messages route only to `store:{StoreId}`; tenant/store REST and recovery predicates are exercised |
| Arbitrary group join rejection | Authenticated hub exposes no Join/Subscribe/Group client method; groups are assigned server-side only |
| Transactional outbox | Alert, durable event and SignalR outbox row commit in one EF transaction |
| Retries and dead letters | Attempt count, due time, retry state and terminal dead-letter transitions are unit/integration tested |
| Idempotency and duplicate suppression | Unique alert/outbox keys plus concurrent duplicate-create integration test produce one authoritative alert/outbox |
| Reconnect recovery | Durable `afterEventId` recovery plus Angular event-ID de-duplication/cursor advancement are tested |
| Unauthorized acknowledgement | Out-of-store alert acknowledgement is hidden/rejected; endpoint requires `Alerts.Acknowledge` |
| Browser TenantId injection | Query/route filter, disallow-unmapped JSON DTOs, deserialization test and Angular request tests reject/omit TenantId |

## Database validation

- SQL Server 2022 container became healthy before database tests.
- `database/09_Upgrade/V1.10.0_Phase11_AlertsRealtime.sql` ran twice against the validated V1.9.0 baseline.
- `database/run-phase11.sql` ran twice as standalone T-SQL; `database/verify-phase11.sql` printed `PHASE11_DATABASE_VERIFICATION_GREEN`.
- The prospective canonical `database/CustSearchAi.sql` installed from scratch through V1.10.0 and passed all constraints.
- Exactly one V1.9.0 and one V1.10.0 database version were verified.
- The canonical V1.10.0 SQL generated only after the complete push workflow passed and was persisted in commit `23ae4cb374a2462c29463bd300408e4630573a6c`.

## Release conclusion

Phase 11 meets the requested completion gate. The draft pull request remains unmerged; no `AIMainBranch` merge is authorized by this Phase 11 request.
