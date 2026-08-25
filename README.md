# CustSearch AI

CustSearch AI is a multi-tenant retail intelligence platform built with ASP.NET Core 8,
Angular, SQL Server, Dapper/stored procedures, EF Core for permitted transactional entity work,
SignalR, background workers, and a Python FastAPI/OpenCV/ONNX service. The current selected source
chain implements Phases 1–16. Phase 17 quality/deployment work is in progress; Phase 18 security
schema exists in the local database but its application source is not present in this branch.

Never infer household relationships from CCTV/co-visits and never label a person a thief from an
AI observation. Recognition is consent-gated. Security observations require human review.

## Prerequisites

- .NET SDK 8.0.424, pinned by `global.json`
- Node/npm versions compatible with the checked-in Angular lock file
- SQL Server 2022-compatible database and `sqlcmd`
- Python 3.12 for the AI service/tests
- Chromium installed by Playwright for browser tests
- Redis only when scale-out/backplane behavior is enabled

## Local secrets and connection

Committed runtime settings deliberately contain no workstation database connection or production
signing keys. Set them in the current PowerShell process or an approved secret store:

```powershell
$env:ConnectionStrings__CustSearchDatabase = 'Server=KRUTARTH-BHAVSA;Database=CustSearch_AI;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:Jwt__SigningKey = '<at-least-32-byte-local-secret>'
$env:ReportsExports__DownloadSigningKey = '<different-at-least-32-byte-local-secret>'
```

Recognition, CCTV service, integration, Redis, and webhook credentials must also come from
environment variables or secret storage. Do not commit passwords, access tokens, camera URLs,
biometric templates, or evidence.

## Database

Schema changes are versioned SQL scripts; EF migrations, `Database.Migrate()` and
`EnsureCreated()` are prohibited. For the current Phase 16 source chain:

```powershell
./database/run-phase16.ps1 -ServerInstance 'KRUTARTH-BHAVSA' -DatabaseName 'CustSearch_AI' -ValidateIdempotency
./database/verify-phase16-canonical.ps1 -ServerInstance 'KRUTARTH-BHAVSA'
```

The first command upgrades/verifies the named database without recreating it. The canonical
verifier uses a uniquely named disposable database and deletes only that exact database in its
`finally` cleanup. Do not run fresh-install validation against an existing business database.

The audited local database currently contains `V1.16.0`, ahead of this branch's canonical
`V1.15.0`. Do not downgrade, drop, or recreate it. See
`planning_document/ALL_PHASE_EXECUTION_CHECKPOINT.md` before running later scripts.

## Build and automated tests

From the repository root:

```powershell
dotnet restore CustSearch_AI.sln
dotnet build CustSearch_AI.sln --configuration Release --no-restore
dotnet test CustSearch_AI.sln --configuration Release --no-build

Set-Location src/CustSearch.Admin
npm ci
npm run lint
npm run test:ci
npm run build:production

Set-Location ../../tests/CustSearch.Admin.E2E
npm ci
npx playwright install chromium
npm test

Set-Location ../..
python -m pip install -r src/CustSearch.AI/requirements-dev.txt
ruff check src/CustSearch.AI tests/CustSearch.AI.Tests
$env:PYTHONPATH = 'src/CustSearch.AI'
python -m pytest -q tests/CustSearch.AI.Tests
```

Observed audit evidence and environment blockers are recorded in
`planning_document/PHASE_16_TEST_REPORT.md` and
`planning_document/ALL_PHASE_EXECUTION_CHECKPOINT.md`.

## Run locally

Open separate PowerShell terminals after setting the required environment variables.

API and Swagger:

```powershell
dotnet run --project src/CustSearch.API --launch-profile https
```

Angular Admin (`/api` and SignalR requests use the development proxy):

```powershell
Set-Location src/CustSearch.Admin
npm start
```

Worker:

```powershell
dotnet run --project src/CustSearch.Worker
```

Python CCTV service in explicitly configured Demo Mode:

```powershell
$env:CUSTSEARCH_AI_DEMO_MODE = 'true'
$env:CUSTSEARCH_AI_API_KEY = '<local-service-key>'
$env:PYTHONPATH = 'src/CustSearch.AI'
python -m uvicorn app.main:app --app-dir src/CustSearch.AI --host 127.0.0.1 --port 8000
```

Before using a real camera/ONNX model, disable Demo Mode only after camera authorization,
calibration, privacy, retention, and secret-storage checks are complete.

## Architecture and tenancy boundary

```text
Angular Admin → ASP.NET Core API → Application/Infrastructure
              → EF Core transactional operations or Dapper/stored procedures
              → SQL Server

Python CCTV → authenticated internal API → tenant/store-authorized processing
Worker      → leased/idempotent outbox, export and retention processing
SignalR     → authenticated tenant/store/user groups + REST recovery
```

TenantId is derived from the validated server session. StoreId is checked against authoritative
user assignments. Angular never calls SQL or stored procedures directly.

## Current status and documentation

- Phase traceability: `planning_document/ALL_PHASE_IMPLEMENTATION_MATRIX.md`
- Resume checkpoint: `planning_document/ALL_PHASE_EXECUTION_CHECKPOINT.md`
- Phase 16 observed tests: `planning_document/PHASE_16_TEST_REPORT.md`
- Main requirements: `planning_document/CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`
- Security/theft constraints: `planning_document/CustSearch_AI_SECURITY_THEFT_SHOPLIFTING_ADDENDUM.md`

Phase 15/16 PRs remain open/draft and must not be merged automatically. Continue on the dedicated
audit branch until quality/deployment gates and the live V1.16 code/schema drift are resolved.
