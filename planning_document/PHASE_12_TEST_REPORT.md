# Phase 12 — Integrations Test Report

**Result:** Green  
**Date:** 2026-08-24  
**Branch:** `phase12-integrations`  
**Validated code commit:** `330f342a42223fa6f92b11bdb985adf6766485ba`  
**Validated canonical SQL commit:** `d35548cf87e36dca8b07440696f7f61a0013220b`  
**Authoritative workflow:** Phase 12 Validate run `32755482226`, job `97521917734`

## Baseline validation

- Phase 11 pull request 13 was merged before implementation began.
- `AIMainBranch` was verified at merge commit `b851c467293894c6c00e6ad67fcfcf28e776f853` with the V1.10.0 canonical schema and green Phase 6–11 workflows.
- `phase12-integrations` was created from that exact merge commit; no existing branch or commit was rewritten.

## Full regression results

| Gate | Result | Evidence |
|---|---:|---|
| .NET restore and Release build | Green | 0 build errors; recommended analyzers enforced as errors |
| .NET unit tests | Green | 81 passed, 0 failed, 0 skipped |
| .NET integration/API tests | Green | 174 passed, 0 failed, 0 skipped |
| Angular dependency install | Green | Reproducible `npm ci` from committed lock file |
| Angular lint | Green | `npm run lint` passed |
| Angular unit tests | Green | 28 files, 67 tests passed |
| Angular production build | Green | Production bundle generated successfully |
| Playwright full regression | Green | 36 passed |
| Python lint and tests | Green | Ruff passed; 3 pytest tests passed |
| Phase 12 security/static checks | Green | HMAC, signed tenant, limits, DTO injection, SSRF, secret-response and payload-free audit guards verified |
| Phase 6–12 workflows | Green | Phase 6, 7, 8, 9, Phase 9 SQL-only, 10, 11 and 12 all succeeded on the validated code head |

## Phase 12 security and behavior coverage

| Requirement | Passing evidence |
|---|---|
| Tenant-scoped configuration | Management service derives TenantId from authenticated server context; route/query/body TenantId injection is rejected |
| Secret handling and rotation | Database stores opaque references only; runtime resolver reads configured values; API/UI return booleans and masked hints; previous signing reference is accepted only during configured grace |
| HMAC authentication | Signature uses HMAC-SHA256 and fixed-time comparison over timestamp, provider event ID, idempotency key, signed tenant and exact raw body |
| Timestamp and replay protection | Bounded clock skew, signed tenant equality, provider-event uniqueness and idempotency uniqueness are exercised |
| Invalid input | Invalid HMAC, expired timestamp, wrong tenant, invalid JSON, oversized body and payload mismatch tests pass |
| Request protection | Inbound endpoint has JSON content enforcement, a 256 KiB request limit, correlation middleware and a per-integration fixed-window rate-limit policy |
| Duplicate suppression | A duplicate signed webhook returns successful duplicate acknowledgement while retaining one authoritative receipt and delivery log |
| Outbound transaction boundary | Business service commits outbox metadata; only the worker transport performs external HTTPS delivery |
| Retry and dead letters | Bounded exponential retry, permanent 4xx dead-letter, attempt exhaustion and tenant-scoped audited manual retry pass |
| Delivery audit | Correlation, provider, direction, status, duration, HTTP code and error category are stored without full inbound bodies, secrets or credentials |
| SSRF protection | Configuration and dispatch require HTTPS, reject user-info/loopback endpoints, resolve DNS and reject non-public addresses |
| Angular administration | Integration settings, connection/webhook status, masked rotation controls, delivery history, manual retry and permission-denied routing pass unit/E2E coverage |

## Database validation

- `database/09_Upgrade/V1.11.0_Phase12_Integrations.sql` ran twice against the validated V1.10.0 baseline on SQL Server 2022.
- `database/run-phase12.sql` ran twice as standalone T-SQL; `database/verify-phase12.sql` printed `PHASE12_DATABASE_VERIFICATION_GREEN`.
- The prospective canonical `database/CustSearchAi.sql` installed from scratch through V1.11.0 and passed all constraints.
- Exactly one V1.10.0 and one V1.11.0 database ledger row were verified at runtime.
- Replay/idempotency unique indexes, tenant-composite foreign keys, outbox claim locks (`UPDLOCK`/`READPAST`) and the tenant permissions were verified.
- The tested V1.11.0 canonical SQL was persisted without replacing any earlier phase content.

## Release conclusion

Phase 12 meets its requested completion gate. Pull request 14 remains draft and unmerged; no `AIMainBranch` merge was requested for this phase.
