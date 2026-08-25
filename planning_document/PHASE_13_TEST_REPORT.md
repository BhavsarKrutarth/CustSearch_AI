# Phase 13 — Cameras, Python CCTV & Tracking Test Report

**Result:** Green  
**Branch:** `phase13-cctv-tracking`
**Validated implementation commit:** `4572f155afb3fa324cfc42574049fec4c748d1b1`  
**Tested canonical persistence head:** `52a719ab2c3e40c56f7797f483eac766c8447912`  
**Phase 13 workflow:** run `32798417105`, job `97654376399`  
**Canonical SQL blob:** `4f048951bfe39e4520e6e8df093f876ff763c757`

Phase 12 was first validated from `AIMainBranch` commit `456c840ae9705cbd560d8213f924442920b4acd5`. Phase 13 then passed the complete Phase 5–13 application regression plus the V1.12.0 SQL Server upgrade, standalone installer/verifier, and canonical fresh-install gates. The separately triggered Phase 6–12 workflows and Phase 9 SQL-only workflow were also green on the validated implementation commit.

## Required gates

| Gate | Status |
|---|---:|
| Phase 12 authoritative baseline | Green |
| .NET Release build | Green — 0 warnings, 0 errors |
| .NET unit | Green — 86/86 |
| .NET integration/API | Green — 188/188 |
| Angular lint/unit/production build | Green — 70/70 tests |
| Playwright full regression | Green — 38/38 |
| Python Ruff and pytest | Green — 7/7 tests |
| SQL V1.12 upgrade twice | Green — SQL Server 2022 |
| Standalone runner twice + verifier | Green — SQL Server 2022 |
| Prospective canonical fresh install | Green — SQL Server 2022 |
| Phase 6–12 workflows + Phase 9 SQL-only | Green |
| Camera tenant/store isolation | Green |
| Zone validation/versioning | Green |
| Python and .NET service authentication | Green |
| Anonymous lifecycle and camera handoff | Green |
| Demo determinism/Production guard | Green |
| No automatic identity recognition | Green |

## Security and privacy evidence

- RTSP URLs and credentials are represented only by protected configuration references; API responses expose a masked hint.
- Python uses the authenticated service-to-service event API and has no SQL Server dependency.
- Service credentials are tenant/store scoped, exact request bodies are HMAC authenticated, and event/idempotency keys suppress replay.
- Phase 13 accepts anonymous tracking metadata only. Identity, biometric, embedding, raw-frame, customer and staff identity fields are rejected at the CCTV ingress boundary.
- Customer/staff association remains an explicit authorized application action and tracking never claims verified real-world identity.
- Demo Mode is deterministic in CI, visibly labeled in Angular, and rejected by production configuration gates.

## Database evidence

`database/09_Upgrade/V1.12.0_Phase13_CamerasTracking.sql`, `database/run-phase13.sql`, and `database/verify-phase13.sql` passed against SQL Server 2022. The workflow then persisted the exact tested V1.12.0 content into `database/CustSearchAi.sql`; its Git blob is `4f048951bfe39e4520e6e8df093f876ff763c757` and the verifier enforces exactly one `DatabaseVersions` row for V1.12.0.
