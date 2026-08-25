# Database Change Log

| Date | Phase | Object | Change | Reason | Test | Result |
|---|---:|---|---|---|---|---|
| 2026-08-25 | 16 | V1.15 runner/verifier | Applied repeat-safe runner twice; added safe runner script | Validate actual database | verifier and constraints | PASS |
| 2026-08-25 | 1-16 | Smoke graph | Inserted deterministic two-tenant connected UAT graph | Connected positive/negative testing | seed twice, verifier, DBCC | PASS |
| 2026-08-26 | 16 | `dbo.WorkerHeartbeats` | Added current `WorkerType`/`IsReady` compatibility and safe legacy defaults | align divergent live schema without deleting rows | Phase 16 twice, verifier, heartbeat | PASS |
| 2026-08-26 | 15 | `dbo.ExportJobs` + report procedures | Applied selected repeat-safe V1.14 alongside legacy AIMain export objects | current Worker expected selected-chain objects | upgrade twice + verifier | PASS |

No database was dropped, recreated, truncated or downgraded. The live V1.16 ledger/schema was left
unchanged. Its SQL provenance was later found at `origin/AIMainBranch` commit `055b052`; that divergent
commit has not been merged or cherry-picked.
