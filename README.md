# CustSearch AI

Production-oriented multi-tenant retail intelligence platform built with ASP.NET Core 8, Angular, SQL Server, Dapper/stored procedures, SignalR and Python AI services.

## Foundation prerequisites

- .NET SDK 8.0.424 (pinned by `global.json`)
- Node.js compatible with the pinned Angular version and npm 11+
- SQL Server 2022 and `sqlcmd`
- Python 3.12 for the AI runtime and test suite
- Docker Desktop only for the optional container workflow

## Build

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build

Set-Location src/CustSearch.Admin
npm ci
npm run lint
npm run test:ci
npm run build:production
```

Run all local release gates (including a disposable canonical database install) from the repository root:

```powershell
.\Invoke-QualityGates.ps1 -SqlServerInstance KRUTARTH-BHAVSA -InstallDependencies
```

Use `-SkipDatabase` only when SQL Server is deliberately unavailable; a deployment candidate is not database-validated until the command succeeds without that switch. The E2E suite installs its pinned Chromium revision when `-InstallDependencies` is supplied.

## Database

Database changes are versioned SQL only. EF Core migrations, `Database.Migrate()` and `EnsureCreated()` are prohibited.

```powershell
.\database\verify-canonical-fresh-install.ps1 -ServerInstance KRUTARTH-BHAVSA
sqlcmd -S KRUTARTH-BHAVSA -d CustSearch_AI -E -C -b -i .\database\run-phase16.sql
sqlcmd -S KRUTARTH-BHAVSA -d CustSearch_AI -E -C -b -i .\database\verify-phase16.sql
```

The canonical verifier creates only a generated `CustSearch_AI_Verify_*` disposable database and guarantees cleanup. Upgrade runners are repeat-safe and do not delete production-style data.

## Local run

Start the API with its HTTPS development profile, then start Angular through the project-local CLI:

```powershell
dotnet run --project src/CustSearch.API --launch-profile https

Set-Location src/CustSearch.Admin
npm start
```

Angular proxies same-origin `/api` calls to `https://localhost:7277`. The initial admin views are available at `/customer-admin` and `/platform-admin`, with Light, Dark and System theme modes.

Production IIS, WebSocket, configuration, backup/restore and rollback guidance is under `planning_document/deployment/`.

## Configuration and secrets

Copy `.env.example` only as a local reference. Production must supply `Jwt__SigningKey` through environment variables, user secrets or the deployment secret store. JWT and refresh lifetimes are configurable under the `Jwt` section in `appsettings.json`. Never commit real secrets.

## Logs

API and Worker use structured Serilog events. Development file logs are written under each executable project's `logs/` directory and are ignored by Git. Audit events remain a separate business concern.
