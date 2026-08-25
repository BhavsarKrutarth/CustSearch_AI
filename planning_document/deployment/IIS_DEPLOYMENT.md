# IIS Angular SPA, API and WebSocket Deployment

## Required IIS features

- IIS Static Content, HTTP Logging, Request Filtering and WebSocket Protocol.
- URL Rewrite for the Angular SPA fallback.
- ASP.NET Core Hosting Bundle 8.x for the API when IIS hosts it in-process/out-of-process.
- ARR only when IIS is the reverse proxy to a separately hosted API/Worker topology.

## Recommended topology

Serve Angular and the API under one HTTPS origin. Route `/api`, `/hubs`, `/health` and (non-production only) `/swagger` to `CustSearch.API`; serve all physical static assets from the Angular `browser` output. The committed `public/web.config` is copied into the production Angular output and rewrites only non-file, non-directory, non-backend routes to `index.html`.

The API/Hub reverse-proxy rule must execute before the SPA fallback. Never rewrite `/hubs/*` to `index.html`. Enable WebSocket proxying, preserve `Host`/forwarded headers only from trusted proxies, and validate `wss://<host>/hubs/alerts` plus `/hubs/reports` through the load balancer.

## Publish

```powershell
& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" publish src/CustSearch.API/CustSearch.API.csproj -c Release -o artifacts/api
& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" publish src/CustSearch.Worker/CustSearch.Worker.csproj -c Release -o artifacts/worker

Set-Location src/CustSearch.Admin
npm ci
npm run build:production
```

Deploy `src/CustSearch.Admin/dist/custsearch-admin/browser/*` to the static site root. Run `CustSearch.Worker` as a Windows Service or equivalent supervised process; do not host it as an IIS request application.

## Validation

1. Browse a deep Angular URL directly and confirm HTTP 200 with the SPA shell.
2. Confirm `/api/unknown` returns an API 404, not `index.html`.
3. Confirm `/health/live` and `/health/ready` return 200.
4. Confirm an anonymous platform operational call returns 401.
5. Authenticate, connect both hubs over WSS, interrupt/recover the network, and confirm reconnect plus REST state recovery.
6. With more than one API node, enable `Redis__Enabled=true` and `Redis__SignalRBackplaneEnabled=true`; validate cross-node delivery before adding traffic.
