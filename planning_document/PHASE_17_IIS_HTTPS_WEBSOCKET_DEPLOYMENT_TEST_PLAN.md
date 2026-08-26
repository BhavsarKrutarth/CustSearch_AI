# Phase 17 — IIS, HTTPS and WebSocket Deployment Test Plan

## 1. Current status

| Area | Current evidence | Status |
|---|---|---|
| .NET Release build/tests | 104 unit + 225 integration tests | PASS |
| Angular lint/unit/production build | lint + 78 tests + production build | PASS |
| Playwright | 49 Chromium tests | PASS |
| Python | Ruff + 10 tests | PASS |
| Redis SignalR backplane | Two local API nodes; cross-node event delivered | PASS |
| IIS deployment | No test IIS site exists | PENDING |
| Public HTTPS certificate | No test binding/certificate exists | PENDING |
| SignalR over deployed WSS | Not executed through IIS | PENDING |

Phase 17 must remain `IN PROGRESS` until every required acceptance criterion in section 12 has
observed evidence. A valid local build does not prove IIS, TLS or WebSocket behavior.

## 2. Deployment objective

Deploy a production-shaped UAT environment where:

1. Users open one valid HTTPS origin, for example `https://<uat-host>`.
2. IIS serves Angular static files and SPA deep links.
3. The same public origin forwards `/api/*` and `/hubs/*` to ASP.NET Core.
4. SignalR upgrades to secure WebSocket (`wss://`) and preserves authenticated tenant/store groups.
5. The API and Worker use least-privilege service identities and runtime secrets.
6. SQL remains authoritative; SignalR reconnect recovers missed state through REST.
7. Logs and IIS tracing never retain bearer tokens, refresh cookies or secret values.

This is a UAT deployment plan. It does not authorize an automatic Production deployment or PR merge.

## 3. Required decisions and inputs

Record these values in the sanitized test report before deployment. Do not commit their secret values.

| Input | Example format | Owner |
|---|---|---|
| UAT public FQDN | `custsearch-uat.example.com` | DNS/admin |
| IIS server | Windows Server 2022/2025 test host | Infrastructure |
| TLS certificate | LocalMachine certificate with matching SAN and trusted chain | Security/admin |
| API internal binding | `http://127.0.0.1:<internal-port>` | Infrastructure |
| SQL Server | Approved reachable database instance | DBA |
| API application-pool identity | Domain service account or gMSA | Security/admin |
| Worker service identity | Separate domain service account or gMSA | Security/admin |
| Redis mode | Disabled for one API node; enabled only for a tested multi-node topology | Architecture |
| Artifact root | Versioned, non-source deployment directory | Release owner |
| Evidence run ID | `CUSTSEARCH_IIS_UAT_<yyyymmdd>_<nnn>` | QA |

Do not use a self-signed/untrusted certificate for the final Phase 17 HTTPS gate. A private enterprise
CA is acceptable only when the test clients trust its complete chain.

## 4. Recommended IIS topology

Use one browser-visible origin because Angular currently calls relative `/api` and `/hubs` paths.

```text
Chrome
  -> https://<uat-host>:443
  -> IIS public Angular site
       /assets, /* SPA     -> Angular files
       /api/*              -> ARR/URL Rewrite -> http://127.0.0.1:<api-port>/api/*
       /hubs/*             -> ARR/WebSocket   -> http://127.0.0.1:<api-port>/hubs/*
       /health/*           -> internal monitoring by default; public proxy only if approved
  -> IIS internal API site (loopback binding only)
       -> ASP.NET Core Module V2 / CustSearch.API
       -> SQL Server
       -> Redis only when scale-out is enabled
  -> CustSearch.Worker Windows Service
       -> SQL Server / export storage / configured integrations
```

Security properties:

- The API internal port is bound to loopback and blocked from external access.
- IIS terminates TLS and sends `X-Forwarded-Proto: https`.
- `ReverseProxy:Enabled=true` and `KnownProxies` contains only the immediate proxy address.
- ARR preserves the public host, and `AllowedHosts` contains only approved hostnames.
- `/hubs/alerts` must preserve the WebSocket upgrade and must not buffer the connection.

### Mandatory template gap

`deployment/iis/admin.web.config` currently implements SPA fallback and explicitly excludes `/api`
and `/hubs`, but it does not contain ARR reverse-proxy rules. Before deployment, either:

1. add reviewed same-origin `/api` and `/hubs` proxy rules to the deploy-time IIS configuration; or
2. place an approved reverse proxy/gateway in front of separate Angular/API IIS sites.

