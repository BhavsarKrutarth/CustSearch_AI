# CustSearch AI — Tenant Vision AI Implementation Plan

**Prepared against:** `camera-motion-tenant-storage` codebase and `CustSearch_AI_Multi_Domain_AI_Training_Tracking_Planning.md`  
**Decision:** deliver Retail as the first complete domain pack. Build shared Vision AI primitives so Warehouse, Factory, Office, Restaurant and Parking reuse the same control plane.

## 1. Current code position

### Already implemented and reusable

| Area | Current code capability | Readiness |
|---|---|---:|
| Tenant scope | JWT scope, server-side tenant/store authorization, permission policies and tenant-safe queries | Strong |
| Camera operations | Camera CRUD, zones, quotas, RTSP secret references, motion rules and per-user preview grants | Strong |
| Live preview | Python capture service and .NET-authorized short-lived preview sessions; Angular live monitoring grid | Strong, preview only |
| Anonymous tracking contract | Signed, idempotent CCTV ingestion; track sessions, handoffs and camera operational events | Strong contract, not real tracker |
| Consent/review | Purpose-specific consent, encrypted derived templates, candidate review and audit records | Strong workflow |
| Security observations | Retail observation ingestion, evidence handling, alert/incident correlation and SignalR | Strong business boundary |
| AI runtime | FastAPI, camera source abstraction and ONNX Runtime loader boundary | Foundation only |

### Not yet implemented

1. No actual detector post-processing, person tracker, bounding-box stream or stable track generation in Python.
2. No face detector, alignment, quality check, embedding model, tenant gallery cache or similarity search.
3. No customer/staff enrollment capture workflow in the UI; only protected derived-template submission exists.
4. No per-tenant AI profile, business domain, feature switches, threshold profile, model pack or object-category setting.
5. No model registry, model deployment/rollback, dataset versioning, annotation workflow, training worker or ONNX evaluation/export pipeline.
6. No object detector/tracker, shelf interaction engine, pickup/put-back inference, or multi-camera re-identification.
7. Live Monitoring currently displays authorized JPEG previews; it does not render AI boxes, labels, recognition state or live tracks.

## 2. Realistic progress estimate

The platform is approximately **70% ready in control-plane/security/tenant architecture**, but only **15–20% ready in the actual Vision AI and training product**. Against the recommended V1 scope in the source planning document, the combined implementation is approximately **35% complete**.

The most valuable first release is not all multi-domain training. It is a production-quality **Retail Vision V1**: person detection/tracking, consent-gated recognition, zones, retail observations, review queue, monitoring UI and model lifecycle. With one experienced backend/AI engineer plus one frontend engineer, this is roughly **16–22 weeks** including QA and pilot hardening. A single engineer should expect **24–32 weeks**. Training a custom retail object model requires labelled data and pilot video; it cannot be scheduled accurately until that dataset exists.

## 3. Mandatory tenant boundary

```text
Browser → authenticated .NET API → server resolves Tenant + permitted Store + Camera
                                → signed/authorized AI runtime manifest
                                → Python worker loads only that tenant's profile and gallery
                                → signed observation/result → .NET validates and persists
                                → SignalR → only authorized tenant/store users
```

Rules:

- Browser never sends `TenantId` for AI actions.
- Python must never have direct SQL credentials.
- Each gallery query/cache key contains `TenantId`, `StoreId`, profile version and model version.
- A camera service credential remains limited to its configured tenant/store set; it cannot choose another tenant.
- Customer recognition is disabled by default. No raw face image, raw frame or embedding is returned to the browser or stored in tracking events.
- Recognition matches create a candidate first. A human reviewer accepts/rejects it; do not silently change a customer identity from an AI result.

## 4. Tenant-wise admin UI plan

### Customer tenant UI

| Screen | Purpose | Required permissions |
|---|---|---|
| AI profile | Select Retail pack, enabled capabilities, thresholds, retention and store overrides | `Recognition.Settings.Manage` + new `AiProfiles.Manage` |
| Camera monitor | Camera health, preview, box overlay, track state, zone and live observation feed | `Cameras.Preview`, `Cameras.View` |
| Recognition enrollment | Consent first; capture/upload samples; quality result; template status; revoke/delete | `Recognition.Enroll`, `Recognition.Consent.Manage` |
| Recognition review | Candidate, confidence, quality, competing score, evidence reference and accept/reject | `Recognition.Review` |
| Object/action review | Validate pickup, put-back and object labels; promote only approved samples | new `AiReview.Manage` |
| Model status | Active model version, health, rollout stage and last reload | `AiProfiles.View` |

