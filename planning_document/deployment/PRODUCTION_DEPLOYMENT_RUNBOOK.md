# Production Deployment Runbook

## Pre-deployment gates

1. Use SQL Server 2022 and validate backups plus a tested restore point.
2. Run `database/verify-canonical-fresh-install.ps1` against a non-production SQL Server and run the V1.15 upgrade/verifier against the target clone.
3. Run .NET, Python, Angular and Playwright quality suites documented in the repository README.
4. Review pending `OPEN_ISSUES.md`; production readiness is false while target SQL Server 2022 or enabled Redis/backplane evidence is missing.
5. Supply secrets through the deployment secret provider. Scan publish output/configuration for blank JWT keys and committed credentials.

## Order of deployment

1. Stop new administrative writes and pause Worker services if the release requires a maintenance window.
2. Take/verify the SQL backup; record current `DatabaseVersions` and application artifact version.
3. Apply the next explicit SQL upgrade with `sqlcmd -b`; abort on any non-zero exit code.
4. Deploy API and Worker artifacts from the same release; deploy Angular static output and its `web.config`.
5. Start one API instance, validate liveness/readiness/auth, then start Worker and confirm a fresh healthy heartbeat.
6. Validate Redis/backplane (when enabled), both SignalR hubs, Python boundary, camera health and queue depth before scaling out.

## Smoke tests

- Use the secret-free Postman collection with local current values.
- Validate 200/400/401/403/404 paths, one platform workflow and one tenant workflow.
- Confirm tenant requests contain no browser-controlled `TenantId` and wrong-store access is hidden/rejected.
- Queue/download a report, verify its SHA-256 metadata, and verify expiry cleanup in a controlled test scope.

## Rollback

- Roll back application artifacts only when the previous version is compatible with the already-applied schema.
- Database rollback is restore/forward-fix according to the reviewed change; never use `git clean`, EF migrations, `EnsureCreated`, or an automatic database drop.
- If a post-deploy gate fails, remove the node from traffic, preserve logs/correlation IDs, stop affected Workers and record the incident before retrying.
