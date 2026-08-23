# CustSearch AI — Retail Security, Theft / Shoplifting Detection Addendum

Status: Planned / Not Implemented Yet  
Applies to: `CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`  
Primary implementation phase: `phase_implementation/PHASE_18_RETAIL_SECURITY_THEFT_DETECTION.md`

> This file is an authoritative addendum to the main CustSearch AI production plan. It adds store-loss-prevention, suspected unpaid-product-exit detection, incident review, evidence, notification and reporting requirements without changing the existing consent, tenant-isolation, audit, CCTV, Aasha, staff, customer, household, invoice or privacy rules.

---

## 1. Repository Review Result Before Adding This Plan

The repository was reviewed before defining this module.

Current implementation is still primarily Foundation through Phase 4:

- Foundation / logging / health checks are present.
- Multi-tenant authentication is present.
- Permission-based authorization is present.
- Platform tenant management is present.
- Angular Admin auth and platform-tenant screens are present.
- The Python AI service currently has a safe FastAPI scaffold, correlation middleware and health endpoint.
- The planned customer, product, invoice, CCTV tracking, real-time alert and recognition modules are not fully implemented yet.
- Therefore, theft/shoplifting detection must be treated as a future implementation module. No existing code should be described as already detecting theft.

### Existing security foundations that should be reused

- Short-lived JWT access tokens.
- Refresh token rotation and token-family reuse detection.
- Refresh token stored in `HttpOnly`, `Secure`, `SameSite=Strict` cookie.
- Access token kept in Angular browser memory rather than persistent browser storage.
- Tenant scope checked server-side.
- Roles and permissions refreshed from authoritative server state.
- Correlation IDs and structured logging.
- Auth rate limiting.
- Audit/authentication event records.

### Security hardening item found during review

The API currently relies on `HttpContext.Connection.RemoteIpAddress` for authentication rate limiting and audit IP values. In production behind IIS/reverse proxy/load balancer, trusted forwarded-header processing must be explicitly configured so the application receives the real client IP only from trusted proxies. Never trust arbitrary `X-Forwarded-For` headers from the public internet.

Also add production HTTP hardening such as HSTS, explicit allowed hosts, environment-specific CORS if cross-origin Admin hosting is used, secure secret injection, request-size limits for evidence endpoints, and service-to-service authentication for the Python AI service.

---

## 2. Business Objective

Add a Store Security / Loss Prevention module that can identify a **possible unpaid product exit** and immediately give the shop owner/security-authorized staff a reviewable incident with evidence.

Example business scenario:

1. A person enters the store.
2. CCTV assigns an anonymous `PersonTrackId` / `VisitId`.
3. The person interacts with a product/shelf zone.
4. The system observes a probable pickup.
5. The person proceeds through the store.
6. POS/invoice data is checked for a matching paid item, category or transaction association.
7. The person crosses the configured store exit zone.
8. If the system has strong evidence that an item is still associated with that person and no matching payment is found, the system creates a **Suspected Unpaid Exit** event.
9. A real-time notification is sent to authorized shop users.
10. A human reviews the incident and marks it Confirmed, False Positive, Resolved or Needs Review.

The system must never automatically label a person a “thief” solely because an AI model produced a score.

---

## 3. Core Product Decision: Suspicion First, Human Confirmation Required

Use neutral operational terminology throughout the product:

- `Security Observation`
- `Suspected Unpaid Exit`
- `Security Incident`
- `Needs Review`
- `Confirmed Loss`
- `False Positive`
- `Resolved`

Do not expose an automatic “Thief” label in UI, API, database or notification templates.

A permanent/repeat-offender watchlist must not be created automatically from AI detections. If such a feature is ever enabled, it requires a separate legal/privacy review, explicit tenant policy, restricted permissions, human confirmation, defined retention and appropriate lawful basis. Face recognition must continue to follow the main plan's consent/privacy rules.

---

## 4. Detection Architecture

The recommended architecture is **multi-signal**, not camera-only.

### 4.1 CCTV signals

