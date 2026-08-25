# Test Results

Observed run: `CUSTSEARCH_SMOKE_20260825_001`

| Phase | Test | Expected | Actual | Result | Notes |
|---:|---|---|---|---|---|
| 1-17 | Release build | clean build | 0 warnings/errors | PASS | nine projects |
| 1-17 | Unit | all pass | 104/104 | PASS | executed |
| 1-17 | Integration/API | all pass | 225/225 | PASS | executed |
| 1-17 | Angular | lint/unit/build pass | lint; 78/78; build | PASS | existing style budget warning |
| 1-17 | Playwright | Chromium suite passes | 49/49 | PASS | after 1.55.1 patch |
| 13-17 | Python | Ruff and pytest pass | Ruff; 7/7 | PASS | Demo Mode |
| 16 | SQL | runner repeatable and schema valid | twice + verifier + DBCC | PASS | live database |
| 2/17 | Real auth | login/me/refresh/logout | expected statuses | PASS | refresh defect fixed/retested |
| 6/17 | Tenant isolation | cross-tenant denied | 404/404; own 200 | PASS | tenant and staff probes |
| 16 | SQL Server 2022 | execute on SQL 2022 | local engine is v17 | BLOCKED | exact environment absent |
| 11/16 | Redis multi-node | prove backplane behavior | topology absent | BLOCKED | external setup required |
| 17 | IIS/WebSocket | deployed smoke | site absent | BLOCKED | external setup required |
