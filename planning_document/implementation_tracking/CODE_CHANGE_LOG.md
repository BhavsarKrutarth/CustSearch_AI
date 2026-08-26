# Code Change Log

| Phase | File/group | Change | Reason | Testing |
|---:|---|---|---|---|
| 16 | export/test infrastructure | released file handles and disabled disposable SQLite pooling | eliminate repeatable file-lock failures | full .NET PASS |
| 16 | readiness/tests | added unavailable SQL and Redis readiness coverage | prove fail-closed readiness | integration PASS |
| 17 | `AuthenticationService.cs` | placed atomic refresh rotation inside SQL retry execution strategy | real SQL refresh returned 500 | auth 28/28 and live flow PASS |
| 17 | API/Worker configuration | removed machine connection and added allowlisted proxy/HSTS behavior | safe portable deployment defaults | build/integration PASS |
| 17 | major infrastructure/worker services | added business/security/tenant boundary comments | maintainable project-wise handoff | build PASS |
| 17 | Playwright manifests | patched 1.55.0 to 1.55.1 | browser certificate advisory | audit clean; E2E 49/49 |
| 17 | docs and SQL smoke tools | added catalogs, runbooks, deterministic seed/verifier/cleanup | reproducible audit and handoff | live SQL/API PASS |
| 16/17 | API/Worker local settings | added `KRUTARTH-BHAVSA/CustSearch_AI` Windows-auth local default | manual run without per-shell setup | API/Worker live startup PASS |
| 16 | Phase 16 SQL | reconciled legacy Worker heartbeat columns/defaults without deleting history | current Worker/readiness schema compatibility | runner twice/verifier/DBCC PASS |
| 16 | Infrastructure/Worker DI | removed unsafe implicit EF transaction replay; added fail-closed background identity/metrics | real Worker startup failed before processing | 329 tests + live startup PASS |
| 17 | Angular/Python local routes | verified Angular API proxy; documented/defaulted Python protected .NET event URL | complete local service graph | Angular proxy 401; Python health 200 |
| 17 | `docs/PROJECT_WISE_MANUAL_RUN_GUIDE.md` | added terminal-by-terminal API/Worker/Angular/Python commands and checks | enable repeatable manual sub-project startup | commands matched observed local startup |
| 16 | Redis backplane smoke script | added executable node-B to node-A SignalR delivery proof | validate actual scale-out behavior | two nodes ready 200; event 2 PASS |
| 16 | SQL Server 2022 verifier | added exact-major-version gate and isolated canonical delegation | prevent SQL 2025/compatibility mode from being misreported as SQL 2022 | local v17 rejected as expected |
| 13/17 | SQL/Infrastructure role provisioning | completed CameraOperator grants, dashboard entry permission and live enum `tinyint` mappings | repair real SQL/API UAT defects without widening tenant scope | build; integration 225/225; SQL twice |
| 13/17 | Python camera source boundary | added authenticated dynamic `env:` resolver and bounded one-frame RTSP probe | support server-side cameras without static IP/credentials | Ruff PASS; pytest 10/10 |
| 13/17 | headed Chrome UAT script/docs | parameterized all runtime identities/camera names and documented dynamic secret setup | reproducible non-hard-coded manual validation | three-context Chrome PASS |
| 17 | IIS/HTTPS/WebSocket deployment test plan | added host, ARR, TLS, WSS 101, authorization, recovery, logging, evidence and rollback gates | make the remaining environment validation executable and evidence-based | planning review only; deployment still BLOCKED |