The monitor must show `Unknown`, `Candidate`, `Recognized`, `Needs review`, `Consent blocked` and `Recognition disabled`. Identity must be hidden when consent is absent or the viewer lacks recognition permission. A store-scoped user only sees cameras and tracks for authorized stores.

### Platform admin UI

| Screen | Purpose |
|---|---|
| Domain/model packs | Create/version/retire model packs and capabilities |
| Dataset/training jobs | Dataset versions, split, annotations, evaluation, ONNX artifact and approval |
| Tenant deployments | Assign a pack/profile to tenant or camera; canary, progress and rollback |
| Platform monitoring | Worker/camera/model health, queues, failure rate, GPU/CPU capacity and deployment audit |

Platform users may manage packs and deployments but must not browse a tenant's customer identity or biometric data by default.

## 5. Delivery phases

### P0 — foundation correction (1–2 weeks)

- Define versioned AI runtime-manifest and result contracts.
- Add `BusinessDomain`, `TenantAiProfile`, `TenantAiFeatureSetting`, `ThresholdProfile` and audit records.
- Change the Python boundary from caller-supplied tenant configuration to a .NET-authorized camera manifest.
- Add AI worker health, per-camera isolation, frame sampling, queue/backpressure metrics and correlation IDs.
- Keep existing Phase 13 CCTV events anonymous; use a separate consent-gated recognition result endpoint.

### P1 — Retail person monitoring (3–4 weeks)

- Implement actual ONNX detector pre/post-processing and ByteTrack/DeepSORT-style person tracker.
- Emit stable track lifecycle, box coordinates, zone transitions and confidence with deduplication.
- Add Angular canvas overlay to the existing live-monitoring page; do not send frames through SignalR.
- Add monitor API cursor/recovery and dashboard counters for camera health, active tracks and zone occupancy.
- Acceptance: a camera keeps the same person track through short occlusion and emits one entry/exit lifecycle.

### P2 — Consent-gated enrollment and recognition (3–4 weeks)

- Implement face detector, alignment, quality gates, embedding generation and encrypted template storage.
- Add tenant/store gallery cache with invalidate/rebuild on consent/template change.
- Use candidate states and cooldown/stabilization: best 3–5 good samples per track, no per-frame recognition.
- Build customer enrollment and template-revocation UI. Extend staff only after the customer flow is approved.
- Acceptance: no cross-tenant match, withdrawn/expired consent cannot match, and every match requires review.

### P3 — Model control plane (2–3 weeks)

- Add `AiModel`, `AiModelArtifact`, `AiModelPack`, `AiModelCapability`, `AiModelDeployment`, deployment audit and rollback.
- Store checksum, input contract, model version, supported engine version and status.
- Worker downloads only approved artifacts, verifies checksum, warms model, reports readiness, then uses canary rollout.
- Acceptance: deploy and rollback one camera without interrupting unrelated cameras.

### P4 — Retail object and event intelligence (4–6 weeks)

- Start with generic person + basket/cart + shelf/checkout model. Do not train individual product recognition first.
- Add object tracks, configured category mapping, zone overlap and deterministic interaction rules.
- Emit `ShelfInteraction`, `ProbablePickup`, `ProbablePutBack`, `CheckoutZoneVisit` as observations; .NET remains the authority for alerts/loss decisions.
- Add object/action review queue and evidence references.
- Acceptance: event deduplication prevents repeated alerts for the same track/zone interval.

### P5 — training platform and pilot (4–6 weeks after dataset availability)

- Create separate `CustSearch.AI.Training` project, object storage for datasets, annotation imports, dataset manifest/version, train/validation/test split by camera/site/time, evaluation report and ONNX export.
- Start offline/manual job execution; add distributed workers only after reproducible training and pilot demand.
- Pilot with 2–3 retail stores, varied lighting/camera angles and explicit consent capture.
- Promotion gates: per-class precision/recall, false-positive rate, night/crowd/occlusion tests and rollback model available.

