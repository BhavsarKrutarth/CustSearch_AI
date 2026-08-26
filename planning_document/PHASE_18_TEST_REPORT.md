# Phase 18 Test Report — Retail Security

Date: 2026-08-27 (Asia/Calcutta)
Branch: `phase18-retail-security`
Overall phase status: **NOT COMPLETED — operational acceptance evidence is still required**

## Verified evidence

| Area | Command / observation | Result |
|---|---|---|
| Database baseline | queried `DatabaseVersions` on `KRUTARTH-BHAVSA/CustSearch_AI` | `V1.16.0`, `V1.16.1`, `V1.16.2`, then applied `V1.17.0` |
| SQL upgrade | `sqlcmd ... -i run-phase18.sql`, executed twice | both runs succeeded; schema verification succeeded; zero retained test incidents |
| SQL synthetic security flow | `sqlcmd ... -i test-phase18.sql` | succeeded inside rollback: wrong-tenant camera rejected, observation duplicate detected, two rule versions created, Candidate → UnderReview → FalsePositive audited |
| .NET unit tests | `dotnet test tests/CustSearch.UnitTests/... --no-restore` | 117 passed, 0 failed |
| Existing .NET integration regression | `dotnet test CustSearch_AI.sln --no-restore` | 234 integration tests passed, 0 failed on the recorded run |
| Python synthetic AI tests | `python -m pytest` from `src/CustSearch.AI` | 22 passed, 0 failed; no RTSP/camera access |
| Angular | `npm run build:production` | production bundle succeeded; one pre-existing admin-shell stylesheet budget warning |
| Terminology scan | restricted-label regex over `src`, `database`, and executable tests | no prohibited automatic person labels found in production/test code |
| Source hygiene | `git diff --check` | no whitespace errors; line-ending notices only |

## Phase 18 coverage

- Versioned server risk rules cover exit, probable possession, checkout absence, payment mismatch,
  product value, optional RFID/EAS, occlusion, camera gap, crowd, group handoff and re-entry.
- Synthetic unit scenarios cover pickup/put-back, paid checkout/exit, no-payment exit, group handoff,
  staff restocking, degraded visibility, occlusion/crowd/camera gap and duplicate re-entry reduction.
- AI emits normalized anonymous observations and has no final-loss decision or identity field.
- AI service tests cover expired authentication and a correctly signed wrong-tenant request. SQL covers
  wrong-tenant camera ownership, nonce/idempotency constraints and duplicate delivery behavior.
- Incident transitions are server-authoritative. Human confirmation needs its distinct permission and a
  reason. Every transition is written to the incident action timeline and central audit log.
- Incident, timeline, assignment, evidence, settings, rule and report lookups derive tenant identity from
  the authenticated session and constrain every ID to authorized stores.
- Evidence tickets are tenant/user/incident/evidence/purpose bound and expire. Local evidence objects use
  AES-GCM integrity protection, canonical path enforcement, audited access/export and retention cleanup.
  False-positive review shortens remaining evidence retention to at most seven days.
- SignalR sends only to the server-derived store group. Supported event names are created, updated,
  critical, acknowledged and resolved.
- In-app notification deliveries use a unique idempotency key. Optional external-channel rows contain only
  an authenticated incident-detail link; CCTV evidence is not attached.
- Worker maintenance covers in-app delivery, escalation queueing, encrypted-object cleanup, POS
  re-correlation, stale-candidate expiry and logged security metrics. Each operation uses state predicates
  or unique keys for repeat safety.
- Production startup requires evidence signing/encryption secrets. Existing host hardening keeps trusted
  forwarded headers opt-in, HSTS outside development, a restrictive AllowedHosts value, same-origin-only
  browser access, request size limits and service-key rate partitions.

## Not yet proven / release blockers

1. Shadow Mode has no retained incidents in the checked database, so precision, false-positive rate and
   drift metrics are **not measured**.
2. No human/POS truth comparison has been performed for a real store dataset.
3. IIS/reverse-proxy HTTPS, forwarded-client-IP and authenticated WebSocket deployment smoke remain
   unexecuted from Phase 17.
4. Secret rotation and optional Push/SMS/WhatsApp/Email provider deliveries were not exercised with real
   provider credentials. External escalation must remain disabled.
5. Real encrypted clips/snapshots were not staged, viewed or exported end-to-end. Only token, path,
   authorization, metadata and cleanup controls were tested locally.
6. Physical live-camera testing was intentionally not performed.

Therefore this report does **not** claim Phase 18 PASS or Completed. The code/database baseline is locally
verified, but the source-of-truth completion gate requires measured Shadow Mode acceptance and production
security/deployment evidence.
