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