### P6 — later shared expansions (after Retail pilot)

- Warehouse PPE/forklift/restricted zone pack.
- Factory PPE/machine zone pack.
- Office/restaurant/parking packs.
- Cross-camera re-identification only after measured reliability; retain current handoff contract as a separate, lower-risk capability.

## 6. Database/API work items

Add a new upgrade after the current camera/recognition phases:

```text
BusinessDomains
TenantAiProfiles
TenantAiFeatureSettings
ThresholdProfiles
AiModels
AiModelArtifacts
AiModelPacks
AiModelPackCapabilities
AiModelDeployments
AiObjectCategories
TenantAiObjectSettings
AiReviewItems
AiDatasetVersions
AiTrainingJobs
AiModelEvaluations
```

Use tenant/store composite foreign keys and indexes on `(TenantId, StoreId, Status, CreatedUtc)`. Never add biometric vectors to `PersonTrackSessions` or raw images to `CameraOperationalEvents`; that existing separation is correct.

## 7. Priority and sequencing decisions

1. Finish real person tracking before face recognition.
2. Finish consent/enrollment/review before displaying customer names in live UI.
3. Build model registry before custom training/deployment.
4. Use rules plus a generic retail detector for pickup/put-back before attempting action-model training.
5. Do not build all domains together. Retail pilot data and outcomes decide the reusable pack contract.

## 8. Definition of done for Retail Vision V1

- A tenant admin can configure only permitted stores/cameras and enable only approved features.
- A camera monitor shows health, preview, stable anonymous tracks, zones and live observations.
- An enrolled, actively consenting customer can result only in a tenant/store-scoped review candidate.
- A reviewer can approve/reject candidate identity with an audit trail; consent withdrawal disables/deletes active template material according to retention policy.
- A model deployment is checksum-verified, monitored, canaried and rollbackable.
- Retail observations are idempotent, tenant isolated and translated by .NET into alerts/incidents.
- No browser request, Python worker event, log, alert or dashboard can leak another tenant's identity, raw RTSP URL, raw frame or biometric vector.

## 9. AI monitoring connection plan

### What changes after a camera is connected

The current system can securely connect a camera and show an authorized preview. AI monitoring adds a background worker for that camera. The worker samples frames, runs only the tenant's enabled models, keeps track state in memory, and sends small signed observations to .NET. It does not continuously upload camera video to the browser or database.

```text
Tenant Admin adds Camera
  → Store + camera zone + opaque RTSP secret reference
  → camera connection/probe succeeds
  → Admin enables AI profile for that store/camera
  → .NET creates authorized runtime manifest
  → AI worker starts monitoring
  → person/object/zone observations are signed to .NET
  → .NET persists safe metadata, applies rules and pushes permitted events through SignalR
  → Tenant Monitor UI updates live
```

### Tenant monitoring features after connection

| Feature | What customer sees | Backend behaviour |
|---|---|---|
| Camera health | Online/offline/degraded, last frame, reconnect count, processing FPS | Worker heartbeat and camera health event |
| Live monitor | Preview with optional box overlay, track ID, zone and confidence | Preview remains authorized; overlay comes from metadata, not a second video stream |
| Person monitoring | Active people, entry/exit, dwell time, occupancy and zone transition | Stable anonymous track lifecycle and deduplicated events |
| Zone monitoring | Entry, exit, restricted-zone presence, checkout/shelf zone activity | Camera-zone geometry is evaluated in the AI worker/rules layer |
| Retail actions | Shelf interaction, probable pickup/put-back, queue and checkout visit | AI emits observation; .NET decides whether to create alert/incident |
| Recognition, optional | Unknown, candidate, reviewed customer/staff identity, consent blocked | Only enabled with active consent; candidate is reviewed before association |
| Alerts | Severity, camera, track, zone, timestamp, acknowledgement state | Existing alert/outbox/SignalR foundation is reused |
| Evidence | Event snapshot/short clip link, retention state | Existing encrypted tenant storage policy is reused |
| Monitoring dashboard | Camera count, health, active tracks, occupancy, alerts, worker/model status | Aggregated tenant/store scoped metrics |

### Features to keep disabled initially

