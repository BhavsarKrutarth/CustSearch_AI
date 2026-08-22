# CustSearch AI

Production-oriented multi-tenant retail intelligence platform built with ASP.NET Core 8, Angular, SQL Server, Dapper, EF Core, SignalR and Python AI services.

## Foundation prerequisites

- .NET SDK 8.0.424 (pinned by `global.json`)
- Node.js compatible with the pinned Angular version and npm 11+
- SQL Server 2022 and `sqlcmd`
- Python (added before the AI runtime is exercised)
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

## Database

Database changes are versioned SQL only. EF Core migrations, `Database.Migrate()` and `EnsureCreated()` are prohibited.

```powershell
.\database\run-phase2.ps1 -ServerInstance KRUTARTH-BHAVSA
```

The Phase 2 runner includes the foundation runner, then applies the tenant/authentication tables, indexes, stored procedure and version record. It uses Windows integrated authentication, is repeat-safe and never drops the database.

## Phase 2 local run

Start the API with its HTTPS development profile, then start Angular through the project-local CLI:

```powershell
dotnet run --project src/CustSearch.Api --launch-profile https

Set-Location src/CustSearch.Admin
npm start
```

Angular proxies same-origin `/api` calls to `https://localhost:7277`. The initial admin views are available at `/customer-admin` and `/platform-admin`, with Light, Dark and System theme modes.

## Configuration and secrets

Copy `.env.example` only as a local reference. Production must supply `Jwt__SigningKey` through environment variables, user secrets or the deployment secret store. JWT and refresh lifetimes are configurable under the `Jwt` section in `appsettings.json`. Never commit real secrets.

## Logs

API and Worker use structured Serilog events. Development file logs are written under each executable project's `logs/` directory and are ignored by Git. Audit events remain a separate business concern.
