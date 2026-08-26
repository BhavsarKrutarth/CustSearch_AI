# Phase 18 Implementation Plan — Reviewable Retail Security

Prepared: 2026-08-27 (Asia/Calcutta)
Branch: `phase18-retail-security`

## Safety boundary

The module creates reviewable `Suspected Unpaid Exit` security candidates. AI observations are risk
signals, not proof. No automated flow may label a Customer, Visitor or PersonTrack as a thief,
shoplifter or criminal. Only a user with `Security.Incidents.ConfirmLoss` and store access may record
`ConfirmedLoss`. Phase 14 recognition is optional and no external identity database or biometric
watchlist is used.

## Dependency validation

| Dependency | Evidence | Status |
|---|---|---|
| Phase 5 tenant/store/user scope | current services, tables and regression | Ready |
| Phase 8 factual POS/invoices | products, invoices, items, payments and reports | Ready |
| Phase 11 alerts/SignalR/outbox | authorized hub groups and durable outbox | Ready |
| Phase 12 integrations | HMAC/replay/body-limit patterns | Ready |
| Phase 13 cameras/tracks/zones | tenant/store camera ownership and anonymous tracks | Ready |
| Phase 15 reports/exports | permissioned jobs and short-lived downloads | Ready |
| Phase 16 operations/retention | worker gates, leases and retention processing | Locally ready |
| Phase 17 deployment | local regression green; IIS/HTTPS/WebSocket deployment smoke absent | Blocked externally |
| Phase 14 recognition | explicitly optional | Not required |

The live database already has recovered Phase 18 `V1.16.0` objects plus later `V1.16.1` and
`V1.16.2` camera addenda. Source reconciliation therefore uses repeat-safe `V1.17.0` and never drops
or downgrades live objects.

## Delivery order

1. Reconcile versioned SQL, permissions, indexes, settings/rule storage and procedures.
2. Add authoritative risk engine, POS correlation and incident state machine.
3. Add protected AI ingestion, tenant/store-scoped incident/evidence/rules/settings/report APIs.
4. Add tenant/store-authorized SignalR, in-app outbox and idempotent worker maintenance.
5. Add Python normalized observations and false-positive scenario fixtures.
6. Add Angular security dashboard/list/detail/settings/rules/reports routes.
7. Run SQL rerun, unit, integration, AI, security and browser tests and record only observed evidence.

## Rollout

All settings default to disabled Shadow Mode. External escalation remains disabled until a real
store/camera shadow dataset has measured precision and false-positive rate and an authorized owner
accepts thresholds. Source completion cannot satisfy that operational measurement by itself.