- Automatic customer-name display without active consent and review.
- Automatic theft/loss conclusion from a camera event.
- Continuous full-video recording by default.
- Cross-camera face re-identification across tenants.
- “Train model” button for every customer or store; enrollment creates embeddings, it does not retrain a model.

## 10. Tenant monitor UI layout

```text
Tenant / Store selector       AI status: Running · Retail Pack v1.0
------------------------------------------------------------------------
Camera tiles                  Live event feed              Health panel
Preview + boxes               10:42 Person entered        FPS / latency
Track ID / zone / state       10:43 Shelf interaction     worker version
Unknown / Candidate label     10:44 Queue threshold       reconnect count
------------------------------------------------------------------------
Active tracks | occupancy | zones | alerts | review candidates | storage
```

The browser receives only the fields needed for the selected screen:

```text
CameraId, StoreId, TrackId, bounding box, zone, state, confidence,
event type, occurred time, alert status and permitted display label.
```

Raw RTSP URLs, worker credentials, raw embeddings and another tenant's track data are never returned.

## 11. Monitoring rollout plan

### Step M1 — connection and health

- Use existing camera creation, secret reference, probe and preview-grant features.
- Add worker registration and heartbeat endpoint.
- Show `Not configured`, `Connecting`, `Running`, `Degraded`, `Offline` and `Paused` in the UI.
- Add restart/reload-model action for authorized camera operators.

### Step M2 — anonymous person monitoring

- Start person detector/tracker at a low frame rate, for example 3–8 FPS per camera depending on hardware.
- Add active track and zone transition APIs plus SignalR event contract.
- Add canvas overlay to current live monitor.
- Add cooldowns so one person does not generate repeated entry/shelf alerts.

### Step M3 — retail monitoring events

- Enable checkout, shelf and restricted/high-value zones per camera.
- Add person-to-zone and person-to-object observations.
- Configure alert threshold, severity, evidence capture and cooldown per tenant/store.
- Send only an observation to the existing security engine; it remains responsible for incident decisions.

### Step M4 — recognition and review

- Enable only for tenants that explicitly turn it on and collect consent.
- Add enrollment, quality results and review queue before live identity labels.
- In live monitor, show `Candidate` until approved; refresh the label only after authorization and review policy permit it.

### Step M5 — operating metrics

- Per camera: input FPS, sampled FPS, inference latency, dropped frames, reconnects, active tracks and model version.
- Per tenant: active cameras, AI minutes, recognition attempts, candidate/review counts, alerts, storage use and quota.
- Platform: worker availability, GPU/CPU use, queue delay, model deployment success/failure and tenant isolation violations (must remain zero).

## 12. Recommended feature packages for selling

| Package | Includes |
|---|---|
| Camera Connect | Camera health, secure preview, zones, storage retention |
| People Monitor | Person detection/tracking, occupancy, entry/exit, zone transitions |
| Retail Insights | Queue, shelf interaction, checkout visits and approved retail observations |
| Recognition Add-on | Consent, enrollment, tenant gallery, candidate review and audited identity display |
| Security Add-on | High-value/restricted zones, evidence, alert/incident workflow and POS/RFID correlation |

This packaging maps cleanly to the existing subscription quota model: cameras, monthly AI processing minutes, monthly recognition attempts, evidence storage and optional model packs.

## 13. User-wise AI option and access plan

AI settings must have three levels. A user receives the intersection of all three; a tenant package never gives every user automatic access.

```text
Platform subscription / tenant AI profile
    → Store and camera feature configuration
        → Role permission and optional user-specific grant
            → final access shown in UI
```

### Per-user options

| AI option | Typical users | Result |
|---|---|---|
| View camera preview | Camera Operator, Store Manager | Can see only granted cameras and their live preview |
| View anonymous monitoring | Camera Operator, Security Analyst | Sees boxes, Track ID, zone and activity; never sees identity |
| View recognized identity | Tenant Owner, approved Security Manager | Sees reviewed/authorized customer or staff label only where consent permits |
| Enroll recognition | CRM Staff, Tenant Admin | Can record consent and submit enrollment samples for approved stores |
| Review candidates | Security Manager, Tenant Admin | Can accept/reject AI recognition candidates with audit reason |
| Configure AI | Tenant Owner, Tenant Admin | Can select allowed model pack, threshold, zone rule and alert policy |
| Handle security alerts | Security Manager | Can acknowledge/resolve AI-triggered alerts and view linked evidence |
| Manage deployments | Platform AI Operator only | Can deploy/rollback approved model packs; cannot browse tenant identity by default |

