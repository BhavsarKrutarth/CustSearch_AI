# Phase 15 — Reports & Async Exports Implementation Plan

**Branch:** `phase15-reports-exports`
**Validated baseline:** Phase 14 final head `b73704a2d474fe07eee6aef26bbc461ddd6774be`

## Delivery design

- Publish one permission-separated catalog for platform and tenant reports.
- Execute report-heavy reads only through allowlisted Dapper stored procedures.
- Derive tenant identity and authorized stores from the authenticated server context before aggregation.
- Persist bounded, leased asynchronous export jobs with an authorization snapshot that the worker revalidates.
- Generate CSV, Open XML Excel and PDF files outside browser memory.
- Use Phase 11 outbox/SignalR event names `export.created`, `export.progress`, `export.completed` and `export.failed`; polling remains authoritative after reconnect.
- Bind download access to requester, tenant, job and a short expiry. Never accept a browser file path.
- Expire files safely and audit queue, retry, ticket, download, completion, failure and retention actions.
- Add Angular report filters, paged results, format selection, queue/progress, retry and authorized download.
- Apply V1.14.0 with standalone repeat-safe runner/verifier and persist the tested canonical SQL only after every gate is green.

## Completion gates

.NET Release build/tests, Angular lint/unit/build, full Playwright, Python Ruff/pytest, tenant/store/IDOR/format/progress/retry/expiry checks, Phase 5–15 regression, V1.14 upgrade twice, standalone runner twice, verifier and canonical fresh install must all pass.