The Python AI service should emit observations such as:

- Person detected.
- Person track started/updated/ended.
- Entry zone crossed.
- Exit zone crossed.
- Shelf/product zone approached.
- Hand-to-shelf interaction.
- Probable item pickup.
- Probable item put-back.
- Item/object appears to remain with tracked person.
- Checkout zone entered/exited.
- Occlusion / low-confidence condition.

### 4.2 Retail/POS signals

Use planned retail billing/invoice integration to correlate:

- Invoice created.
- Invoice paid.
- Invoice cancelled.
- Product/SKU lines.
- Quantity.
- Checkout time.
- Register/store.
- Customer/visit association when available.

### 4.3 Optional high-confidence hardware signals

For stores that need stronger exact-product detection, support future integrations with:

- RFID/EAS tags.
- Smart shelf / weight sensors.
- Barcode scan events.
- Gate/exit sensor events.
- Electronic article surveillance alarms.

### 4.4 Important accuracy rule

A generic CCTV camera usually cannot reliably determine the exact SKU for every shelf product, especially with occlusion, similar packaging, poor lighting or crowded aisles.

Therefore:

- CCTV-only mode may identify a **product zone/category** and probable item possession.
- Exact SKU claims require sufficient visual confidence or stronger POS/RFID/sensor correlation.
- Low-confidence SKU guesses must not be shown as facts.
- The incident may display `Unknown item from Zone X` or `Possible Category: Cosmetics` rather than inventing a SKU.

---

## 5. Store Configuration Required

Each tenant/store must configure Security independently.

### 5.1 Security settings

Add tenant/store settings:

- `SecurityMonitoringEnabled`
- `UnpaidExitDetectionEnabled`
- `RealtimeSecurityAlertsEnabled`
- `EvidenceClipEnabled`
- `SecurityMinimumConfidence`
- `CriticalAlertMinimumConfidence`
- `HighValueThreshold`
- `ExitGracePeriodSeconds`
- `CheckoutCorrelationWindowMinutes`
- `IncidentDeduplicationWindowSeconds`
- `EvidencePreEventSeconds`
- `EvidencePostEventSeconds`
- `UnreviewedEvidenceRetentionDays`
- `FalsePositiveEvidenceRetentionDays`
- `ConfirmedIncidentRetentionDays`
- `RequireSecondApprovalForWatchlist`
- allowed notification channels
- escalation timing
- store security contact routing

All values must be server-validated and tenant/store-scoped.

### 5.2 Camera/zone setup

Add or extend camera configuration with polygons/areas for:

- Entrance.
- Exit.
- Checkout.
- Product shelf zones.
- Restricted/staff-only area.
- High-value merchandise zone.
- Blind/low-confidence zones.

Zone configuration must belong to the same `TenantId + StoreId + CameraId` scope.

---

## 6. Security Event State Machine

Recommended incident states:

1. `Observed`
2. `Candidate`
3. `Alerted`
4. `Acknowledged`
5. `UnderReview`
6. `ConfirmedLoss`
7. `FalsePositive`
8. `Resolved`
9. `Archived`

Rules:

- AI can create `Observed` / `Candidate` only.
- Rule engine can move a qualified candidate to `Alerted`.
- Authorized human users can acknowledge/review.
- Only authorized roles can mark `ConfirmedLoss`.
- Every state change must be immutable/audited.
- A Confirmed Loss should record reviewer, reason, optional amount, linked invoice/payment findings and evidence references.
- False Positive must record a reason so it can be used for model/rule tuning.

---

## 7. Suspicion / Risk Score

Do not use one AI confidence number as the entire decision.

Build a server-side risk score from independent signals, for example:

- Pickup confidence.
- Put-back confidence.
- Exit crossing confidence.
- Whether person visited checkout zone.
- Matching paid invoice found/not found.
- Product/category/quantity mismatch.
- Item value / high-value zone.
- Track continuity quality.
- Camera occlusion score.
- Optional RFID/EAS event.
- Duplicate/recent incident suppression.

Example conceptual rule:

`Risk = weighted visual evidence + payment mismatch + exit evidence + sensor evidence - uncertainty penalties`

Weights and thresholds must be configurable/versioned, not hard-coded into Angular.

### Severity

- `Info`: observation only; normally no interruption.
- `Low`: weak suspicion; add to review queue.
- `Medium`: credible mismatch; in-app notification.
- `High`: strong unpaid-exit evidence or high-value item; immediate real-time alert.
- `Critical`: strong multi-signal evidence plus configurable high-value/risk rule; immediate escalation.

A Critical alert still means “urgent review,” not automatic guilt.

---

## 8. Shop Owner Security List / Incident List

Add Admin route such as:

`/admin/security/incidents`

The list should show:

- Incident number.
- Tenant/store.
- Event time.
- Entry time / exit time if known.
- Anonymous `PersonTrackId` / `VisitId`.
- Customer identity only when legitimately associated under existing consent rules.
- Camera and exit zone.
- Suspected product/category.
- Suspected quantity.
- Estimated value if known.
- POS/payment match status.
- Risk score.
- Confidence summary.
- Severity.
- Incident status.
- Notification delivery/acknowledgement status.
- Assigned reviewer.
- Evidence thumbnail/clip availability.
- Final resolution.

Filters:

- Today / date range.
- Store.
- Camera.
- Severity.
- Status.
- Confirmed vs false positive.
- Product/category.
- Value range.
- Reviewed/unreviewed.
- Notification acknowledged/unacknowledged.

### Incident detail page

Show a timeline:

- Entry observation.
- Shelf interaction.
- Pickup/put-back observations.
- Checkout visit.
- Invoice/POS correlations.
- Exit crossing.
- Alert creation.
- Notifications sent.
- Staff acknowledgements.
- Review decisions.

Also show only the minimum evidence needed for review.

---

## 9. Real-Time Notification Plan

Reuse the planned Phase 11 alert/outbox/SignalR architecture.

### 9.1 Required notification channels

Base implementation:

- Angular in-app toast/banner.
- Security notification center.
- SignalR real-time event.

Optional integrations configured per tenant:

- Mobile push when a mobile app/provider exists.
- SMS.
- WhatsApp provider.
- Email.

### 9.2 Example alert payload

Include:

- Incident number.
- Store.
- Severity.
- Time.
- “Possible unpaid product exit detected.”
- Product/category if confidence is adequate.
- Estimated value if known.
- Camera/exit zone.
- Review action/deep link.

Do not place face embeddings, raw biometric templates, full payment data or sensitive customer details in notification payloads.

### 9.3 Escalation

Make escalation store-configurable, for example:

- Send first in-app/SignalR alert immediately.
- If not acknowledged within configured period, send next configured channel.
- Escalate Critical alerts to owner/security manager.
- Stop escalation after acknowledgement/resolution.
- Use idempotency/deduplication so the same incident is not spammed repeatedly.

---

## 10. Proposed Database Model

Schema changes must follow the project rule: **versioned SQL scripts; no EF Core migrations.**

Recommended new tables/entities:

### `SecurityRules`

- Id
- TenantId
- StoreId nullable for tenant default
- RuleCode
- Name
- Enabled
- Severity
- Thresholds/config JSON only for non-sensitive rule parameters
- Version
- CreatedUtc / UpdatedUtc

### `SecurityObservations`

Short-lived normalized observations from AI/sensors.

- Id
- TenantId
- StoreId
- CameraId
- VisitId / PersonTrackId
- ObservationType
- OccurredUtc
- ZoneId
- ProductId nullable
- ProductCategoryId nullable
- Confidence
- CorrelationId
- ModelVersion
- MetadataJson (strict schema/size limit; no secrets)

### `SecurityIncidents`