### UI behaviour for each user

```text
User A: Camera Operator
  Camera preview + boxes + anonymous tracks
  No customer names, no enrollment, no threshold settings

User B: CRM Staff
  Customer consent + enrollment status
  No live security monitor and no deployment settings

User C: Security Manager
  Live monitor + alerts + review queue + evidence
  Identity shown only after consent and permission checks

User D: Tenant Admin
  Store/camera AI settings, authorized review and reporting
  Cannot change platform model artifact or access another tenant

User E: Platform AI Operator
  Model registry/deployment health
  No raw tenant identities or biometric material in normal views
```

### Required database/API additions

Use the existing `CameraUserPreviewGrants` concept as the starting point, but do not overload it with identity privileges. Add a separate versioned grant record:

```text
UserAiAccessGrants
------------------
Id
TenantId
UserId
StoreId nullable        -- null means tenant-wide only for users permitted tenant-wide access
CameraId nullable       -- null means all permitted cameras in the selected store scope
CanViewAnonymousMonitoring
CanViewRecognizedIdentity
CanEnrollRecognition
CanReviewRecognition
CanManageAiSettings
CanViewAiEvidence
CanAcknowledgeAiAlerts
ValidFromUtc
ValidUntilUtc nullable
IsActive
GrantedByUserId
Reason
RowVersion
```

The effective access calculation is:

```text
subscription feature enabled
AND tenant AI profile enabled
AND user role has base permission
AND user is allowed for requested store/camera
AND active user grant permits the sensitive action
AND consent is active for any identity result
```

### API and frontend rules

- Do not trust a `userId`, `TenantId`, `StoreId` or `CameraId` sent by browser to broaden scope.
- APIs resolve the authenticated user, then validate the requested camera against store assignment and `UserAiAccessGrants`.
- SignalR groups must be tenant/store/camera scoped. Identity events go to a separate permission-filtered group; anonymous events can go to normal monitor viewers.
- When a grant expires or an admin removes it, terminate associated preview/monitor sessions and refresh SignalR authorization on reconnect.
- The UI hides unavailable buttons, but the API must enforce every permission independently.
- Audit every grant create/change/revoke and every recognition review, enrollment, identity view/evidence download where required by policy.

### Delivery order

1. Reuse current per-user camera preview grant for preview access.
2. Add base permissions: `AiMonitoring.View`, `AiIdentity.View`, `AiProfiles.Manage`, `AiEvidence.View`, `AiAlerts.Acknowledge`.
3. Add `UserAiAccessGrants`, server authorization decorator and grant-management UI for Tenant Admin.
4. Split SignalR messages into anonymous-monitoring and identity-monitoring contracts.
5. Add effective-access endpoint so the UI renders only the actions the signed-in user can perform.

## 14. Retail People Monitor — Python project plan

Keep `CustSearch.AI` as an inference worker. It must not become a second business backend and must not connect directly to SQL Server.

```text
src/CustSearch.AI/
  app/
    main.py                       # health, worker/admin service endpoints only
    config.py
    contracts/
      runtime_manifest.py          # server-authorized camera/tenant configuration
      observations.py              # signed result/event contract
      health.py
    camera/
      camera_source.py             # existing RTSP secret-reference capture boundary
      capture_worker.py            # one isolated loop per camera
      reconnect.py
      frame_sampler.py             # configurable 3–8 FPS AI sampling
    inference/
      model_loader.py
      person_detector.py           # ONNX preprocess, inference, postprocess/NMS
      model_health.py
    tracking/
      person_tracker.py            # ByteTrack/DeepSORT adapter
      track_state.py               # stable IDs, age, last seen, confidence
      track_stabilizer.py          # occlusion/lost/ended lifecycle
    zones/
      polygon.py
      zone_engine.py               # box/footpoint against configured zones
    events/
      event_builder.py             # entered/exited/zone-entered/zone-exited/occupancy
      deduplicator.py
      delivery.py                  # signed retry/idempotent .NET submission
    runtime/
      camera_supervisor.py         # starts/stops/reloads one camera safely
      manifest_cache.py
      metrics.py
```

