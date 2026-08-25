# Phase 16 — Operational Platform Test Report

**Result:** LOCAL PASS — SQL Server 2022 environment validation blocked

**Validated branch:** `audit/all-phases-database-smoke`

**Source branch:** `phase16-operations` at `3905775e3556259494688998cab3875a86c66dcf`

**Run:** `CUSTSEARCH_SMOKE_20260825_001`

**Observed date:** 2026-08-25 (Asia/Calcutta)

Phase 16 code, canonical database installation, live-database upgrade verification, frontend,
browser, and Python regression gates passed on the local workstation. The available local SQL
engine reports version `17.0.1000.7` with compatibility level 160, so the required SQL Server
2022-specific execution remains `BLOCKED`; Docker is not installed on this workstation. This
report does not claim that blocked platform check passed.

| Gate | Observed status | Evidence |
|---|---:|---|
| Fully green Phase 15 prerequisite | PASS | Phase 15 is a verified ancestor of Phase 16; Phase 15 remote checks are successful. |
| Immutable/safe audit | PASS | Integration tests reject audit mutation and sensitive audit metadata. |
| Worker lease, pause/resume, retry, idempotency | PASS | Phase 16 service/regression tests passed in the 225-test integration run. |
| Redis unavailable and SQL unavailable readiness | PASS | Three focused readiness tests passed, including fail-closed dependency cases. |
| Retention and audit trail | PASS | Integration tests and live/canonical SQL verifier passed. |
| Cross-tenant operational control resistance | PASS | Platform-only policy, permission, and unknown tenant/store scope rejection tests passed. |
| Secret/reference masking | PASS | Service test proves stored reference is returned only as a masked suffix. |
| Angular and Playwright | PASS | Angular lint; 78/78 unit tests; production build; 49/49 Playwright tests. |
| V1.15 upgrade/runner/verifier/canonical | PASS | Live runner executed twice; verifier and `DBCC CHECKCONSTRAINTS` passed; isolated canonical database passed and was dropped. |
| Full Phase 5–16 regression | PASS | .NET unit 104/104; .NET integration 225/225; Angular 78/78; Playwright 49/49; Python 7/7. |
| SQL Server 2022-specific validation | BLOCKED | Local engine is version 17, not SQL Server 2022; Docker command is unavailable. |
| Redis multi-node/backplane validation | BLOCKED | No Redis multi-node environment is configured; fail-closed readiness behavior is covered locally. |

## Repairs made during validation

- Cached strict web JSON serializer options to resolve analyzer failure `CA1869` while retaining
  rejection of unknown client-controlled fields.
- Allowed authorized export streams to coexist with expiry cleanup and disabled pooling in
  disposable SQLite tests, eliminating deterministic teardown file locks.
- Added executable Redis and SQL unavailable readiness coverage.
- Synchronized Phase 16 V1.15 into the canonical SQL file without rewriting existing canonical
  line endings, and added an isolated, safely cleaned-up canonical verifier.
- Added business/security comments to the readiness checks and export file-sharing behavior.

## Observed test executions

| Command / validation | Start / end | Exit | Passed | Failed | Skipped | Observed result |
|---|---|---:|---:|---:|---:|---|
| `dotnet build CustSearch_AI.sln --configuration Release` | 2026-08-25 local run | 0 | Build | 0 errors | n/a | 0 warnings, 0 errors after fixes. |
| `dotnet test tests/CustSearch.UnitTests/... --configuration Release` | 2026-08-25 local run | 0 | 104 | 0 | 0 | PASS. |
| `dotnet test tests/CustSearch.IntegrationTests/... --configuration Release --no-restore` | 20:14:26–20:14:51 +05:30 | 0 | 225 | 0 | 0 | PASS in 20 seconds. |
| Focused `PhaseSixteenReadinessTests` | 2026-08-25 local run | 0 | 3 | 0 | 0 | Redis disabled/unavailable and SQL unavailable behavior PASS. |
| `npm run lint`, `npm run test:ci`, `npm run build:production` | 2026-08-25 local run | 0 | 78 tests | 0 | 0 | PASS; existing admin-shell style budget warning is 61 bytes. |
| Playwright `npm test` | 2026-08-25 local run | 0 | 49 | 0 | 0 | Chromium E2E PASS. |
| Ruff and Python pytest | 2026-08-25 local run | 0 | 7 | 0 | 0 | PASS. |
| Phase 16 live runner twice + verifier + constraints | 2026-08-25 local run | 0 | All checks | 0 | 0 | PASS; live database data/version were preserved. |
| `database/verify-phase16-canonical.ps1` | completed 20:12 +05:30 | 0 | All checks | 0 | 0 | Isolated database created, V1.15 verified, constraints checked, database dropped. |

## Environment facts and recovery commands

- Live `CustSearch_AI` already contains database versions through `V1.16.0`, later than this
  Phase 16 branch's canonical `V1.15.0`. No downgrade or destructive synchronization was done.
- To close the SQL Server 2022 blocker, run the repository's Phase 16 SQL validation workflow on
  an actual SQL Server 2022 instance, then rerun:

```powershell
./database/run-phase16.ps1 -ServerInstance '<SQL2022_INSTANCE>' -DatabaseName 'CustSearch_AI_Phase16_Verify'
./database/verify-phase16-canonical.ps1 -ServerInstance '<SQL2022_INSTANCE>'
```

- To close the Redis topology blocker, configure the approved Redis/backplane environment, set
  `OperationalPlatform:RedisEnabled=true`, and exercise readiness, reconnect, and multi-node
  SignalR delivery. Secrets must be supplied through environment/secret storage, never Git.