- Id
- IncidentNumber
- TenantId
- StoreId
- VisitId nullable
- PersonTrackId nullable
- CustomerId nullable only when legitimately linked
- IncidentType
- Severity
- RiskScore
- Status
- FirstObservedUtc
- ExitObservedUtc nullable
- EstimatedLossAmount nullable
- Currency
- AssignedUserId nullable
- ResolutionCode nullable
- ResolutionNotes nullable
- ConfirmedByUserId nullable
- ConfirmedUtc nullable
- CreatedUtc / UpdatedUtc
- RowVersion

### `SecurityIncidentItems`

- Id
- SecurityIncidentId
- ProductId nullable
- ProductCategoryId nullable
- DisplayDescription
- Quantity nullable
- UnitValue nullable
- ProductConfidence nullable
- PaymentMatchStatus

### `SecurityIncidentEvidence`

- Id
- SecurityIncidentId
- EvidenceType
- CameraId nullable
- CapturedUtc
- StorageObjectKey
- ContentHash
- StartUtc / EndUtc for clip
- RetentionUntilUtc
- IsRestricted
- CreatedUtc

Do not store public file URLs. Generate short-lived authorized download/stream access.

### `SecurityIncidentActions`

Immutable review/action history:

- Id
- SecurityIncidentId
- ActionType
- FromStatus
- ToStatus
- UserId
- ReasonCode
- Notes
- OccurredUtc
- CorrelationId

### `SecurityNotificationDeliveries`

- Id
- SecurityIncidentId
- Channel
- RecipientUserId / destination reference
- Status
- AttemptCount
- QueuedUtc
- SentUtc nullable
- AcknowledgedUtc nullable
- ProviderMessageId nullable
- FailureCode nullable

### `SecurityPaymentCorrelations`

- Id
- SecurityIncidentId
- InvoiceId nullable
- Transaction reference
- MatchType
- MatchScore
- MatchedUtc
- Notes

### Optional `SecurityWatchlistEntries`

Do not create this in the baseline MVP unless legal/privacy requirements are approved. If implemented later, it must be restricted, human-reviewed, expiration-based and heavily audited.

---

## 11. API Plan

Tenant/store authorization must come from authenticated server context and permission checks, never from trusting request body tenant IDs.

Suggested endpoints:

- `GET /api/security/incidents`
- `GET /api/security/incidents/{id}`
- `POST /api/security/incidents/{id}/acknowledge`
- `POST /api/security/incidents/{id}/assign`
- `POST /api/security/incidents/{id}/review`
- `POST /api/security/incidents/{id}/resolve`
- `POST /api/security/incidents/{id}/mark-false-positive`
- `POST /api/security/incidents/{id}/confirm-loss`
- `GET /api/security/incidents/{id}/timeline`
- `POST /api/security/incidents/{id}/payment-correlation/recheck`
- `GET /api/security/incidents/{id}/evidence`
- authorized evidence stream/download endpoint using short-lived access
- `GET/PUT /api/security/settings`
- `GET/POST/PUT /api/security/rules`
- `GET /api/security/reports/summary`

Internal AI ingestion should be separate from user APIs, e.g.:

- `POST /api/internal/security/observations`

Internal endpoints require service authentication, replay protection/idempotency, tenant/store/camera validation and rate limits. Do not expose them as anonymous public ingestion endpoints.

---

## 12. SignalR Event Plan

Suggested server events:

- `security.incident.created`
- `security.incident.updated`
- `security.incident.acknowledged`
- `security.incident.resolved`
- `security.incident.critical`

Rules:

- Join users only to authorized Tenant/Store groups.
- Never accept client-selected arbitrary group names as authorization.
- Event contains a lightweight incident summary/id only.
- Angular reloads authoritative incident detail through REST.
- Reconnect must perform REST recovery so alerts are not lost.
- Use event IDs/version for de-duplication.

---

## 13. Python AI Service Plan

Create security-specific modules under `src/CustSearch.AI/app/` such as:

- `cameras/`
- `tracking/`
- `zones/`
- `interactions/`
- `security/`
- `models/`
- `clients/`

Responsibilities:

1. Camera frame ingestion.
2. Person detection/tracking.
3. Zone crossing.
4. Shelf interaction observations.
5. Pickup / put-back candidate observations.
6. Track-quality and uncertainty calculation.
7. Send observations to ASP.NET API.