Do not deploy the current Angular template alone and claim REST/SignalR connectivity.

The current SPA fallback also does not exclude `/health`. The recommended design keeps health probes
on the internal API binding for the load balancer/monitor. If public health is required, add an
explicit `/health/*` proxy rule before SPA fallback and confirm that its response exposes no sensitive
dependency detail.

## 5. Work package A — host and module preparation

Administrator actions:

1. Patch Windows and reboot if required.
2. Install IIS Web Server, Static Content, Default Document, Request Filtering, HTTP Logging and
   **WebSocket Protocol**.
3. Install the pinned .NET 8 Hosting Bundle matching the repository support policy.
4. Install IIS URL Rewrite and Application Request Routing (ARR); enable ARR proxy support.
5. Install the trusted TLS certificate under `Cert:\LocalMachine\My`.
6. Create DNS for the UAT FQDN and verify it resolves from the browser test host.
7. Create least-privilege API/Worker identities. Prefer gMSA so passwords do not enter commands/files.
8. Grant SQL permissions only to required application objects; do not use a DBA/sysadmin identity.

Preflight evidence commands (run in elevated PowerShell):

```powershell
Get-WindowsFeature Web-Server,Web-WebSockets,Web-Static-Content
# On a Windows client test host, use Get-WindowsOptionalFeature instead.
Import-Module WebAdministration
Get-WebGlobalModule | Where-Object Name -Match 'Rewrite|WebSocket|AspNetCoreModuleV2'
Get-ChildItem Cert:\LocalMachine\My | Select-Object Subject, Thumbprint, NotBefore, NotAfter
Resolve-DnsName '<uat-host>'
```

Gate A passes only when all required features/modules are enabled, the certificate is valid for the
FQDN, and the service identities are available.

## 6. Work package B — repeatable release artifacts

Use pinned repository versions and an empty versioned staging directory. Never publish directly over
a running site.

```powershell
Set-Location 'D:\Project\AdminCore\CustSearch_AI\CustSearch_AI'
$dotnet8 = "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"

& $dotnet8 restore .\CustSearch_AI.sln
& $dotnet8 build .\CustSearch_AI.sln -c Release --no-restore
& $dotnet8 test .\CustSearch_AI.sln -c Release --no-build

& $dotnet8 publish .\src\CustSearch.API\CustSearch.API.csproj `
  -c Release --no-build --self-contained false -o '<staging>\api'
& $dotnet8 publish .\src\CustSearch.Worker\CustSearch.Worker.csproj `
  -c Release --no-build --self-contained false -o '<staging>\worker'

Set-Location .\src\CustSearch.Admin
npm ci
npm run lint
npm run test:ci
npm run build:production
```

Then:

1. Copy `deployment/iis/api.web.config` as `<staging>\api\web.config`.
2. Copy the Angular browser output into `<staging>\admin`.
3. Apply the reviewed deploy-time proxy configuration to `<staging>\admin\web.config`.
4. Produce SHA-256 hashes for every staged file and record the source commit SHA.
5. Scan the staging directory for connection strings, signing keys, camera credentials and tokens.

Gate B passes with clean builds/tests, immutable artifact hashes and zero committed/staged secrets.

## 7. Work package C — runtime configuration and permissions

