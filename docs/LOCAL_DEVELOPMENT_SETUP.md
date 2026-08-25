# Local Development Setup

## 1. Tooling

Use the repository-pinned .NET/Node/Angular/Python dependency versions. Install `sqlcmd` and ensure
the target database is reachable with Windows Integrated Security. Redis and physical cameras are
optional for ordinary local work; CCTV Demo Mode keeps development deterministic.

## 2. Environment variables

```powershell
$env:ConnectionStrings__CustSearchDatabase = 'Server=KRUTARTH-BHAVSA;Database=CustSearch_AI;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:Jwt__SigningKey = '<local-secret-at-least-32-bytes>'
$env:ReportsExports__DownloadSigningKey = '<different-local-secret-at-least-32-bytes>'
```

For this workstation, API and Worker also contain this Windows-authenticated Development connection,
so Visual Studio or `dotnet run` works without setting the connection variable. Set the variable when
targeting any other server; environment configuration overrides `appsettings`.

Add only the service secrets needed by the flow under test. Never place them in `appsettings.json`.

## 3. Verify SQL before starting

```powershell
sqlcmd -S 'KRUTARTH-BHAVSA' -d 'CustSearch_AI' -E -C -b -Q "SELECT @@SERVERNAME,DB_NAME(),SUSER_SNAME();"
./database/run-phase16.ps1 -ServerInstance 'KRUTARTH-BHAVSA' -ValidateIdempotency
```

Read `planning_document/ALL_PHASE_EXECUTION_CHECKPOINT.md` first: the current live database is ahead
of the Phase 16 canonical source and must not be downgraded/recreated.

## 4. Restore/build/test

Use the commands in the root README. A test is successful only when its process exits zero and the
reported pass/fail counts are observed. SQL Server 2022 and Redis multi-node checks require separate
approved environments on this workstation.

## 5. Start services

1. Start API with the `https` profile.
2. Start Angular with `npm start`.
3. Start Worker when exercising outbox/export/retention flows.
4. Start Python with Demo Mode and a local service key for CCTV fixtures.

Swagger is available only in Development. Health endpoints are `/health/live` and `/health/ready`.

## 6. Optional reverse proxy

Forwarded headers are disabled by default. For a local approved proxy, set:

```powershell
$env:ReverseProxy__Enabled = 'true'
$env:ReverseProxy__KnownProxies__0 = '127.0.0.1'
```

Never enable forwarded headers without an allowlisted immediate proxy, because client IP is used
for audit and rate limiting.