### Python execution flow

```text
.NET sends/refreshes authorized CameraRuntimeManifest
  → CameraSupervisor starts camera worker
  → frame sampler reads RTSP frame
  → ONNX person detector produces boxes
  → tracker assigns stable track IDs
  → zone engine evaluates configured zones
  → event builder/deduplicator creates metadata event
  → signed delivery posts event to .NET
  → worker reports heartbeat, FPS, latency and errors
```

### Python V1 contracts

`CameraRuntimeManifest` should include only:

```text
RuntimeCameraId, TenantId, StoreId, CameraId, CameraCode,
opaqueRtspConfigurationReference, enabled=true,
personDetection=true, personTracking=true,
samplingFps, minimumDetectionConfidence,
activeZones, modelArtifactUrl/checksum/version,
eventCooldownSeconds, expiresUtc, manifestVersion
```

`PersonMonitoringEvent` should include only:

```text
eventId, idempotencyKey, RuntimeCameraId, CameraId, StoreId,
TrackId, eventType, occurredUtc, confidence,
boundingBox { x, y, width, height }, zoneCode,
modelVersion, trackerVersion, correlationId
```

For V1 event types use:

```text
camera.online
camera.degraded
camera.offline
person.entered
person.updated              # throttled live overlay update
person.zone_entered
person.zone_exited
person.exited
occupancy.changed
```

### Python V1 acceptance criteria

- One camera failure never stops another worker.
- A detected person keeps the same track ID through a short occlusion.
- At most one entry and one exit event per track/camera lifecycle.
- Event retries retain the same `eventId` and `idempotencyKey`.
- Camera reconnect has exponential backoff and visible health state.
- No RTSP URL, raw image or customer identity appears in logs/events.
- CPU-only baseline works first; GPU provider is an optional configuration after profiling.

## 15. Retail People Monitor — .NET/API project plan

The existing API owns tenant resolution, authorization, persistence, alerts and SignalR. Add a dedicated monitoring service rather than extending the anonymous Phase 13 event endpoint with UI-specific data.

```text
src/CustSearch.Application/
  AiMonitoring/
    IAiMonitoringService.cs
    AiMonitoringModels.cs
    ICameraRuntimeManifestService.cs

src/CustSearch.Infrastructure/
  AiMonitoring/
    AiMonitoringService.cs
    CameraRuntimeManifestService.cs
    AiMonitoringRealtimePublisher.cs
    AiMonitoringEventProcessor.cs

src/CustSearch.API/
  Controllers/
    AiMonitoringController.cs         # tenant UI read/configuration APIs
    InternalAiMonitoringController.cs # signed Python ingestion only
  AiMonitoring/
    AiMonitoringExceptionFilter.cs
    AiMonitoringHub.cs or scoped AlertHub events
```

Required APIs:

| API | Caller | Purpose |
|---|---|---|
| `GET /api/tenant/ai-monitoring/access` | Admin UI | Effective user/store/camera AI permissions |
| `GET /api/tenant/ai-monitoring/summary` | Admin UI | Camera health, active tracks, occupancy and recent events |
| `GET /api/tenant/ai-monitoring/cameras/{id}/tracks` | Admin UI | Authorized active/recent anonymous tracks and overlays |
| `PUT /api/tenant/ai-monitoring/cameras/{id}/profile` | Tenant Admin | Enable/disable person monitor, threshold, sample rate and zones |
| `POST /api/internal/ai-monitoring/events` | Python only | HMAC-signed, idempotent monitoring events |
| `GET /api/internal/ai-monitoring/runtime-manifests/{runtimeCameraId}` | Python only | Short-lived authorized runtime manifest |
| `POST /api/internal/ai-monitoring/heartbeats` | Python only | Worker, model, FPS, latency and health metrics |

Do not make the runtime-manifest endpoint browser accessible. Its service credential maps to the tenant/store/camera scope and .NET validates every returned event against that mapping.

## 16. Retail People Monitor — Angular Admin project plan

