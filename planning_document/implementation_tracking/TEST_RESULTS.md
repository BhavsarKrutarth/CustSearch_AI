# Test Results

Only executed checks are marked PASS.

| Phase | Test | Expected | Actual | Result | Notes |
|---:|---|---|---|---|---|
| Baseline | Git state | Existing user work known | Clean `AIMainBranch` before changes | PASS | No user changes overwritten |
| Baseline | SQL service/connection | Encrypted Windows-auth connection succeeds | `MSSQLSERVER` running; `CustSearch_AI` queried | PASS | Alias resolves to `DESKTOP-K08UK5F` |
| Baseline | DB ledger/catalog | Phase 1-14 and objects present | V1.0.0-V1.13.0 exactly once; 62 tables/47 SPs initially | PASS | Pre-change inventory retained |
| 15 | SQL upgrade rerun | Repeat-safe V1.14.0 | Executed repeatedly; one V1.14.0 ledger row | PASS | Live configured database |
| 15 | SQL verifier | Tables, constraints, indexes, 15 SPs valid | `Phase 15 report/export database verification passed` | PASS | Live configured database |
| 15 | Rollback-only SQL tests | Validation, audit, lifecycle and events work without residue | `Phase 15 rollback-only database tests passed` | PASS | `database/test-phase15.sql` |
| 15 | Worker authorization negative path | Invalid requester cannot export | Job failed with safe unauthorized error | PASS | Exposed and led to NULL-safe platform-scope fix |
| 15 | Real Worker export | Authorized platform job reaches completed state | Status 3, progress 100, CSV written | PASS | Synthetic data removed afterward |
| 15 | Artifact integrity | DB/file length and SHA-256 match | 182 bytes and matching SHA-256 | PASS | CSV retained headers for zero-row result |
| 15 | .NET Release build | 0 errors/warnings | 0 errors, 0 warnings | PASS | Correct .NET 8 SDK path used |
| 15 | Unit tests | All pass | 94/94 passed | PASS | Includes CSV/XLSX/PDF/path/formula tests |
| 15 | Integration tests | All pass | 214/214 passed | PASS | Includes seven report API authorization tests and three SignalR publisher/group tests |
| 15 | Report API authorization boundary | 200/400/401/403/404 and scope behavior | 7/7 focused tests passed | PASS | Includes client TenantId stripping at tenant controller boundary |
| 15 | Angular unit tests | All pass | 76/76 passed | PASS | 31 test files |
| 15 | Angular lint | No errors | All files pass | PASS | `npm run lint` |
| 15 | Angular production build | Build succeeds | Build succeeded | PASS | Existing SCSS budget warning: 61 bytes |
| 15 | DB-backed requester isolation | Different requester cannot read job | Rollback-only live SQL test returned zero rows | PASS | Tenant/store isolation is inherited from prior phase tests and current server-derived scope |
| 15 | SignalR durable relay components | Durable claim/complete and requester-only publish | SQL relay lifecycle PASS; publisher/group 3/3 PASS | PASS | Browser E2E retains REST fallback when no hub server is present |
| 15 | Playwright report workflow | Export/progress/download flow passes | Focused 2/2; full suite 47/47 | PASS | Tenant payload contains no TenantId |
| Regression | Python lint/tests | Pinned Ruff and pytest pass | Ruff PASS; pytest 7/7 | PASS | Installed pinned dev requirements locally |
| 15 | Canonical fresh install | Full database creates through V1.14.0 | PASS: 64 tables, 62 procedures, 15 versions, constraints clean | PASS | Disposable `CustSearch_AI_Phase15_Verify_20260825` removed afterward |
| 16 | Live SQL upgrade/rerun | Repeat-safe V1.15.0 | One ledger row; 66 tables, 69 procedures, 16 versions | PASS | `database/run-phase16.sql` |
| 16 | SQL verifier/constraints | Objects, permissions, defaults and constraints valid | Verifier PASS; DBCC clean | PASS | 33 platform defaults |
| 16 | SQL rollback tests | Precedence, invalid scope, safety lock, audit isolation, heartbeat and privacy retention | Rollback-only suite PASS; zero synthetic residue | PASS | Includes template erasure and old visitor deletion |
| 16 | .NET Release build | No warnings/errors | 0 warnings, 0 errors | PASS | .NET 8 SDK 8.0.424 |
| 16 | Unit tests | All pass | 98/98 | PASS | Adds setting security and artifact deletion |
| 16 | Integration tests | All pass | 219/219 | PASS | Adds five operations API auth/scope tests |
| 16 | Angular lint/tests/build | All green | lint PASS; 78/78; build PASS | PASS | Existing 61-byte SCSS warning only |
| 16 | Playwright full suite | Existing workflows remain green | 47/47 | PASS | Operations routes compile/guard; no regressions |
| 16 | Real API health boundary | Live/ready/auth work | 200 / 200 / 401 | PASS | Development host on loopback |
| 16 | Real Worker heartbeat | Running then stopped state persisted | Status 1 while running, status 2 after shutdown | PASS | Live `WorkerHeartbeats` row retained as operational evidence |
| 16 | Real expired artifact cleanup | Retryable expiry and durable event | Status 5, reference NULL, `ReportExportExpired`; exact test rows removed | PASS | Missing-file deletion proved idempotent |
| 17 | Integrated local quality gate | SQL, .NET, Python, Angular and browser gates all pass | Canonical 66/69/16 and cleanup; .NET 98+219; Python 7; Angular 78; Playwright 48 | PASS | `Invoke-QualityGates.ps1` |
| 17 | SignalR forced reconnect | Client reconnects, reports cursor and recovers missed event | New connection and `ReportReconnect`; recovered alert rendered | PASS | Focused 1/1 and full 48/48 |
| 17 | Swagger authorization metadata | Protected routes advertise Bearer; anonymous request rejected | 172 secured operations; login anonymous; health API 401 | PASS | Live development API |
| 17 | Deployment artifacts | Postman JSON and IIS XML valid; IIS file included in production output | Parse PASS; output file present | PASS | No secrets in Postman environment |
| 17 | Dependency audits | No high/critical vulnerabilities | Angular 0; E2E 0 after patched pins | PASS | `npm audit --audit-level=high` |
| 17 | Requested production platforms | SQL Server 2022 and enabled multi-node Redis validate | Local SQL is v17; Redis unavailable | PENDING | Deployment-environment gate; not marked PASS |
| 18 | Live V1.16.0 runner/verifier | Repeat-safe objects, safe defaults, no watchlist | 75 tables, 75 procedures, 17 versions, 13 permissions; DBCC clean | PASS | Live configured database |
| 18 | Ingestion rollback tests | Valid input, exact replay, changed replay and wrong store behave safely | Created once; duplicate returned; body mismatch/wrong store rejected; zero residue | PASS | AI observation did not auto-create an incident |
| 18 | Canonical fresh install | V1.16.0 installs from one canonical script and cleans up | 75/75/17, DBCC PASS, exact disposable DB dropped | PASS | `verify-canonical-fresh-install.ps1` |
