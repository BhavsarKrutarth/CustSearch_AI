# Phase 17 — Full Quality and Deployment

Status: In Progress

## Scope

Full .NET/Python/Angular/Playwright validation, Swagger/API/runbook documentation, production
configuration hardening, IIS SPA/WebSocket deployment and final security review.

## Implemented in audit run CUSTSEARCH_SMOKE_20260825_001

- Replaced stale Phase 2 root setup with current Phase 16/17 local/database/start/test commands.
- Removed workstation-specific SQL connections from committed API/Worker runtime configuration.
- Added optional allowlisted forwarded-header handling, production HSTS and non-wildcard default hosts.
- Added project-wise component/comment catalog and meaningful comments to major services/workers.
- Added database object, code traceability and security review catalogs from the live database.
- Added local setup, post-login, API, role, SignalR, worker, CCTV, IIS and troubleshooting guides.
- Added deterministic two-tenant Phase 1–16 smoke data, verifier and exact-token cleanup.
- Executed real SQL/API login, refresh, logout and cross-tenant denial tests.
- Fixed SQL Server refresh-token transaction execution-strategy incompatibility found by live smoke.

## Remaining gates

- Add/validate deployable IIS SPA rewrite artifact and perform an actual IIS/WebSocket smoke test.
- Validate on an actual SQL Server 2022 environment.
- Validate Redis multi-node SignalR/backplane behavior.
- Close any remaining critical/high security findings; live V1.16/source drift blocks Phase 18.

## Done Summary

Not complete. Local implementation/testing is in progress; deployment/environment gates above remain
blocked or pending. Do not start Phase 18 application implementation until Phase 17 is evidence-green
and V1.16 schema provenance is safely reconciled.

## Observed local regression

- Added separate reviewed IIS API hosting and Angular SPA rewrite templates with WebSocket support.
- Release solution build: 0 warnings and 0 errors.
- .NET: 104 unit and 225 integration tests passed.
- Angular: lint, 78 unit tests and production build passed.
- Playwright Chromium: 49 tests passed after upgrading the patched 1.55.1 release.
- Python: Ruff passed and 7 pytest cases passed.
- Supply chain: NuGet found no vulnerable packages; both Angular and E2E npm audits report zero vulnerabilities.
