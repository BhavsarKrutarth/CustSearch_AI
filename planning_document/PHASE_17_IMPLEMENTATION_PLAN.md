# Phase 17 Implementation Plan — Full Quality & Deployment

## Requirement extraction

- Execute the complete .NET, Python, Angular and Playwright suites and preserve evidence.
- Retain explicit tenant-isolation, authorization, negative-path and WebSocket reconnect coverage.
- Publish useful Swagger/OpenAPI security metadata plus a versioned, secret-free Postman collection/environment.
- Produce a deterministic database fresh-install verifier and deployment validation commands.
- Add IIS Angular SPA rewrite, API reverse-proxy/WebSocket guidance and a production deployment/runbook checklist.
- Validate production Angular output and document configuration/secrets, SQL Server 2022, Redis/backplane, Worker, Python, backup/restore and rollback gates.

## Existing implementation audit

| Area | Existing | Gap / action |
|---|---|---|
| .NET | 98 unit and 219 integration tests green | Preserve a single repeatable quality command and report |
| Python | Ruff/pytest green from Phase 15 | Add to repeatable quality command/documentation |
| Angular | lint, 78 unit tests and production build green | Keep existing 61-byte style warning visible |
| Playwright | 47 platform/tenant workflows green | No explicit WebSocket reconnect E2E found; add deterministic reconnect/fallback coverage |
| OpenAPI | Basic Swagger generation/UI in Development | Add bearer security definition, operation requirements and API metadata |
| Postman | README only | Add versioned collection and local environment template without secrets |
| IIS | No `web.config`/runbook | Add SPA rewrite/static security configuration and WebSocket/reverse-proxy deployment guidance |
| Database deployment | Versioned runners and canonical SQL | Add reusable exact-name disposable canonical verifier with guaranteed cleanup |
| CI | No repository workflow | Add a Windows-oriented quality script first; workflow credentials/runner policy remain deployment-owned |

## Acceptance criteria

- One documented command sequence validates SQL, .NET, Python, Angular and browser suites without hiding failures.
- Canonical verifier creates only a validated disposable database name and drops it in `finally`.
- Swagger accurately advertises JWT bearer auth and remains non-production by default.
- Postman artifacts contain no credentials/tokens and demonstrate health/auth/tenant/platform boundaries.
- IIS configuration supports SPA fallback without rewriting API/hub/health/static-file requests and documents WebSocket prerequisites.
- Production readiness remains BLOCKED until SQL Server 2022 and enabled Redis/backplane are tested in the target environment.

## Executed result — 2026-08-25

- Added `Invoke-QualityGates.ps1`; the complete local gate passed with canonical SQL validation and guaranteed disposable-database cleanup.
- Added explicit SignalR disconnect/reconnect/cursor recovery E2E; full Playwright suite is 48/48.
- Added JWT-aware OpenAPI metadata; live Swagger exposed `Bearer` on 172 protected operations, omitted it on login, and the protected operations health endpoint returned 401 anonymously.
- Added secret-free Postman collection/environment; both parse as valid JSON.
- Added Angular `web.config`; production build copied valid XML into `dist/custsearch-admin/browser`.
- Upgraded E2E-only Playwright/`ws` dependencies after audit; Angular and E2E audits both report zero vulnerabilities.
- Deployment-environment gates for SQL Server 2022 and enabled multi-node Redis/backplane remain pending and are recorded in `OPEN_ISSUES.md`.