The Python service should **not** make the final business decision that a theft occurred. The ASP.NET Application layer combines AI observations with POS/invoice/security rules and creates the incident.

### AI service security

- Private network/service endpoint where possible.
- Service identity via mTLS or strong signed service token/HMAC approach.
- Key rotation.
- Timestamp + nonce/idempotency key for replay protection.
- Strict request schemas.
- Body-size limit.
- Camera IDs validated server-side against Tenant/Store ownership.
- No raw camera credentials in logs.
- Model version recorded with each observation.

---

## 14. ASP.NET Layer Mapping

Follow current solution boundaries.

### `CustSearch.Domain`

Add domain entities/enums for:

- SecurityIncident
- SecurityIncidentItem
- SecurityIncidentAction
- SecurityRule
- SecurityObservation
- Evidence reference
- Notification delivery status
- IncidentSeverity / IncidentStatus / SecurityObservationType

### `CustSearch.Application`

Add:

- `ISecurityIncidentService`
- `ISecurityRuleEngine`
- `ISecurityEvidenceService`
- `ISecurityNotificationService`
- queries/commands/DTO models
- tenant/store access guards

### `CustSearch.Infrastructure`

Add:

- SQL/EF mappings where appropriate.
- Dapper report/read queries where appropriate.
- Stored procedure integrations.
- evidence storage adapter.
- incident repository/data services.

### `CustSearch.Integrations`

Add optional adapters for:

- POS/invoice systems.
- WhatsApp/SMS/email providers.
- RFID/EAS providers.

### `CustSearch.API`

Add:

- SecurityIncidentsController.
- SecurityRulesController.
- SecuritySettingsController.
- protected internal AI ingestion controller/endpoint.
- SignalR hub integration when Phase 11 foundation is available.

### `CustSearch.Worker`

Add background jobs for:

- Notification outbox delivery.
- evidence retention cleanup.
- payment re-correlation.
- incident escalation.
- stale candidate expiry.
- security metrics aggregation.

### `CustSearch.Admin`

Add Angular features:

- Security dashboard.
- Live alerts.
- Incident list.
- Incident detail/timeline.
- Evidence viewer.
- Security settings/rules.
- Security reports.

---

## 15. Permission Plan

Add granular permissions such as:

- `Security.Incidents.View`
- `Security.Incidents.Acknowledge`
- `Security.Incidents.Assign`
- `Security.Incidents.Review`
- `Security.Incidents.ConfirmLoss`
- `Security.Incidents.Resolve`
- `Security.Evidence.View`
- `Security.Evidence.Export`
- `Security.Settings.View`
- `Security.Settings.Manage`
- `Security.Rules.View`
- `Security.Rules.Manage`
- `Security.Reports.View`

Store scope must still be enforced even if a user has the permission.

Evidence export should be more restricted than ordinary incident viewing.

---

## 16. Privacy, Evidence and Abuse-Prevention Rules

1. AI suspicion is not proof.
2. Human review is mandatory before `ConfirmedLoss`.
3. Anonymous tracking should remain anonymous unless a legitimate existing customer association is available under project rules.
4. Do not use face recognition to identify unknown people against external databases.
5. Do not automatically build a biometric “thief database.”
6. Every evidence view/export should be permission-checked and auditable.
7. Evidence must be encrypted at rest and in transit.
8. Evidence URLs must be short-lived/authorized.
9. False-positive evidence should have shorter retention.
10. Incident retention must be configurable and comply with applicable policy/law.
11. Do not send sensitive evidence as WhatsApp/email attachments by default; send an authenticated deep link.
12. Require a reason when confirming a loss or overriding AI results.
13. Consider second-person approval for permanent restricted watchlist entries, if that optional feature is ever legally enabled.
14. Security staff must not be able to bypass tenant/store isolation.
15. Audit rule changes, evidence access, status changes and exports.

---

## 17. False Positive Controls

The security module is useful only if staff can trust it.

Mandatory controls:

