# CustSearch AI Project-Wise Manual Run Guide

This guide starts every executable project manually on the local workstation. Use a separate
PowerShell terminal for each long-running process and keep the API terminal running while using the
Angular or Python projects.

## Local addresses

| Component | Address | Direct SQL access |
|---|---|---|
| SQL Server | `KRUTARTH-BHAVSA` / `CustSearch_AI` | N/A |
| ASP.NET API | `https://localhost:7277` and `http://localhost:5002` | Yes |
| Worker | background process | Yes |
| Angular Admin | `http://localhost:4200` | No; `/api` and `/hubs` proxy to API |
| Python AI | `http://localhost:8000` | No; events go to protected .NET API |

The API and Worker local settings already contain this Windows-authenticated connection:

```text
Server=KRUTARTH-BHAVSA;Database=CustSearch_AI;Integrated Security=True;Encrypt=True;TrustServerCertificate=True
```

No database password is needed because the processes use the current Windows account. For another
machine/server, override it in the terminal with `ConnectionStrings__CustSearchDatabase`.

## 0. Verify SQL Server first

Open PowerShell:

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI"

sqlcmd -S "KRUTARTH-BHAVSA" -d "CustSearch_AI" -E -C -b -Q `
  "SELECT @@SERVERNAME ServerName, DB_NAME() DatabaseName, SUSER_SNAME() LoginName;"
```

Expected: database is `CustSearch_AI`, a server name is returned, and the login is the current Windows
user. If this fails, do not start the application; check the `SQL Server (MSSQLSERVER)` Windows service.

## 1. Start the ASP.NET API — Terminal 1

The API must start before Angular and before Python sends events.

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI"

& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" restore CustSearch_AI.sln
& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" run `
  --project src\CustSearch.API\CustSearch.API.csproj `
  --launch-profile https
```

Keep this terminal open. Verify in another terminal or browser:

```powershell
Start-Process "https://localhost:7277/swagger"
curl.exe -k -i "https://localhost:7277/health/live"
curl.exe -k -i "https://localhost:7277/health/ready"
```

Expected: Swagger opens and both health endpoints return HTTP 200. The `-k` option is only for the
local ASP.NET development certificate.

## 2. Start the Worker — Terminal 2

Start the Worker after the API/database validation. The Worker processes integrations, exports,
retention, leases and heartbeats; it does not expose an HTTP page.

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI"

& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" run `
  --project src\CustSearch.Worker\CustSearch.Worker.csproj `
  --launch-profile CustSearch.Worker
```

Expected console lines include `Application started` and Worker activity without `FTL`, missing-table
or missing-column errors. Confirm the fresh database heartbeat:

```powershell
sqlcmd -S "KRUTARTH-BHAVSA" -d "CustSearch_AI" -E -C -b -Q `
  "SELECT TOP (10) InstanceId,WorkerType,IsReady,LastHeartbeatUtc FROM dbo.WorkerHeartbeats ORDER BY LastHeartbeatUtc DESC;"
```

## 3. Start Angular Admin — Terminal 3

Keep the API running. Angular does not connect to SQL directly. Its checked-in development proxy sends
`/api` and `/hubs` to `https://localhost:7277`.

First run only, or whenever `package-lock.json` changes:

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI\src\CustSearch.Admin"
npm ci
```

Start Angular:

```powershell
npm start
```

Open:

```powershell
Start-Process "http://localhost:4200/login"
```

If Angular displays an API/proxy error, first confirm `https://localhost:7277/health/live` and accept
the local development certificate in the browser if required.

## 4. Start Python AI — Terminal 4

The Python service never opens a SQL connection. It performs camera/AI work and targets the protected
.NET endpoint `https://localhost:7277/api/internal/cctv/events`.

Install pinned dependencies:

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI"
python -m pip install -r src\CustSearch.AI\requirements-dev.txt
```

Start in Demo Mode:

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI\src\CustSearch.AI"
$env:CUSTSEARCH_AI_DEMO_MODE = "true"
python -m uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
```

Verify:

```powershell
curl.exe -i "http://127.0.0.1:8000/health/live"
curl.exe -i "http://127.0.0.1:8000/health/ready"
```

For authenticated event publishing, set the service identity values configured for the .NET API:

```powershell
$env:CUSTSEARCH_AI_DOTNET_EVENT_URL = "https://localhost:7277/api/internal/cctv/events"
$env:CUSTSEARCH_AI_SERVICE_ID = "<configured-local-service-id>"
$env:CUSTSEARCH_AI_SERVICE_SECRET = "<local-secret-not-committed-to-git>"
```

Demo Mode remains usable without a physical RTSP camera or ONNX production model.

For a dynamically configured physical camera, never edit Python/source code with an IP or password.
Configure one allow-listed runtime variable, restart Python, and call the authenticated probe:

