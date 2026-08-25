# Phase 18 Implementation Plan — Reviewable Retail Security

## Requirement extraction

- Keep anonymous visit/person-track operation as the baseline; recognition and customer identity are optional, legitimate associations only.
- Normalize signed, replay-protected AI observations. Python emits evidence signals, never a guilt/theft decision.
- Correlate pickup/put-back/exit/checkout observations with finalized paid retail invoices and configurable versioned rules.
- Create only reviewable `SuspectedUnpaidExit` incidents. `ConfirmedLoss` requires an authorized human, reason and immutable action history.
- Enforce TenantId and authorized StoreId at controller/service/stored-procedure boundaries; reject client TenantId on user APIs.
- Store opaque evidence object keys only, issue short-lived authorized access, and audit every evidence view/export.
- Deliver tenant/store-authorized SignalR/outbox events idempotently and provide bounded incident, timeline and report queries.
- Support shadow-mode rollout, false-positive reasons, evidence retention, escalation/re-correlation/stale-candidate Worker jobs and synthetic AI scenario tests.

## Dependency audit

Phases 5, 8, 11-13 and 15-17 provide stores/users, retail invoices, alerts/outbox/SignalR, integrations, camera zones/tracks, reports, settings/retention and repeatable quality gates. Phase 14 recognition remains optional. No existing Phase 18 incident implementation was found.

## Delivery order

1. V1.16.0 tenant/store schema, constraints, indexes, settings, permissions and stored procedures.
2. Domain/application risk and state-transition model with Dapper repositories.
3. HMAC/timestamp/nonce/body-hash protected internal ingestion and bounded APIs.
4. Incident/evidence/rules Angular pages, realtime/outbox and Worker maintenance.
5. SQL, unit, API authorization, browser and synthetic Python scenario validation; canonical synchronization and final audit.

## Safety decisions

- Optional watchlist storage is excluded.
- UI and Python will not contain authoritative risk thresholds or transition rules.
- Exit alone cannot alert; paid checkout and put-back reduce/suppress risk; occlusion and track gaps reduce confidence.
- External notification payloads contain authenticated deep links, not evidence or biometric/payment detail.

