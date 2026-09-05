# CustSearch_AI — Multi-Domain AI Training, Recognition & Video Tracking Planning

## 1. Current Branch Status

The `camera-motion-tenant-storage` branch already contains a useful Python CCTV/AI foundation, but the complete user-wise face recognition, enrollment, tenant-scoped recognition, domain-wise object training, and production training pipeline are still missing.

### Existing Components

| Feature | Status | Existing File |
|---|---|---|
| Camera / RTSP Handling | ✅ Existing | `src/CustSearch.AI/app/camera_source.py` |
| ONNX Model Runtime / Loading | ✅ Existing | `src/CustSearch.AI/app/vision_runtime.py` |
| Anonymous Person Tracking Contracts | ✅ Existing | `src/CustSearch.AI/app/tracking.py` |
| Python FastAPI Service Wiring | ✅ Existing | `src/CustSearch.AI/app/main.py` |
| Security / Retail Observation Contracts | ✅ Existing | `src/CustSearch.AI/app/security_observations.py` |
| Pretrained Model Inference Support | ⚠️ Partial | Existing ONNX boundary |
| AI Model Training Pipeline | ❌ Missing | Add new training project |
| Face Detection + Alignment | ❌ Missing | Add |
| Customer / Staff Face Enrollment | ❌ Missing | Add |
| Face Embedding Generation | ❌ Missing | Add |
| Tenant-wise Face Gallery / Index | ❌ Missing | Add |
| Customer / Staff Identity Matching | ❌ Missing | Add |
| Name + Confidence + Unknown UI | ❌ Missing | Add |
| Consent-based Recognition | ❌ Missing | Add |
| Human Review Queue | ❌ Missing | Add |
| Domain-wise Object Training | ❌ Missing | Add |
| Model Registry / Versioning | ❌ Missing | Add |

---

# 2. Product Goal

CustSearch_AI should not remain only a CCTV face-recognition application.

The target should be a:

> **Multi-Tenant, Multi-Domain Vision AI Platform**

The same platform should be sellable to different types of businesses such as:

- Retail
- Warehouse
- Factory
- Office
- Restaurant
- Showroom
- Parking
- Custom business domains

Each tenant should be able to use a different AI capability set, model pack, object taxonomy, recognition policy, thresholds, alerts, and camera rules.

---

# 3. Final High-Level Architecture

```text
                       CustSearch AI PLATFORM
                                │
                      Tenant / Business
                                │
                ┌───────────────┴────────────────┐
                │                                │
          Business Domain                 Tenant Settings
                │                                │
        Retail / Factory /               AI Features ON/OFF
        Warehouse / Office /             Thresholds
        Restaurant / etc.                Cameras / Zones
                │
                ↓
                    DOMAIN MODEL PACK
                           │
         ┌─────────────────┼──────────────────┐
         ↓                 ↓                  ↓
    Person AI          Object AI         Action AI
         ↓                 ↓                  ↓
    Detection          Detection          Behaviour
    Tracking           Category           Events
         │                 │                  │
         └─────────────────┼──────────────────┘
                           ↓
                    EVENT ENGINE
                           ↓
              Tenant / Store / Camera
                           ↓
              .NET Business Rules
                           ↓
                 Dashboard / Alerts
```

Optional identity layer:

```text
Person Detection
      ↓
Person Tracking
      ↓
Face Available?
      ↓
Consent / Enrollment allowed?
      ↓
Face Embedding
      ↓
ONLY CURRENT TENANT GALLERY
      ↓
 ┌────┴────────┐
 ↓             ↓
MATCH        NO MATCH
 ↓             ↓
Aarav 89%    Unknown
```

---

# 4. Uploaded Video Type Live Experience

The desired output should behave like the uploaded reference video.

Example:

```text
┌─────────────────────┐
│                     │
│       PERSON        │
│                     │
└─────────────────────┘
Aarav Sharma
Customer • 89%

┌─────────────────────┐
│                     │
│       PERSON        │
│                     │
└─────────────────────┘
Unknown
Track #T392
```

The system should support:

- Bounding boxes
- Stable Track IDs
- Known / Unknown states
- Name
- Recognition confidence
- Customer / Staff type
- Zone
- Object interaction
- Live events
- Deduplication
- Camera health
- Tenant isolation

---

# 5. Important Architecture Decision

Do **not** build one giant model that tries to understand everything.

Bad approach:

```text
ONE MODEL
 ↓
Person
Face
Retail Product
Forklift
Helmet
Food
Vehicle
Shelf
Machine
Everything
```

Recommended approach:

```text
SHARED CORE MODELS
    Person Detector
    Person Tracker
    Face Detector
    Face Embedder
    Generic Object Detector

          +

DOMAIN MODEL PACKS

Retail Pack
Warehouse Pack
Factory Pack
Office Pack
Restaurant Pack
Parking Pack
Custom Pack
```

This keeps the product scalable, maintainable, and easier to sell across industries.

---

# 6. Tenant-wise Domain Configuration

Example: Retail tenant

```text
Tenant: ABC Super Market
BusinessDomain = RETAIL

Enabled:
✓ Person Detection
✓ Person Tracking
✓ Customer Recognition
✓ Staff Recognition
✓ Shopping Basket
✓ Shopping Cart
✓ Shelf Interaction
✓ Product Pickup
✓ Checkout
✓ Queue
```

Example: Warehouse tenant

```text
Tenant: XYZ Warehouse
BusinessDomain = WAREHOUSE

Enabled:
✓ Person Detection
✓ Person Tracking
✓ Forklift
✓ Pallet
✓ Carton
✓ Loading Zone
✓ Restricted Zone
✓ PPE
✗ Customer Recognition
✗ Shelf Pickup
```

---

# 7. AI Architecture — Four Main Layers

## 7.1 Person AI

Responsible for:

- Person detection
- Stable tracking
- Track lifecycle
- Entry / exit
- Zone tracking
- Cross-camera handoff
- Occupancy

Flow:

```text
Camera
 ↓
Person Detector
 ↓
Multi Object Tracker
 ↓
TrackId
```

Example:

```text
Frame 1 → T001
Frame 2 → T001
Frame 3 → T001
...
Frame 400 → T001
```

Do not create a new visitor for the same person on every frame.

Recommended tracking attributes:

```text
TrackId
BoundingBox
TrackAge
FirstSeenUtc
LastSeenUtc
DetectionConfidence
TrackConfidence
CurrentZone
PreviousZone
RecognitionState
ObjectAssociations
```

---

# 8. Face Recognition — Enrollment, Not Per-User Model Retraining

Do not retrain the full neural network whenever a new customer is added.

Bad:

```text
Customer Add
 ↓
Retrain AI Model
 ↓
Deploy New Model
```

Recommended:

```text
ENROLLMENT
 ↓
Face Images
 ↓
Quality Validation
 ↓
Face Detection
 ↓
Face Alignment
 ↓
Face Embedding Model
 ↓
Face Vector(s)
 ↓
Tenant Gallery
```

Example:

```text
Tenant 1001
CustomerId 501
Aarav Sharma

Image 1 → Embedding
Image 2 → Embedding
Image 3 → Embedding
Image 4 → Embedding
Image 5 → Embedding
```

Live camera:

```text
Live Face
 ↓
Embedding
 ↓
Tenant 1001 Gallery
 ↓
Similarity
 ↓
0.89
 ↓
Aarav Sharma
```

---

# 9. Recognition States

Support:

```text
Unknown
Candidate
Recognized
NeedsReview
ConsentBlocked
RecognitionDisabled
```

Example threshold logic:

```text
Similarity >= HighThreshold
→ Recognized

Similarity >= ReviewThreshold
and < HighThreshold
→ Candidate / NeedsReview

Similarity < ReviewThreshold
→ Unknown
```

Thresholds must not be hard-coded.

Store configuration such as:

```text
RecognitionThreshold
HumanReviewThreshold
RecognitionCooldownSeconds
MinimumFaceQuality
MinimumDetectionConfidence
```

---

# 10. Identity Recognition Must Be Optional

Tenant AI settings should include:

```text
PersonTracking          ON
ObjectDetection         ON
FaceRecognition         OFF
CustomerRecognition     OFF
StaffRecognition        ON
ObjectInteraction       ON
ZoneMonitoring          ON
BehaviourAnalytics      ON
```

If identity recognition is disabled:

```text
Person #T128
Unknown Visitor
```

If the user is properly enrolled and consent is valid:

```text
Aarav Sharma
Returning Customer
89%
```

---

# 11. Tenant Isolation

This is mandatory.

```text
Camera
 ↓
Store
 ↓
Tenant
 ↓
ONLY THAT TENANT'S GALLERY
```