- Pickup followed by put-back cancels/reduces risk.
- Checkout/POS payment correlation cancels/reduces risk.
- Staff handling inventory must be classified separately from shopper events.
- Family/group handoff should not automatically create separate theft alerts.
- Camera handoff gaps reduce confidence.
- Occlusion reduces confidence.
- Exit without product evidence should not alert.
- Duplicate alerts for the same track/incident must be suppressed.
- Staff can mark false positive with reason.
- False-positive reasons feed model/rule tuning reports.
- Thresholds should be piloted per store before enabling disruptive notifications.

---

## 18. Reporting

Add security reports for authorized shop owner/security roles:

- Suspected incidents by day/week/month.
- Confirmed losses.
- False-positive rate.
- Estimated confirmed loss amount.
- Category/product loss trends.
- Store/camera/zone trends.
- Time-of-day trends.
- Average alert acknowledgement time.
- Average review resolution time.
- Notification delivery success/failure.
- Model/rule version vs false-positive rate.
- POS mismatch patterns.

Never rank or publicly shame individual customers/persons based only on AI suspicion.

---

## 19. Testing / Acceptance Requirements

### Unit tests

- Risk rule calculations.
- State transition rules.
- permission checks.
- tenant/store scope checks.
- duplicate suppression.
- retention calculation.

### Integration tests

- AI observation ingestion auth.
- wrong-tenant camera rejected.
- incident creation after qualifying observation sequence.
- paid invoice prevents/escalation cancellation as configured.
- SignalR group authorization.
- evidence permission controls.
- outbox idempotency.
- audit creation.

### AI tests

Create prerecorded/synthetic scenario tests for:

- pickup then put-back.
- pickup + checkout + paid + exit.
- pickup + no checkout + exit.
- product handoff between family/group members.
- staff restocking.
- crowded aisle.
- occlusion.
- multiple exits.
- person leaves and re-enters.

### Security tests

- service-token replay rejected.
- unsigned/expired internal requests rejected.
- evidence path traversal impossible.
- evidence access requires correct tenant/store permission.
- direct object reference to another tenant rejected.
- rate-limit behavior behind trusted proxy verified.
- notification content does not expose sensitive data.

---

## 20. Rollout Strategy

Do not start with automatic disruptive alerts at maximum sensitivity.

Recommended rollout:

1. **Shadow Mode** — generate candidate events but notify only test/security admins.
2. Compare candidates with actual staff review and POS data.
3. Measure precision / false-positive rate by store/camera/zone.
4. Fix camera positioning and zone mapping.
5. Tune risk thresholds.
6. Enable Medium/High in-app alerts.
7. Enable optional external escalation channels only after store acceptance.
8. Keep ongoing drift/false-positive monitoring.

---

## 21. Dependencies on Existing Roadmap

This module depends on planned capabilities from:

- Phase 5 — tenant users/stores/staff.
- Phase 6/7 — shopper/visit identity where applicable.
- Phase 8 — products/retail billing/POS correlation.
- Phase 11 — alerts, SignalR, notification outbox.
- Phase 12 — external integrations.
- Phase 13 — camera/person tracking/zone events.
- Phase 15 — reports/exports.
- Phase 16 — operations/retention/monitoring.
- Phase 17 — quality/deployment controls.

Phase 14 consent recognition is **not required** for basic theft detection. Security must work with anonymous `PersonTrackId` / `VisitId` and should not depend on face recognition.

---

## 22. Final Shop-Owner Experience

When the module is fully implemented, the shop owner should be able to open Security and see:

- What suspicious event happened.
- Which store/camera/exit saw it.
- When it happened.
- What product/category may be involved.
- Whether payment was found.
- Why the system generated the alert.
- What the AI confidence/uncertainty was.
- The minimum necessary evidence clip/timeline.
- Whether the alert was acknowledged.
- Who reviewed it.
- Final result: Confirmed Loss / False Positive / Resolved.

The objective is a **reviewable, auditable loss-prevention workflow**, not an unverified automatic accusation system.
