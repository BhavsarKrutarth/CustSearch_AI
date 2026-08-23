# Phase 18 — Retail Security / Suspected Unpaid Product Exit Detection

Status: Not Started

## Objective

Implement a production-grade, tenant/store-scoped retail loss-prevention module that combines CCTV observations with checkout/POS/invoice and optional sensor signals to create **reviewable Suspected Unpaid Exit incidents**.

The system must not automatically label a person a thief. AI may raise observations/candidates; an authorized human confirms or dismisses incidents.

## Dependencies

Required or strongly recommended foundations:

- Phase 5 — Tenant users, stores, staff
- Phase 8 — Products, retail billing, invoice/POS data
- Phase 11 — Alerts, SignalR, notification outbox
- Phase 12 — External integrations
- Phase 13 — Cameras, Python CCTV tracking, zones
- Phase 15 — Reports/exports
- Phase 16 — Operations, retention, monitoring
- Phase 17 — Quality/deployment validation

Phase 14 consent-based recognition is optional and must not be required for the baseline security flow. Anonymous `VisitId` / `PersonTrackId` is sufficient.

## Scope

### 1. Store Security Configuration

Add tenant/store-scoped settings for:

- Security monitoring enable/disable
- Unpaid-exit detection enable/disable
- Real-time alert enable/disable
- Risk thresholds
- High-value threshold
- checkout/POS correlation window
- exit grace period
- evidence clip before/after seconds
- evidence retention
- notification channels
- escalation policy

### 2. CCTV / AI Observations

Python AI service produces normalized observations:

- person entry/exit
- shelf-zone interaction
- probable pickup
- probable put-back
- checkout-zone visit
- track continuity/occlusion quality
- probable item/category association

Python does not make the final theft decision.

### 3. POS / Invoice Correlation

ASP.NET server checks:

- paid invoice exists
- matching product/category/quantity exists
- checkout timing is compatible
- cancellation/refund/payment state
- known visit/customer association when available

### 4. Security Rule Engine

Create a versioned server-side rule/risk engine combining:

- visual evidence
- product possession evidence
- exit crossing
- checkout behavior
- payment mismatch
- product value/high-value zone
- optional RFID/EAS event
- confidence penalties for occlusion/gaps

Thresholds are configurable; Angular must not contain authoritative risk rules.

### 5. Incident Workflow

States:

`Observed -> Candidate -> Alerted -> Acknowledged -> UnderReview -> ConfirmedLoss | FalsePositive | Resolved -> Archived`

Only authorized human users may set `ConfirmedLoss`.

### 6. Database / SQL

Use versioned SQL scripts only; no EF migrations.

Planned tables/entities:

- SecurityRules
- SecurityObservations
- SecurityIncidents
- SecurityIncidentItems
- SecurityIncidentEvidence
- SecurityIncidentActions
- SecurityNotificationDeliveries
- SecurityPaymentCorrelations

Optional watchlist storage is out of baseline scope and requires separate privacy/legal approval.

### 7. API

Add protected APIs for:

- list/detail incidents
- acknowledge/assign/review
- false-positive resolution
- confirm loss
- timeline
- evidence listing/access
- security settings/rules
- reports
- protected internal AI observation ingestion

Internal AI ingestion requires service-to-service authentication, idempotency/replay protection, schema validation, body-size limits and camera/store ownership validation.

### 8. SignalR + Notification

Add real-time security events with authorized tenant/store groups:

- `security.incident.created`
- `security.incident.updated`
- `security.incident.critical`
- `security.incident.acknowledged`
- `security.incident.resolved`

Base notification:

- Angular in-app alert
- notification center
- SignalR

Optional tenant channels:

- push
- SMS
- WhatsApp
- email

External notifications should link to authenticated incident detail instead of attaching sensitive CCTV evidence.

### 9. Angular Admin

Create:

- `/admin/security/dashboard`
- `/admin/security/incidents`
- `/admin/security/incidents/:id`
- `/admin/security/settings`
- `/admin/security/rules`
- `/admin/security/reports`

Incident list/detail must show risk, severity, payment correlation, product/category confidence, evidence availability, status, reviewer and resolution.

### 10. Worker Jobs

Implement background jobs for:

- outbox delivery
- alert escalation
- evidence retention cleanup
- payment re-correlation
- stale candidate expiry
- security summary metrics

### 11. Permissions

Add granular permissions:

- Security.Incidents.View
- Security.Incidents.Acknowledge
- Security.Incidents.Assign
- Security.Incidents.Review
- Security.Incidents.ConfirmLoss
- Security.Incidents.Resolve
- Security.Evidence.View
- Security.Evidence.Export
- Security.Settings.View
- Security.Settings.Manage
- Security.Rules.View
- Security.Rules.Manage
- Security.Reports.View

Store scope remains mandatory in addition to permission checks.

### 12. Privacy / Evidence Controls

- AI suspicion is not proof.
- Human confirmation required.
- Anonymous tracking by default where identity is not legitimately associated.
- Do not identify unknown persons via external face databases.
- Do not automatically build a biometric thief database.
- Encrypt evidence in transit/at rest.
- Use authorized short-lived evidence access.
- Audit evidence views/exports and all incident decisions.
- False-positive evidence should have shorter retention.
- Notification payloads must avoid unnecessary customer/biometric/payment data.

### 13. Security Hardening

During this phase also validate production hardening relevant to security operations:

- trusted reverse-proxy Forwarded Headers before relying on `RemoteIpAddress`
- HSTS in production
- explicit allowed hosts
- environment-specific CORS only where needed
- service-to-service authentication for AI
- secret rotation
- request/body size limits
- replay protection
- rate limits for ingestion and evidence APIs
- authorization against IDOR/cross-tenant access

### 14. False Positive Controls

Must correctly handle:

- pickup then put-back
- purchase at checkout
- family/group item handoff
- staff restocking/handling
- crowded aisle
- occlusion
- camera handoff gaps
- duplicate exit events
- re-entry

False-positive reason must be recorded for tuning.

## Required Tests

### Unit

- risk score/rule versions
- state transitions
- permissions
- tenant/store scope
- retention
- deduplication

### Integration

- wrong-tenant camera rejected
- AI service auth/replay protection
- qualifying event creates incident
- paid POS match suppresses/reduces incident as configured
- SignalR group authorization
- evidence access authorization
- notification idempotency
- audit trail

### AI Scenario Tests

- pickup + put-back
- pickup + paid checkout + exit
- pickup + no payment + exit
- group handoff
- staff restock
- low light/occlusion
- crowded aisle

### Security Tests

- cross-tenant IDOR attempts rejected
- expired/invalid service auth rejected
- replayed observation rejected/deduplicated
- evidence path traversal rejected
- evidence URL/access expires
- trusted proxy client-IP behavior validated

## Rollout

1. Shadow Mode only.
2. Compare events against human review/POS truth.
3. Measure precision and false-positive rate per store/camera/zone.
4. Tune camera placement/zones/rules.
5. Enable Medium/High in-app alerts.
6. Enable external escalation only after acceptance.
7. Monitor drift and false positives continuously.

## Done Criteria

Phase 18 is complete only when:

- security schema/scripts apply cleanly without EF migrations
- anonymous visit-based detection works
- POS correlation works
- incident state/audit flow works
- SignalR authorization is store/tenant safe
- evidence access is permissioned and audited
- notification outbox is idempotent
- test scenarios pass
- shadow-mode precision target is agreed and measured
- privacy/retention configuration is documented
- production security hardening checks pass

## Done Summary

Pending approval, implementation and validation.