```text
src/CustSearch.Admin/src/app/features/ai-monitoring/
  ai-monitoring-api.service.ts
  ai-monitoring.models.ts
  tenant-monitor-dashboard-page.ts
  camera-monitor-page.ts
  camera-ai-profile-page.ts
  user-ai-access-page.ts
  components/
    monitor-camera-tile.ts
    bounding-box-overlay.ts
    active-track-list.ts
    live-event-feed.ts
    camera-health-card.ts
    occupancy-card.ts
```

### Pages and user experience

| Page | Main content | User access |
|---|---|---|
| Tenant Monitor Dashboard | Camera status, active tracks, occupancy, alert/event feed, store filter | `AiMonitoring.View` |
| Camera Monitor | Existing preview frame plus canvas overlay, zones, track list and camera health | camera-specific grant |
| Camera AI Profile | Sampling FPS, detection threshold, active zones, cooldown and pause/resume | `AiProfiles.Manage` |
| User AI Access | Per-user/store/camera grants and expiry | Tenant Admin only |
| Event history | Entry/exit/zone events, filters and linked alert/evidence | monitoring/evidence permissions |

### Live update rules

- Initial data loads through REST.
- SignalR sends small updates: camera health, track box, zone state, occupancy and event summary.
- `bounding-box-overlay` draws on a `<canvas>` positioned above the existing authenticated preview image.
- Throttle UI overlay updates to roughly 4–8 per second per visible camera; do not render every inference frame.
- When browser tab is hidden, stop or reduce frame polling and overlay rendering.
- Reconnect SignalR using cursor-based recovery; REST reload remains the fallback.

### First screen to build

Build **Tenant Monitor Dashboard** first. It validates the end-to-end path without identity complexity:

```text
Store selector | AI worker health | Active cameras | Active people | Occupancy

Camera tile: preview + anonymous box/Track #T18 + zone + confidence

Live feed: Person entered → Zone entered → Person exited
```

Only after this works reliably, add recognition/enrollment, retail object events and user-wise identity access.

## 17. Retail categories and staff-to-customer invoice plan

### Multiple retail categories: yes, but use two different concepts

Retail business can have unlimited business categories such as grocery, fashion, electronics, cosmetics, footwear, furniture, jewellery, pharmacy and home goods. These belong in the existing product/category catalog and are selected on products and invoices.

The AI model should initially use a smaller **vision taxonomy**, not every SKU/category:

| Business catalog category | V1 AI needs to identify | Later trained AI category |
|---|---|---|
| Grocery / Fashion / Cosmetics | person, shelf, basket, cart, checkout | product/package family only if pilot data supports it |
| Electronics / Jewellery | person, display counter, high-value zone, checkout | selected high-value product class |
| Furniture / Home goods | person, large-item zone, checkout | furniture/carton class |
| Pharmacy | person, counter, restricted zone | medicine package only after compliance review |

Do not train a separate Python model for each retail category or each tenant. Use:

```text
One Retail Person/Object model pack
  + product/category registry from .NET
  + camera zones
  + tenant/store feature and threshold configuration
  + optional specialty model for a proven use case
```

The current project already has product categories, product-to-store assignments, invoice items and sales-by-category reporting. The missing part is the AI object-category mapping from a general detection class to the tenant's business category. Add this after People Monitor is stable.

### Staff handles which customer

This should be an **Assistance Session**, not an automatic permanent face-based assignment.

```text
Staff opens “Assist customer” on POS/mobile/admin
  → selects customer by phone/QR/customer code, or an approved recognition candidate
  → selects store and starts AssistanceSession
  → AI monitoring may attach track/zone/time as supporting context
  → staff transfers or ends the session
  → invoice uses current/selected assistance session
  → .NET saves authoritative staff attribution
```

The AI may suggest: “Staff S12 and Customer C45 stayed in the same sales zone for 8 minutes.” It must never directly create an invoice or decide commission. Staff/customer selection at the POS remains the source of truth.

### Invoice attribution design

Current code already supports these useful pieces:

- `RetailInvoice.CustomerId` identifies the billed customer.
- `RetailInvoice.CreatedByUserId` records the user who created the invoice.
- Invoice participants and item-level customer attribution already support multiple customers/items.
- Staff profiles, shifts and presence sessions already exist.

