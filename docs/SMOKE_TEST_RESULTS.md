# Smoke Test Results

Run: `CUSTSEARCH_SMOKE_20260825_001`

Observed date: 2026-08-25 (Asia/Calcutta)
Branch checkpoint before smoke: `4fcb4c6`

| Test | Expected | Actual | Result |
|---|---|---|---|
| Encrypted Windows-auth SQL connection | Connected to `CustSearch_AI` | Server/login/database returned | PASS |
| Phase 16 runner and idempotency | Apply twice without duplicate/drift | Both executions and verifier passed | PASS |
| Canonical V1.15 isolated install | Fresh disposable DB valid and removed | constraints PASS; exact DB dropped | PASS |
| Connected smoke seed | Deterministic tenant/store/business graph | Tenant `10019`, store `11`, customer `5`, invoice `3`, camera `3` | PASS |
| Seed rerun | No duplicate business keys | Same IDs returned | PASS |
| Isolation seed | Separate tenant/store/customer | Tenant `10020`, store `12`, customer `6` | PASS |
| Smoke verifier | Required relationships and constraints valid | verifier/DBCC PASS | PASS |
| Invalid password | 401 | 401 | PASS |
| Tenant Admin login and `/me` | current tenant/session returned | tenant `SMOKE-TENANT-001`, expected email | PASS |
| Refresh rotation | new access/session issued | PASS after retry-strategy repair | PASS |
| Logout | refresh session revoked/cookie deleted | HTTP 204 | PASS |
| Platform Admin login | platform scope | `isPlatformAdmin=true` | PASS |
| Staff login | one authorized store | `storeIds.Count=1` | PASS |
| Tenant A reads Tenant B customer | deny without existence leak | 404 | PASS |
| Store-scoped Staff A reads Tenant B customer | deny | 404 | PASS |
| Tenant B reads own customer | allow | 200 | PASS |
| Release .NET build | no warnings/errors | 0 warnings, 0 errors | PASS |
| .NET unit suite | all pass | 104/104 | PASS |
| .NET integration suite | all pass | 225/225 | PASS |
| Angular lint/unit/production | all pass | lint PASS; 78/78; build PASS | PASS |
| Playwright Chromium after security patch | all pass | 49/49 in 1.6 minutes | PASS |
| Python Ruff/pytest | all pass | Ruff PASS; 7/7 in 1.52 seconds | PASS |
| NuGet vulnerable package scan | no known vulnerability | none in all nine projects | PASS |
| E2E npm audit | no high vulnerability | 0 vulnerabilities after `@playwright/test` 1.55.1 | PASS |

## Defect found and fixed

The first real-SQL refresh returned HTTP 500 because SQL Server retry-on-failure was configured while
the refresh rotation opened a user transaction outside the execution strategy. The whole atomic
rotation now runs through `CreateExecutionStrategy()` with a clean tracker on retry. Focused auth
integration tests passed 28/28 after the repair and the live login/refresh/logout flow passed.

## Data and credentials

No password/hash is recorded here or in Git. Smoke users currently have an agent-generated local
password that is intentionally not disclosed. Before manual login, set `CUSTSEARCH_SMOKE_PASSWORD`
to your chosen value and rerun `database/10_TestData/run-smoke-data.ps1`; only deterministic smoke
accounts are rotated.

No biometric template and no Phase 18 security incident was inserted. The smoke verifier explicitly
fails if either appears for the smoke tenant.

## Blocked smoke areas

- SQL Server 2022-specific environment execution: local engine is version 17.
- Redis multi-node/SignalR backplane behavior: no Redis topology configured.
- Physical RTSP/ONNX calibration: Demo Mode only.
- IIS deployment/WebSocket smoke: no deployed IIS site configured.
- Phase 18 flows: source branch absent and live schema provenance unresolved.
