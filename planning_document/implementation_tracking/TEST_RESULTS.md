# Test Results

Observed run: `CUSTSEARCH_SMOKE_20260825_001`

| Phase | Test | Expected | Actual | Result | Notes |
|---:|---|---|---|---|---|
| 1-17 | Release build | clean build | 0 warnings/errors | PASS | nine projects |
| 1-17 | Unit | all pass | 104/104 | PASS | executed |
| 1-17 | Integration/API | all pass | 230/230 | PASS | pinned .NET 8.0.424; executed |
| 1-17 | Angular | lint/unit/build pass | lint; 82/82; production build | PASS | existing admin-shell style budget warning (151 bytes) |
| 1-17 | Playwright | Chromium suite passes | 49/49 | PASS | after 1.55.1 patch |
| 13-17 | Python | Ruff and pytest pass | Ruff; 7/7 | PASS | Demo Mode |
| 16 | SQL | runner repeatable and schema valid | twice + verifier + DBCC | PASS | live database |
| 2/17 | Real auth | login/me/refresh/logout | expected statuses | PASS | refresh defect fixed/retested |
| 6/17 | Tenant isolation | cross-tenant denied | 404/404; own 200 | PASS | tenant and staff probes |
| 16 | SQL Server 2022 | execute on SQL 2022 | local engine is v17 | BLOCKED | exact environment absent |
| 16 | SQL 2022 version gate | reject non-v16 before schema work | local v17 rejected with exact reason | PASS | wrapper syntax and fail-fast path executed |
| 11/16 | Redis multi-node | prove backplane behavior | nodes ready 200; event 2 crossed B to A | PASS | Memurai 4.1.2 / Redis protocol 7.2.5 |
| 17 | IIS/WebSocket | deployed smoke | site absent | BLOCKED | external setup required |
| 16/17 | Manual API/Worker startup | no connection env required | both running; live/ready 200; heartbeat fresh | PASS | committed Windows-auth local settings |
| 17 | Angular to API | dev proxy reaches API/SQL auth | UI 200; invalid login 401 through proxy | PASS | actual processes |
| 13/17 | Python service | local health and .NET event target | health 200; protected API URL configured | PASS | no direct SQL access |
| 13/17 | Camera role upgrade | repeat-safe tenant-only CameraOperator grants | V1.16.1 twice; 13 tenant grants per smoke tenant; zero platform grants | PASS | live SQL observed |
| 13/17 | Dynamic camera access API | Office user sees own camera; isolation user sees none | counts 1/0; direct foreign camera request 404 | PASS | real JWT/API/SQL |
| 13/17 | Headed Chrome camera UAT | isolated Platform/Office/Random sessions | three independent browser contexts and screenshots | PASS | actual Google Chrome |
| 13/17 | Python dynamic source probe | authenticated allow-listed env resolver; no secret output | Ruff; pytest 10/10; missing runtime secret returns expected 422 | PASS | physical frame still blocked by credential |
| 13/17 | Physical RTSP frame | connected and frame received | authorized runtime stream credential absent | BLOCKED | exact recovery command in camera guide |
| 2/5/17 | Self change-password service/API | wrong current rejected; hash changed; old password/session rejected; new password accepted; audit safe | all positive/negative conditions observed | PASS | SQLite integration plus real HTTP in-process API; no live UAT password changed |
| 5/17 | Tenant-admin password reset | in-scope reset succeeds; hash verifies; audit excludes password; cross-store reset denied | expected success and tenant/store denial | PASS | focused integration tests included in 230/230 |
| 2/5/17 | Password UI | confirmation/policy validation; correct API routes; session clear and login redirect | 4 new Angular tests pass | PASS | full Angular 82/82 and lint PASS |
| 2/5/17 | Release build while manual API is running | compile without touching locked runtime binaries | isolated `OutDir` build 0 warnings/errors | PASS | exact pinned SDK `8.0.424`; user process PID 7560 not stopped |