Set environment-specific values through the approved server secret/configuration mechanism, not Git:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__CustSearchDatabase`
- `Jwt__SigningKey`
- `ReportsExports__DownloadSigningKey`
- `AllowedHosts=<uat-host>`
- `ReverseProxy__Enabled=true`
- `ReverseProxy__KnownProxies__0=127.0.0.1`
- Redis connection/settings only when multiple API nodes are deployed
- integration/CCTV/recognition secret references and keys only when those modules are enabled

Required filesystem access:

| Identity | Read/execute | Modify | Must not access |
|---|---|---|---|
| Public Angular app pool | Angular release | IIS logs only as required | API secrets, exports, source |
| API app pool | API release | API logs and approved export path | source repo, camera raw credentials |
| Worker service | Worker release | Worker logs and approved export path | Angular files, interactive profiles |

Additional rules:

- Keep API/Worker identities separate unless an approved threat review says otherwise.
- Do not enable `CctvRuntime:DemoMode` in Production environment.
- Do not grant broad write access to the IIS site root.
- Make export downloads requester-bound and keep the storage directory outside the public Angular root.
- Configure Worker recovery, delayed restart and graceful stop; readiness requires a fresh heartbeat.

Gate C passes when startup validation succeeds and secrets cannot be read through static files, API
responses, process arguments or ordinary application logs.

## 8. Work package D — IIS, HTTPS and Worker deployment

1. Back up the current versioned release and export the existing IIS configuration.
2. Create separate Angular and internal API application pools with **No Managed Code**.
3. Disable 32-bit mode unless a reviewed dependency requires it.
4. Configure the API pool for `AlwaysRunning`; enable site/application preload where supported.
5. Bind the internal API site only to `127.0.0.1:<api-port>`.
6. Bind the public Angular site to HTTPS 443 with SNI and the approved certificate.
7. Redirect public HTTP to HTTPS without redirect loops.
8. Configure ARR rules for `/api/*` and `/hubs/*` before the SPA fallback rule.
9. Enable WebSocket on both the public proxy site and API site.
10. Configure HSTS and approved security headers on the public Angular IIS site; API HSTS alone does
    not protect static Angular responses.
11. Start the Worker as an automatic Windows Service under its approved identity.
12. Start API, then Worker, then the public Angular site.
13. Keep the previous release intact until all acceptance tests pass.

Important ordering for public IIS rewrite rules:

```text
1. /api/*  -> API /api/*
2. /hubs/* -> API /hubs/* with WebSocket upgrade
3. existing file/directory -> serve normally
4. remaining route -> /index.html
```

## 9. Work package E — HTTPS and application smoke tests

Record command, UTC start/end, exit code, response status and sanitized evidence for every row.

| Test | Expected |
|---|---|
| `http://<uat-host>` | Single 301/308 redirect to the exact HTTPS host |
| `https://<uat-host>` | Trusted certificate; no browser warning or mixed content |
| TLS inspection | Valid hostname, chain and expiry; approved TLS versions/ciphers |
| Internal API `/health/live` and `/health/ready` | 200; ready includes SQL/Worker/Redis policy |
| Public `/health/*` | Not exposed by default, or explicitly proxied with minimal reviewed output |
| `/swagger` in Production | 404/not exposed |
| Angular root | 200 with hashed production assets |
| Direct SPA deep link | 200/index shell, then auth guard/login—not IIS 404 |
| Static missing asset | 404, not rewritten to misleading HTML where avoidable |
| Login, `/api/auth/me`, refresh, logout | Expected 200/204; cookie Secure/HttpOnly/SameSite/Path correct |
| Invalid login | 401 without sensitive detail |
| Cross-tenant request | 403/404 according to contract |
| Host-header attack | Rejected; unapproved host is not served as trusted origin |
| Security headers | HSTS on HTTPS after validation, nosniff/referrer policy; no server leakage |

Use a browser and command-line client. Do not use `-k`/certificate bypass for the final HTTPS PASS.

## 10. Work package F — deployed SignalR/WebSocket tests

### F1. Transport proof

1. Login as a tenant user with `Alerts.View`.
2. Open the notification center.
3. In Chrome DevTools Network, select WS and confirm `/hubs/alerts` returns HTTP `101 Switching
   Protocols` and uses `wss://<uat-host>/hubs/alerts`.
4. Capture protocol/connection evidence without copying the `access_token` query value.
5. Treat Long Polling/SSE fallback as a failure for this specific WebSocket gate.

### F2. Authentication and authorization

| Test | Expected |
|---|---|
| No token | Hub negotiate/connect returns 401 |
| Invalid/expired token | 401; no connection/group assignment |
| Platform-only identity | Tenant hub denied |
| Tenant A connection | Receives Tenant A/authorized-store events only |
| Tenant B event | Never delivered to Tenant A connection |
| Unauthorized StoreId supplied by browser | Ignored/rejected; groups remain server-derived |
| Disabled user/session | Existing/reconnect request rejected after authoritative revalidation |

### F3. Delivery, reconnect and recovery

1. Connect Tenant A and wait for `RealtimeReady` contract version 1.
2. Create a unique alert through the authenticated REST API.
3. Require exactly one matching `AlertEvent` within the agreed UAT threshold.
4. Recycle the API pool or interrupt the client network briefly.
5. Observe Angular states `Reconnecting` then `Connected` within the configured retry window.
6. During the interruption create another durable alert.
7. After reconnect, require `ReportReconnect` plus REST recovery to return the missed event once.
8. Verify duplicate event IDs do not duplicate UI records.
9. Run one asynchronous export and observe progress/completion SignalR events; download remains
   requester-bound through REST.

For two API nodes, reuse `src/CustSearch.Admin/scripts/redis-backplane-smoke.mjs` with UAT node URLs
and a short-lived test token. Redis backplane is not required for a single API node.

### F4. WebSocket security/logging

- Remove `cs-uri-query` from IIS W3C logging or apply an approved rule that prevents hub query tokens
  from being persisted.
- Ensure Failed Request Tracing and proxy logs do not capture SignalR `access_token` values.
- Verify API/Serilog logs contain correlation and connection metadata but no bearer token/cookie.
- Confirm maximum message size, rate limits and server-derived groups remain active behind IIS.

Gate F passes only with observed WSS 101, valid delivery, denied unauthorized cases, successful
reconnect/recovery and zero token leakage.

## 11. Work package G — resilience and rollback tests

Execute during an approved UAT window:

| Failure | Expected behavior |
|---|---|
| API app-pool recycle | Client reconnects and recovers authoritative events |
| Worker restart | Lease/heartbeat recovers; no duplicate processing |
| SQL unavailable | `/health/live` remains live; `/health/ready` fails closed |
| Redis unavailable when disabled | No effect |
| Redis unavailable when enabled | Readiness fails; behavior documented, no false healthy state |
| Expired/removed certificate simulation in isolated binding | Deployment monitor alerts; no silent HTTP downgrade |
| Previous artifact rollback | Prior version starts without database downgrade |

Rollback sequence:

1. Disable the new public binding/site.
2. Stop Worker and API pool.
3. Restore the prior versioned directories and exported IIS configuration.
4. Restore the previous certificate binding if it changed.
5. Start API, Worker and Angular; rerun health/login smoke.
6. Do not roll back/drop the database—Phase 17 introduces no database schema migration.

## 12. Definition of done

Phase 17 IIS/HTTPS/WebSocket gate is `PASS` only when all conditions are true:

- [ ] Windows/IIS prerequisites and .NET 8 Hosting Bundle verified.
- [ ] Reviewed same-origin API/Hub proxy configuration deployed.
- [ ] Valid trusted HTTPS certificate; HTTP redirects; no mixed content.
- [ ] Angular root, assets and direct deep links work through IIS.
- [ ] API live/ready are 200 with Worker/SQL and configured Redis healthy.
- [ ] Production Swagger is unavailable.
- [ ] Login, refresh cookie, logout and representative CRUD work through public HTTPS.
- [ ] WebSocket transport shows WSS + HTTP 101, not fallback.
- [ ] Missing/invalid/expired/incorrect-scope hub connections are denied.
- [ ] Tenant/store SignalR isolation is observed with at least two tenants/stores.
- [ ] Reconnect and REST recovery return missed events exactly once.
- [ ] Export progress/completion is delivered; download authorization remains requester-bound.
- [ ] IIS/proxy/API logs contain no query bearer tokens, cookies or secrets.
- [ ] App-pool/Worker recycle and dependency readiness tests behave as designed.
- [ ] Rollback is rehearsed successfully without database destruction.
- [ ] No unresolved Critical/High deployment-security finding remains.
- [ ] Sanitized evidence and observed results are added to `PHASE_17_TEST_REPORT.md` and tracking files.

If any required row is unexecuted, record `BLOCKED` with the missing environment/admin action. Never
convert a planned/expected result into `PASS`.

## 13. Evidence and handoff format

Create a local evidence folder outside the public site, for example:

```text
artifacts/phase17-iis/<run-id>/
  host-preflight.txt
  artifact-hashes.txt
  iis-bindings-sanitized.txt
  tls-results.txt
  health-results.txt
  auth-results.txt
  websocket-101.png
  websocket-negative-results.txt
  reconnect-recovery-results.txt
  log-secret-scan.txt
  rollback-results.txt
```

Do not commit raw tokens, cookies, private certificate material, connection strings, internal account
passwords or unsanitized IIS logs. Commit only the sanitized summary and update:

- `planning_document/PHASE_17_TEST_REPORT.md`
- `planning_document/ALL_PHASE_EXECUTION_CHECKPOINT.md`
- `planning_document/implementation_tracking/TEST_RESULTS.md`
- `planning_document/implementation_tracking/OPEN_ISSUES.md`
- `planning_document/implementation_tracking/SESSION_HANDOFF.md`

## 14. First exact action

Provision or identify the IIS UAT host, public FQDN, trusted certificate and approved service
identities. Then run Work Package A preflight. No application deployment should begin until Gate A is
green and the same-origin `/api` + `/hubs` proxy choice is approved.
