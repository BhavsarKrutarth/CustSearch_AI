# IIS, Angular and SignalR Deployment

Reviewed templates are available at `deployment/iis/api.web.config` and
`deployment/iis/admin.web.config`. Copy each as `web.config` only into its corresponding published
site root; do not place both in one IIS application.

1. Publish API/Worker with pinned .NET 8 runtime and build Angular production assets.
2. Host Angular as the IIS site root and API under an approved origin/path or reverse proxy.
3. Install URL Rewrite; direct SPA routes must fall back to `index.html`, excluding API/hubs/assets.
4. Enable WebSocket Protocol and proxy `/hubs/alerts` without buffering/connection-header damage.
5. Terminate HTTPS with an approved certificate; API production middleware emits HSTS.
6. Set `AllowedHosts` for deployed hostnames through configuration.
7. Enable `ReverseProxy__Enabled` only with exact immediate proxy IPs in `KnownProxies`.
8. Supply SQL/JWT/export/integration/CCTV/recognition/Redis secrets through the deployment secret store.
9. Install Worker as a service identity with least SQL/file permissions and recovery configuration.
10. Validate `/health/live`, `/health/ready`, Swagger policy, login, deep links, SignalR reconnect,
    export download and graceful Worker restart.

No IIS deployment was available in the audit environment, so this remains a Phase 17 blocked gate.