Never:

```text
All Platform Faces
```

Expected structure:

```text
tenant_gallery_cache
    ├── tenant_10
    ├── tenant_11
    └── tenant_12
```

Recognition query:

```text
RecognitionCandidates
WHERE TenantId = CurrentCameraTenantId
AND RecognitionEnabled = 1
AND ConsentStatus = 'Active'
```

Tenant ID sent by browser must not be trusted directly.

The .NET backend should resolve:

```text
CameraId
→ StoreId
→ TenantId
```

and send only authorized context to Python.

---

# 12. Consent-based Recognition

Before identity matching:

```text
RecognitionEnabled?
      ↓
Person enrolled?
      ↓
Consent Active?
      ↓
Not Revoked?
      ↓
Embedding Available?
      ↓
Recognition
```

Otherwise:

```text
Person Detection ✓
Tracking ✓
Zone Analytics ✓
Object Analytics ✓
Identity Lookup ✗

Label = Unknown
```

Do not infer sensitive personal attributes from CCTV.

---

# 13. Multi-Domain Object Training

Domain-specific object detection is where actual custom model training becomes useful.

## 13.1 Retail Pack

Possible categories:

```text
person
shopping_basket
shopping_cart
product
product_package
shopping_bag
shelf
checkout_counter
cash_register
```

Events:

```text
Person Entered
Person Exited
Shelf Interaction
Probable Product Pickup
Probable Put Back
Checkout Visit
Queue Formed
Item Association
```

---

## 13.2 Warehouse Pack

Categories:

```text
person
forklift
pallet
carton
hand_truck
truck
loading_area
safety_vest
helmet
```

Events:

```text
Forklift Entered Zone
Pallet Moved
Person Entered Restricted Zone
Loading Activity
Missing PPE Observation
Unattended Object
```

---

## 13.3 Factory Pack

Categories:

```text
person
helmet
safety_vest
gloves
machine
tool
material
container
```

Events:

```text
PPE Observation
Restricted Machine Zone Entry
Machine Area Occupancy
Person Down / Fall Candidate
Object Left in Zone
```

---

## 13.4 Office Pack

Categories:

```text
person
chair
desk
door
laptop
bag
```

Events:

```text
Entry
Exit
Occupancy
Restricted Room Entry
Visitor Presence
Staff Presence
```

---

## 13.5 Restaurant Pack

Categories:

```text
person
table
chair
tray
plate
cup
counter
```

Events:

```text
Table Occupied
Table Vacant
Queue
Counter Visit
Wait Time Observation
Staff / Customer Tracking
```

---

# 14. Domain Pack Architecture

Recommended database concepts:

```text
BusinessDomain
----------------
Retail
Warehouse
Factory
Office
Restaurant
Parking
Custom
```

```text
AIModelPack

ModelPackId
BusinessDomainId
Name
Version
Status
MinimumEngineVersion
CreatedUtc
```

```text
AIModelCapability

PersonDetection
PersonTracking
FaceDetection
FaceRecognition
ObjectDetection
ObjectTracking
ZoneAnalytics
ActionRecognition
PPE
Queue
Occupancy
```

```text
TenantAIProfile

TenantId
BusinessDomainId
ActiveModelPackId
RecognitionEnabled
ObjectDetectionEnabled
ActionDetectionEnabled
ThresholdProfileId
```

---

# 15. Configurable Object Category Registry

Do not hard-code all object classes directly in Python.

Recommended:

```text
AIObjectCategory

ObjectCategoryId
BusinessDomainId
Code
DisplayName
ModelClassId
Enabled
DefaultConfidence
IsTrackable
IsZoneAware
```

Example values:

```text
RETAIL_PRODUCT
SHOPPING_CART
SHOPPING_BASKET

WAREHOUSE_PALLET
WAREHOUSE_FORKLIFT

FACTORY_HELMET
FACTORY_VEST
```

Tenant-specific override:

```text
TenantAIObjectSetting

TenantId
ObjectCategoryId
Enabled
MinConfidence
AlertEnabled
```

---

# 16. Person ↔ Object Intelligence

The system should not only detect an object.

It should correlate:

```text
Person Track T109
      +
Product P554
      +
Shelf Zone Z12
      +
Movement
      ↓
Probable Pickup
```

Pipeline:

```text
Person T109
        ↓
Interaction
        ↓
Product Detected
        ↓
Shelf Zone Overlap
        ↓
Object Leaves Shelf Area
        ↓
Observation:
PROBABLE_PICKUP
        ↓
.NET Business Correlation
```

Python should emit observations.

The .NET backend should make the final business/security decision.

---

# 17. Training Project

Keep training separate from production inference.

Recommended structure:

```text
src/

CustSearch.AI/
    app/
        camera/
        inference/
        tracking/
        identity/
        objects/
        actions/
        domains/
        events/
        zones/

CustSearch.AI.Training/
    datasets/
    taxonomy/
    labeling/
    trainers/
    evaluation/
    export/
    registry/
    scripts/
```

Do not run heavy training inside the production CCTV worker.

---

# 18. Dataset Lifecycle

```text
Business Domain
      ↓
Define Categories
      ↓
Collect Images / Videos
      ↓
Data Quality Check
      ↓
Annotation
      ↓
Dataset Version
      ↓
Train
      ↓
Validation
      ↓
Test
      ↓
Export ONNX
      ↓
Model Registry
      ↓
Staging
      ↓
Tenant Pilot
      ↓
Production
```

Avoid leakage such as:

```text
frame100 → Train
frame101 → Test
```

Better:

```text
Camera A / Period A → Train
Camera B / Period B → Validation
Camera C / Site C   → Test
```

---

# 19. Training Metrics

Do not store only one overall accuracy value.

Store:

```text
Precision
Recall
mAP
FalsePositiveRate
FalseNegativeRate
PerClassMetrics
LightingPerformance
CameraAnglePerformance
CrowdedScenePerformance
OcclusionPerformance
NightPerformance
```

Example:

```text
Person        0.94
Cart          0.89
Basket        0.86
Product       0.72
Forklift      0.96
Helmet        0.90
```

---

# 20. Model Registry

Never simply overwrite:

```text
models/model.onnx
```

Recommended:

```text
AIModel

ModelId
ModelCode
ModelType
BusinessDomain
Version
FileReference
Checksum
InputWidth
InputHeight
CreatedUtc
Status
```

Deployment:

```text
AIModelDeployment

TenantId
CameraId nullable
ModelId
DeploymentStatus
ActiveFromUtc
RollbackModelId
```

Example:

```text
retail-person-object-v1.4.2
factory-ppe-v2.1.0
```

Rollback must be supported.

---

# 21. Recommended Python Runtime Structure

```text
src/CustSearch.AI/app/

main.py
config.py

camera/
    camera_source.py
    stream_manager.py
    reconnect.py
    frame_sampler.py

inference/
    model_loader.py
    model_registry.py
    inference_worker.py
    device_provider.py

tracking/
    person_tracker.py
    object_tracker.py
    track_state.py
    handoff.py

identity/
    face_detector.py
    face_alignment.py
    face_quality.py
    face_embedder.py
    recognizer.py

identity/gallery/
    tenant_gallery.py
    gallery_cache.py
    gallery_refresh.py

enrollment/
    enrollment_service.py
    quality_validator.py
    embedding_builder.py

objects/
    object_detector.py
    object_association.py
    category_mapper.py

actions/
    interaction_engine.py
    action_classifier.py
    temporal_buffer.py

domains/
    domain_router.py

    retail/
        config.py
        events.py

    warehouse/
        config.py
        events.py

    factory/
        config.py
        events.py

    office/
        config.py
        events.py

events/
    observation_builder.py
    deduplicator.py

zones/
    zone_engine.py

health/
    model_health.py
    camera_health.py
```

---

# 22. Domain Router

When a camera starts:

```text
CameraId
 ↓
.NET Configuration
 ↓
TenantId
StoreId
BusinessDomain
EnabledFeatures
ModelPack
 ↓
Python DomainRouter
```

Retail example:

```text
Load:
- Person Detector
- Person Tracker
- Retail Object Model
- Retail Action Engine

If RecognitionEnabled:
- Face Detector
- Face Embedder
- Tenant Gallery
```

Factory example:

```text
Load:
- Person Detector
- Person Tracker
- PPE Model
- Factory Zone Engine
```

Do not load unnecessary models for every camera.

---

# 23. Video-style Stable Recognition

Recommended recognition flow:

```text
VIDEO FRAME
     ↓
PERSON DETECTOR
     ↓
TRACKER
     ↓
T109
     ↓
FACE ROI AVAILABLE?
     ↓
YES
     ↓
QUALITY CHECK
     ↓
EMBEDDING
     ↓
TENANT GALLERY
     ↓
Aarav 0.88
     ↓
STABILIZER
     ↓
Aarav 88%
```

Do not recognize on every frame.

Bad:

```text
Frame 1 → Recognition
Frame 2 → Recognition
Frame 3 → Recognition
...
```

Better:

```text
Track Starts
 ↓
Collect Best 3–5 Samples
 ↓
Recognize
 ↓
Lock Stable Result
 ↓
Periodic Revalidation
```

This prevents label flickering.

---

# 24. Cross-Camera Tracking

Future advanced flow:

```text
Entrance Camera
Person T101
      ↓
Exit

800ms gap

Aisle Camera
Person T881
```

The backend may associate:

```text
VisitSession V55

Entrance / T101
   ↓
Aisle / T881
   ↓
Checkout / T331
```

Use cross-camera continuity only when confidence is sufficient.

Face recognition should not be mandatory for anonymous continuity.

---

# 25. Event Deduplication

A camera may run at 15–30 FPS.

Do not store one DB record per frame.

Persist meaningful lifecycle events:

```text
TrackStarted
RecognitionConfirmed
ZoneEntered
ObjectInteraction
ZoneExited
TrackEnded
```

---

# 26. Recommended Database Concepts

Inspect existing schema first and reuse existing tables where possible.

Logical concepts required:

```text
AI_BusinessDomain
AI_Model
AI_ModelVersion
AI_ModelCapability
AI_ModelDeployment

AI_ObjectCategory
AI_TenantObjectSetting

AI_RecognitionProfile
AI_RecognitionEnrollment
AI_RecognitionTemplate

AI_PersonTrackSession
AI_ObjectTrackSession
AI_TrackObjectAssociation

AI_Observation
AI_RecognitionReview

AI_ModelMetric
AI_ModelTrainingRun
AI_Dataset
AI_DatasetVersion
AI_Audit
```

Tenant-owned records should include:

```text
TenantId
```

where applicable:

```text
StoreId
CameraId
```

---

# 27. Runtime vs Training Separation

Training:

```text
Dataset
 ↓
Training
 ↓
Evaluation
 ↓
ONNX Export
 ↓
Model Registry
```

Production:

```text
Model Registry
 ↓
Approved ONNX
 ↓
Inference
```

Do not automatically train new production models from live CCTV traffic.

---

# 28. Continuous Learning

Future workflow:

```text
AI Uncertain
 ↓
Human Review
 ↓
Correct Label
 ↓
Training Candidate
```

Example:

```text
Predicted:
shopping_basket 0.52

Reviewer:
shopping_bag
```

Then:

```text
Reviewed Samples
 ↓
Dataset v8
 ↓
Offline Training
 ↓
Evaluation
 ↓
Approval
 ↓
Model v3
 ↓
Canary Deployment
 ↓
Production
```

Never push human corrections directly into production weights without validation.

---

# 29. Full Final Runtime Flow

```text
                    CCTV / RTSP
                         │
                    Frame Sampler
                         │
              ┌──────────┴──────────┐
              ↓                     ↓
        Person Detector       Object Detector
              ↓                     ↓
        Person Tracker        Object Tracker
              │                     │
              └────────┬────────────┘
                       ↓
                 Association
                       ↓
              Domain Intelligence
                       ↓
         ┌─────────────┼─────────────┐
         ↓             ↓             ↓
      Zones         Actions       Identity
         ↓             ↓             ↓
    Entry/Exit      Pickup       Face Check
    Shelf           Putback      Enrollment
    Checkout        Queue        Tenant Match
         │             │             │
         └─────────────┼─────────────┘
                       ↓
                   Observation
                       ↓
                  Deduplication
                       ↓
                    .NET API
                       ↓
              Business Rule Engine
                       ↓
          SignalR / Dashboard / Alert
```

---

# 30. Implementation Phases

## Phase AI-01 — Existing Foundation Hardening

Reuse existing:

```text
camera_source.py
tracking.py
vision_runtime.py
main.py
security_observations.py
```

Complete:

- Real detector inference
- Real tracker
- Stable Track IDs
- Bounding boxes
- RTSP reconnect handling

Acceptance:

```text
RTSP → Person Boxes → Stable TrackIds
```

---

## Phase AI-02 — Model Registry

Add:

```text
Model
Version
Capabilities
Domain
Deployment
ONNX Checksum
Rollback
```

---

## Phase AI-03 — Generic Object Detection

Add:

```text
object_detector.py
object_tracker.py
category_mapper.py
```

Start with Retail Pack.

---

## Phase AI-04 — Face Enrollment

Add:

```text
Face Detection
Face Alignment
Face Quality Validation
Face Embeddings
Enrollment
```

---

## Phase AI-05 — Tenant Recognition

Add:

```text
Tenant Gallery
Recognition Thresholds
Unknown
Candidate
Recognized
NeedsReview
```

Then implement uploaded-video-style:

```text
Name + Confidence + Bounding Box
```

---

## Phase AI-06 — Stable Person Tracking

Add:

```text
Track Lifecycle
Recognition Locking
Occlusion Handling
Event Deduplication
```

---

## Phase AI-07 — Person ↔ Object Intelligence

Add:

```text
Person T10
+
Product P4
+
Shelf Z2
=
Probable Pickup
```

---

## Phase AI-08 — Domain Packs

Start with:

```text
Retail
```

Then:

```text
Warehouse
Factory
Office
Restaurant
Parking
Custom
```

---

## Phase AI-09 — Training Platform

Build:

```text
Dataset Versioning
Training Scripts
Evaluation
ONNX Export
Model Registry Integration
```

---

## Phase AI-10 — Human Review / Continuous Learning

Add:

```text
Recognition Review
Object Review
Action Review
Training Candidate
Dataset Promotion
```

---

## Phase AI-11 — Multi-Camera Continuity

Add:

```text
Entry
→ Aisle
→ Checkout
```

as one visit/session when technically reliable.

---

## Phase AI-12 — Production Optimization

Add:

```text
GPU / CPU Profiles
Frame Sampling
Worker Queues
Backpressure
Model Warmup
Health Monitoring
Camera Failure Isolation
Model Failure Isolation
```

---

# 31. Key Model Training Rules

The missing functionality should be added with the following separation:

```text
PERSON NAME
    = Enrollment + Embedding
    ≠ Full Model Retraining

PERSON TRACKING
    = Detector + Multi-Object Tracker
    ≠ User Training

BUSINESS OBJECTS
    = Domain-specific Model Training

ACTIONS / INTERACTIONS
    = Temporal AI + Rules + Optional Trained Classifiers

TENANT BEHAVIOUR
    = Configuration
    ≠ Separate Neural Network per Tenant
```

This is the most scalable architecture.

---

# 32. Recommended V1 Production Scope

The first strong production version should contain:

```text
✓ RTSP Camera
✓ Person Detection
✓ Stable Tracking
✓ Bounding Boxes
✓ Unknown Person
✓ Enrolled Person Recognition
✓ Name + Confidence
✓ Customer / Staff Type
✓ Tenant Isolation
✓ Consent Enforcement
✓ Retail Object Detection
✓ Object Tracking
✓ Zone Detection
✓ Shelf Interaction
✓ Probable Pickup / Putback Observations
✓ Checkout Visit
✓ Human Review
✓ Model Registry
✓ Dataset Versioning
✓ Offline Training Pipeline
✓ ONNX Deployment
✓ Real-time SignalR UI
```

---

# 33. Recommended First Commercial Domain

Start with **Retail** as the first complete domain pack because the current project already contains retail-oriented security observation concepts such as:

- Person Entry
- Person Exit
- Shelf Interaction
- Probable Pickup
- Probable Put Back
- Checkout Zone Visit
- Track Continuity
- Probable Item Association
- RFID / EAS signal integration

After Retail becomes stable, reuse the same shared core for Warehouse, Factory, Office, Restaurant, Parking, and Custom deployments.

---

# 34. Final Product Positioning

The final product should become:

> **CustSearch_AI — Tenant-Configurable Multi-Domain Vision AI Platform**

It should provide:

```text
CCTV
→ Person Detection
→ Person Tracking
→ Optional Tenant Recognition
→ Object Detection
→ Object Tracking
→ Zone Analytics
→ Action / Interaction Intelligence
→ Domain Rules
→ Event Deduplication
→ .NET Business Decisions
→ SignalR
→ Dashboard / Alerts / Reports
```

This architecture keeps the existing CCTV foundation reusable while adding all currently missing recognition, training, object intelligence, domain packs, model registry, human review, and tenant-aware AI capabilities.