What is missing: explicit **sales-assistant staff attribution**. Do not reuse `CreatedByUserId`, because a cashier may create the invoice while another salesperson assisted the customer.

Add:

```text
CustomerStaffAssistanceSessions
-------------------------------
Id, TenantId, StoreId, CustomerId, StaffProfileId,
PersonTrackSessionId nullable,
StartedUtc, EndedUtc nullable,
StartedByUserId, EndedByUserId nullable,
Source (Manual, QR, POS, ReviewedAI),
Confidence nullable, Status, Notes, RowVersion

RetailInvoiceStaffAttributions
------------------------------
Id, TenantId, StoreId, InvoiceId, StaffProfileId,
AssistanceSessionId nullable,
AttributionType (PrimarySales, SharedSales, Cashier, Referral),
AmountAttributed, PercentageAttributed nullable,
Source (Manual, POS, AssistanceSession),
ConfirmedByUserId, ConfirmedUtc, Notes
```

Rules:

1. Invoice creation selects `CustomerId` and optional current `AssistanceSessionId`.
2. Backend validates that customer, staff, session and invoice belong to the same tenant/store.
3. The POS user may be automatically recorded as `Cashier`; the sales assistant is recorded separately.
4. A manager can split shared sales between multiple staff, with total percentage/amount validation.
5. AI suggestion remains `ReviewedAI` until a permitted user confirms it.
6. Finalized invoices keep staff attribution immutable; corrections create an audited adjustment instead of rewriting history.

### Required Admin UI

| Screen/action | Behaviour |
|---|---|
| Customer profile | Shows active staff assistance session and previous handled-by history, only for permitted store users |
| Staff sales queue | Shows current manually-confirmed customer sessions; AI suggestion is visually separate |
| Invoice editor | Customer selection, primary salesperson, optional split attribution and cashier information |
| Invoice detail | Customer, cashier, sales assistant(s), attribution source and audit trail |
| Sales performance report | Invoice amount, conversion, category mix and commission-ready totals by staff/store/date |

### Delivery order

1. Add manual `CustomerStaffAssistanceSession` from staff/POS UI.
2. Add invoice staff attribution and staff sales report.
3. Add staff presence and zone context to the monitor.
4. Add AI co-presence suggestion only after person tracking is reliable.
5. Add recognition-assisted customer selection only with consent and review.

## Voice-assisted guest interests and invoice context (2026-09-06)

The [detailed voice/visitor/category/invoice plan](Retail_People_Monitor_AI_Phases/AI_VOICE_VISITOR_CATEGORY_INVOICE_PLAN.md) records the existing code review, gaps and delivery gates. Staff explicitly selects a guest/customer assistance session on their authenticated device, speaks the configured trigger and multiple category names, and the server saves scoped visit interests after required resolution/confirmation. A draft created from that exact session automatically includes requested-category context and staff attribution. Actual product selections determine billable items and their catalog categories. Add optional Python speech processing separately from vision inference; a new vision model is not required. Tenant settings, device/session binding, concurrency, duplicate delivery, transfers, anonymous registration and invoice snapshots are specified in the detailed plan.

## Tenant AI approval, voluntary profile enrichment and monitor box colors (2026-09-06)

Status: Planning only. The [detailed addendum](Retail_People_Monitor_AI_Phases/AI_TENANT_PROFILE_ENRICHMENT_MONITOR_COLORS_ADDENDUM.md) extends phases 01, 04, 07 and 08 with:

- Platform-admin capability approval followed by tenant enablement and individual user grants; tenant-scoped provider credential storage, health and revocation.
- Voluntary provider account connections or person-shared profile links, permitted basic details, multiple social profiles and tenant-defined typed extra fields/columns. Support is provider-specific; an AI key does not unlock all social media.
- Bounding-box defaults: new/unknown green, confirmed returning customer blue and confirmed staff purple, with tenant-admin customization, preview, reset and accessible legends.
- Explicit Unknown/candidate/confirmed states, verified prior-visit rules and server-side protection against leaking identity-derived colors to anonymous-only viewers.

Unknown CCTV faces are not used to search public/social platforms or automatically populate personal identities. Profile enrichment requires voluntary sharing/connection and separate consent. No new vision training is required for palette configuration or provider-field imports; existing gallery recognition remains separately consented and reviewed.
