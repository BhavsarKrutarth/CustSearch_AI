# Phase 17 Test Report

Run: `CUSTSEARCH_SMOKE_20260825_001`

Branch: `audit/all-phases-database-smoke`

Observed through: 2026-08-26 03:30 +05:30

Status: `BLOCKED` for deployment-environment gates; local regression is green.

| Suite | Command | Passed | Failed | Result |
|---|---|---:|---:|---|
| Release build | `dotnet build CustSearch_AI.sln --configuration Release --no-restore` | 9 projects | 0 | PASS |
| Unit | `dotnet test ... --no-build` | 104 | 0 | PASS |
| Integration/API | `dotnet test ... --no-build` | 225 | 0 | PASS |
| Angular lint/unit/build | repository scripts | 78 | 0 | PASS |
| Chromium E2E | `npm test` | 49 | 0 | PASS |
| Python | Ruff and pytest | 7 | 0 | PASS |
| Live SQL | runner twice, verifier, constraints | all checks | 0 | PASS |
| NuGet vulnerability | `dotnet list ... --vulnerable --include-transitive` | 9 projects clean | 0 | PASS |
| E2E npm audit | `npm audit --audit-level=high` | 0 vulnerabilities | 0 | PASS |

The E2E dependency scan initially identified the Playwright 1.55.0 browser certificate advisory.
The exact patched 1.55.1 release was installed, the audit became clean, and all 49 Chromium tests
passed again. The Angular build retains one existing 61-byte component-style budget warning.

## Live connected evidence

Encrypted Windows Integrated Security connected successfully. A deterministic two-tenant dataset was
seeded twice without duplication. Login, `/me`, refresh rotation, logout, platform scope and staff store
scope passed. Tenant A and Store-scoped Staff A received 404 for Tenant B's customer while Tenant B
received 200 for its own customer. `DBCC CHECKCONSTRAINTS` returned no violations.

## Blocked gates

- SQL Server 2022 validation: the reachable local engine identifies as version 17, compatibility 160.
- Redis multi-node/backplane validation: no Redis topology is configured.
- IIS/HTTPS/WebSocket deployed smoke: no IIS site/certificate is configured.
- Physical RTSP/production ONNX calibration: Demo Mode is the validated fallback.
- Phase 18: live V1.16 objects have no matching source branch in the selected ancestry.

These are recorded as `BLOCKED`, not passed. Phase 18 must not be declared implemented from live
database objects alone.