```powershell
$env:CUSTSEARCH_AI_API_KEY = "<local-service-api-key>"
$env:CUSTSEARCH_CAMERA_ENTRY01_RTSP = "rtsp://<authorized-user>:<password>@<camera-ip>:554/<stream-path>"
python -m uvicorn app.main:app --host 127.0.0.1 --port 8000

$headers = @{ "X-CustSearch-AI-Key" = $env:CUSTSEARCH_AI_API_KEY }
$body = @{ configuration_reference = "env:CUSTSEARCH_CAMERA_ENTRY01_RTSP"; timeout_seconds = 5 } |
  ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:8000/v1/cctv/cameras/probe" `
  -Headers $headers -ContentType "application/json" -Body $body
```

The returned object contains connection/frame metadata only. The RTSP URL, password and frame are
not returned. Continuous detection/event publishing remains a separate implementation step.

To repeat the real Google Chrome access-isolation flow, keep API and Angular running, then set UAT
values in the current shell (none are embedded in the script):

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI\tests\CustSearch.Admin.E2E"
$env:CUSTSEARCH_MANUAL_TEST_PASSWORD = "<local-uat-password>"
$env:CUSTSEARCH_UAT_PLATFORM_USER = "<platform-user>"
$env:CUSTSEARCH_UAT_OFFICE_TENANT = "<office-tenant-code>"
$env:CUSTSEARCH_UAT_OFFICE_USER = "<camera-operator-user>"
$env:CUSTSEARCH_UAT_OFFICE_CAMERA_NAME = "<configured-camera-name>"
$env:CUSTSEARCH_UAT_NO_CAMERA_TENANT = "<isolation-tenant-code>"
$env:CUSTSEARCH_UAT_NO_CAMERA_USER = "<isolation-user>"
node .\scripts\manual-server-camera-access-flow.mjs
```

The script uses separate browser contexts for every actor and fails if the isolation user sees the
office camera. Evidence images are local ignored artifacts and contain no credentials.

## 5. Optional Redis scale-out

Redis is disabled by default, so it is not required for ordinary one-API-node development. To test
multiple API nodes, start a Redis-compatible server and set these variables before starting each API:

```powershell
$env:ConnectionStrings__Redis = "127.0.0.1:6379,abortConnect=false"
$env:OperationalPlatform__RedisEnabled = "true"
$env:OperationalPlatform__RedisEndpoint = "redis://127.0.0.1:6379"
```

Do not enable Redis unless the endpoint is running; `/health/ready` intentionally fails closed when an
enabled dependency is unavailable.

For a repeatable two-node SignalR proof, run API node A with the alert dispatcher disabled and node B
with it enabled, obtain a Tenant Admin access token using username `smoke.tenantadmin` (not its email)
and tenant code `SMOKE-TENANT-001`, then run:

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI\src\CustSearch.Admin"
$env:CUSTSEARCH_SIGNALR_TOKEN = "<tenant-admin-access-token>"
$env:CUSTSEARCH_SIGNALR_NODE_A = "http://127.0.0.1:5081"
$env:CUSTSEARCH_SIGNALR_NODE_B = "http://127.0.0.1:5082"
node .\scripts\redis-backplane-smoke.mjs
```

Expected output contains `"result":"PASS"`. This test creates a deterministic-keyed alert on node B
and waits for the matching cross-node `AlertEvent` on node A.

## 6. Recommended startup and shutdown order

Startup:

```text
1. SQL Server
2. Redis only when scale-out testing is required
3. ASP.NET API
4. Worker
5. Angular Admin
6. Python AI
```

Shutdown: press `Ctrl+C` in Python, Angular, Worker and API terminals. SQL Server can remain running.

## 7. Quick troubleshooting

| Symptom | Check |
|---|---|
| API says database connection missing | confirm the API `appsettings` file and Development profile |
| Login/API returns SQL error | run the SQL verification command from step 0 |
| Angular shows proxy error | start API HTTPS profile on port 7277 first |
| Worker exits immediately | read the first `FTL` message and run Phase 15/16 database verifiers |
| Python health fails | run from `src\CustSearch.AI` and install `requirements-dev.txt` |
| Redis readiness is unhealthy | start Redis or set `OperationalPlatform__RedisEnabled=false` |

Do not run EF migrations or `EnsureCreated()` against `CustSearch_AI`; schema changes use the versioned
scripts under `database/09_Upgrade`.

For the Phase 16 certification gate, an administrator-provisioned SQL Server 2022 instance can be
checked without touching the existing database:

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI"
.\database\verify-phase16-sqlserver2022.ps1 -ServerInstance "<SQL2022_INSTANCE>"
```

The command rejects SQL Server versions other than major version 16 and uses a uniquely named,
isolated validation database that the verifier removes after completion.
