# CustSearch AI — Tenant Operations Live Camera Monitoring, Motion Rules, Optional Zones & Storage Planning

**Repository:** `BhavsarKrutarth/CustSearch_AI`  
**Reviewed planning documents:**
- `planning_document/CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`
- `planning_document/CustSearch_AI_SECURITY_THEFT_SHOPLIFTING_ADDENDUM.md`

**Status:** Planning / Future Implementation  
**Scope:** Customer Admin / Tenant Operations / CCTV / Motion Detection / Security Evidence / Storage Retention

---

# 1. Final Requirement Summary

Customer Admin ko apne Tenant/Client ke cameras ek common page par live monitor karne hain.

Example:

```text
Customer Admin
bhavsarkrutarth88@gmail.com

Tenant
Bhavsar Office

Store
Surat Main Office

Admin Camera Limit
5

Connected Cameras
CAM-001 Reception
CAM-002 Main Gate
CAM-003 Billing Counter
CAM-004 Warehouse
CAM-005 Parking
```

Important rules:

1. Platform Admin tenant/customer ke liye maximum camera limit define karega.
2. Agar `MaxCameras = 5` hai to Customer Admin maximum 5 active cameras hi connect/add kar sakta hai.
3. Camera live monitoring ek common screen par hoga.
4. Motion Detection tenant/store/camera-wise configurable hoga.
5. Motion Detection Zone optional hoga.
6. Motion rules multiple categories me configurable honge.
7. Snapshot/video evidence tenant-wise storage quota me save hoga.
8. Tenant/customer ka separate storage folder/object namespace hoga.
9. Default motion evidence retention 15 days ho sakta hai.
10. Background Worker expired evidence automatically remove karega.
11. Tenant storage usage SQL Server me track hoga.
12. TenantId Angular request par trust nahi karna; authenticated server context se derive karna hai.

---

# 2. Existing Project Architecture Reuse

Current project planning already defines:

- Multi-tenant `TenantId` isolation.
- Customer Admin own-tenant access only.
- Tenant subscription/quotas.
- `MaxCameras`.
- Cameras.
- Camera Health.
- Camera Events.
- Live Monitoring.
- Python FastAPI/OpenCV/ONNX CCTV service.
- Person Detection.
- Person Tracking.
- Face Detection.
- Consent-Based Recognition.
- Zone Tracking.
- Dwell-Time Tracking.
- SignalR/WebSocket real-time events.
- Alert rules.
- Worker service.
- Retention handling.
- Retail security incidents.
- Evidence clips.
- Evidence retention cleanup.
- Entry/Exit/Checkout/Shelf/Restricted/High-Value/Blind zones.
- Security Observation and Incident concepts.

New module should extend these foundations instead of creating a separate CCTV/security architecture.

---

# 3. Common Live Camera Monitoring Page — Maximum 5 Cameras

## 3.1 Platform Admin Configuration

Platform Admin Tenant detail/subscription page:

```text
Tenant:
Bhavsar Office

Plan:
Professional

Maximum Stores:
3

Maximum Users:
20

Maximum Cameras:
5

Storage Quota:
2 GB

Motion Evidence Retention:
15 Days
```

Existing `MaxCameras` concept should remain the authoritative camera limit.

Recommended effective limit calculation:

```text
EffectiveCameraLimit =
Tenant Override MaxCameras
OR
Subscription Plan MaxCameras
```

If a Tenant-specific override exists, use that according to platform policy.

---

## 3.2 Camera Add Validation

Customer Admin presses:

```text
Add Camera
```

Backend checks:

```text
Authenticated TenantId
        ↓
Effective MaxCameras
        ↓
Current Active Camera Count
        ↓
Current Count < MaxCameras ?
        ↓
YES → Camera can be added
NO  → Reject
```

Example:

```text
MaxCameras = 5
Current Active Cameras = 5

User tries to add CAM-006

Result:
Camera limit reached.
Your current plan supports maximum 5 cameras.
```

This validation must be server-side.

Angular should also disable the Add Camera button for better UX, but Angular is not the security authority.

---

## 3.3 What Counts Toward Camera Limit?

Recommended:

```text
IsActive = true
AND
IsDeleted = false
```

counts toward `MaxCameras`.

A disabled/decommissioned camera should not consume an active camera license if Platform policy allows reuse.

Recommended separate states:

```text
Active
Disabled
Offline
Decommissioned
Deleted
```

`Offline` is still an active configured camera and therefore should count against the quota.

---

## 3.4 Live Monitoring Grid for 5 Cameras

Recommended layout:

```text
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│ CAM-001          │ │ CAM-002          │ │ CAM-003          │
│ Reception        │ │ Main Gate        │ │ Billing Counter  │
│ LIVE             │ │ LIVE             │ │ LIVE             │
└──────────────────┘ └──────────────────┘ └──────────────────┘

       ┌──────────────────┐ ┌──────────────────┐
       │ CAM-004          │ │ CAM-005          │
       │ Warehouse        │ │ Parking          │
       │ LIVE             │ │ LIVE             │
       └──────────────────┘ └──────────────────┘
```

Page header:

```text
Live Camera Monitoring

Store: Surat Main Office

Camera Limit:       5
Configured:         5 / 5
Online:             4
Offline:            1
Motion Active:      2
Recording Events:   1
Storage Used:       1.25 GB / 2 GB
```

Controls:

```text
[All Cameras]
[Online]
[Motion Active]
[Full Screen]
[Auto Rotate]
[Recent Motion]
```

---

# 4. Motion Rule Configuration — Master Switch

Each camera should have:

```text
Enable Motion Rules: ON / OFF
```

If OFF:

- No motion rule processing for that camera.
- Live viewing still works.
- Camera health still works.
- No new motion evidence clips/snapshots generated from motion rules.

If ON:

Admin can configure one or more rule categories.

---

# 5. Motion Rule Categories Recommended for CustSearch AI

The project already contains customer tracking, staff tracking, dwell-time, zones, alerts and retail security planning.

Therefore do not create only one generic `MotionDetected` switch.

Use configurable **Rule Type / Category**.

---

## 5.1 Category A — Basic Camera Motion

### 1. General Motion

```text
RuleCode:
MOTION_GENERAL

Detect:
Any meaningful frame movement.
```

Use case:

- Basic office activity.
- After-hours activity.

Recommended fields:

```text
Sensitivity
MinimumMotionSeconds
CooldownSeconds
Schedule
```

---

### 2. Person Detected

```text
RuleCode:
PERSON_DETECTED
```

Use project Person Detection/Tracking.

Generate event only when person detection confidence passes configured threshold.

Recommended default for shops.

---

### 3. Person Track Started

```text
RuleCode:
PERSON_TRACK_STARTED
```

Create/update anonymous tracking session.

Use for:

- Visitor journey.
- Current visitors.
- Zone tracking.

---

### 4. Person Track Ended

```text
RuleCode:
PERSON_TRACK_ENDED
```

Useful for closing visitor tracking sessions.

---

# 5.2 Category B — Entry / Exit Rules

These directly fit existing Camera Zone and retail security planning.

### 5. Person Entered Store

```text
RuleCode:
ENTRY_CROSSED
```

Requires Entry Zone.

Actions:

```text
Create observation
Update current visitor count
Optional snapshot
Optional alert
```

---

### 6. Person Exited Store

```text
RuleCode:
EXIT_CROSSED
```

Requires Exit Zone.

Can later participate in security correlation.

---

### 7. Checkout Entered

```text
RuleCode:
CHECKOUT_ENTERED
```

Requires Checkout Zone.

Useful for POS/visit correlation.

---

### 8. Checkout Exited

```text
RuleCode:
CHECKOUT_EXITED
```

Can be correlated with invoice/payment activity.

---

# 5.3 Category C — Dwell / Customer Behaviour

The main project already plans Zone Tracking + Dwell Time.

### 9. Dwell Threshold Reached

```text
RuleCode:
DWELL_THRESHOLD
```

Example:

```text
Zone:
Electronics

Threshold:
60 Seconds
```

Possible action:

```text
Customer interest signal
Staff assistance alert
Analytics event
```

Important:

Dwell time is an interest signal, not proof of purchase intent.

---

### 10. Product/Shelf Zone Approached

```text
RuleCode:
SHELF_ZONE_APPROACHED
```

Useful for:

- Category interest.
- Retail analytics.
- Security correlation.

---

# 5.4 Category D — Retail Security / Shoplifting Signals

These categories align with the Retail Security / Theft / Shoplifting Addendum.

## 11. Hand-to-Shelf Interaction

```text
RuleCode:
SHELF_INTERACTION
```

Represents an observed interaction only.

Do not directly label theft.

---

## 12. Probable Item Pickup

```text
RuleCode:
ITEM_PICKUP_CANDIDATE
```

Fields:

```text
MinimumConfidence
ZoneId
ProductCategoryId optional
EvidenceClipEnabled
```

Status is a candidate observation, not a final theft result.

---

## 13. Probable Item Put-Back

```text
RuleCode:
ITEM_PUTBACK_CANDIDATE
```

Important for reducing false security alerts.

---

## 14. Item Appears to Remain With Person

```text
RuleCode:
ITEM_REMAINS_WITH_TRACK
```

Used as one signal in the security risk engine.

---

## 15. Suspected Unpaid Exit Candidate

```text
RuleCode:
SUSPECTED_UNPAID_EXIT
```

Do not trigger from only one AI signal.

Recommended server-side correlation:

```text
Probable Pickup
+
No Put-Back
+
Exit Crossing
+
No Matching Paid Invoice
+
Track Quality
+
Configured Confidence
=
Suspected Unpaid Exit Candidate
```

UI terminology:

```text
Suspected Unpaid Exit
Needs Review
```

Never automatically show:

```text
Thief
Shoplifter
```

Human review remains mandatory.

---

# 5.5 Category E — Special Security Zones

## 16. Restricted Zone Entry

```text
RuleCode:
RESTRICTED_ZONE_ENTRY
```

Example:

```text
Cash Room
Server Room
Back Office
Stock Room
```

Can create immediate alert.

---

## 17. Staff-Only Area Entry

```text
RuleCode:
STAFF_ONLY_ZONE_ENTRY
```

Use tracking identity and policy/permissions correctly.

Unknown/customer person entering a staff-only zone can create alert.

---

## 18. High-Value Product Zone Activity

```text
RuleCode:
HIGH_VALUE_ZONE_ACTIVITY
```

Example:

```text
Jewellery
Mobile Phones
Premium Cosmetics
Expensive Electronics
```

Can use lower dwell/pickup alert thresholds than normal zones.

---

## 19. Blind / Low-Confidence Zone

```text
RuleCode:
LOW_CONFIDENCE_CONDITION
```

This is not necessarily an alert.

Use it to suppress unreliable security conclusions and record detection quality.

---

# 5.6 Category F — Camera Health / Integrity

## 20. Camera Offline

```text
RuleCode:
CAMERA_OFFLINE
```

SignalR alert to authorized Admin.

---

## 21. Camera Online

```text
RuleCode:
CAMERA_ONLINE
```

Useful after reconnection.

---

## 22. Camera Occlusion / View Blocked

```text
RuleCode:
CAMERA_OCCLUDED
```

Example:

```text
Camera covered
Lens blocked
Image extremely dark
View changed unexpectedly
```

Useful for security monitoring.

---

# 5.7 Category G — Existing CustSearch Intelligence

These are not basic "motion" events, but can use the same rule engine UI.

## 23. Anonymous Visitor Detected

```text
RuleCode:
ANONYMOUS_VISITOR_DETECTED
```

Unknown people become `AnonymousVisitor` first.

---

## 24. Visit Party Detected

```text
RuleCode:
VISIT_PARTY_DETECTED
```

Do not infer family relationships from appearance.

---

## 25. Staff/Customer Proximity

```text
RuleCode:
STAFF_CUSTOMER_PROXIMITY
```

Can support staff/customer interaction evidence and assisted-conversion analytics.

---

## 26. Recognition Review Required

```text
RuleCode:
RECOGNITION_REVIEW_REQUIRED
```

Must remain subject to active biometric consent/privacy rules.

---

# 6. Recommended Rule Category Groups in Admin UI

Instead of showing 26 switches at once, group them:

```text
Motion Rule Category

1. Basic Detection
2. Entry / Exit
3. Customer Behaviour
4. Retail Security
5. Restricted Areas
6. Camera Health
7. Customer Intelligence
```

Example UI:

```text
Camera: CAM-002 Main Gate

Motion Rules: ON

Basic Detection
[x] Person Detected
[ ] General Motion

Entry / Exit
[x] Entry Crossed
[x] Exit Crossed

Customer Behaviour
[ ] Dwell Threshold

Retail Security
[x] Suspected Unpaid Exit
[ ] Item Pickup Candidate

Restricted Areas
[ ] Restricted Zone Entry

Camera Health
[x] Camera Offline
[x] Camera Occluded
```

---

# 7. Per-Rule Configuration

Recommended rule model:

```text
CameraMotionRule

CameraMotionRuleId
TenantId
StoreId
CameraId

RuleType
RuleName

IsEnabled

MinimumConfidence
Sensitivity
MinimumDurationSeconds
CooldownSeconds

StartTime
EndTime

DaysOfWeek

EvidenceSnapshotEnabled
EvidenceClipEnabled

EvidencePreEventSeconds
EvidencePostEventSeconds

Severity
CreateAlert
RealtimeNotificationEnabled

ZoneRequired
ZoneId nullable

CreatedUtc
UpdatedUtc
```

Do not hard-code rule thresholds in Angular.

Backend should be authoritative.

---

# 8. Motion Detection Zone — Optional

Requirement:

> Zone optional dena hai. Admin ON kare tabhi zone set karna compulsory ho.

Recommended camera-level setting:

```text
EnableMotionZone = false
```

Default:

```text
OFF
```

When OFF:

```text
Detection Area:
Entire Camera Frame
```

No zone coordinates required.

When ON:

```text
Detection Area:
Configured Polygon / Rectangle
```

Admin must create/select at least one valid zone.

---

## 8.1 Admin UI

```text
Use Detection Zone:
[ OFF ]

Result:
Motion detection works on full frame.
```

When enabled:

```text
Use Detection Zone:
[ ON ]

Zone Type:
[ Entry ▼ ]

Zone Name:
Main Gate

Draw Zone:
[Open Camera Frame Editor]

[ Save ]
```

Validation:

```text
EnableMotionZone = true
AND
No active zone exists

→ Save rejected.
```

---

# 9. Zone Types

Based on existing project/security planning:

```text
Entrance
Exit
Checkout
Product Shelf
Category
Restricted
Staff Only
High Value Merchandise
Blind / Low Confidence
Custom
```

Recommended database:

```text
CameraZone

CameraZoneId
TenantId
StoreId
CameraId

ZoneCode
ZoneName
ZoneType

CoordinatesJson

IsEnabled

CreatedUtc
UpdatedUtc
```

Scope validation must always be:

```text
TenantId
+
StoreId
+
CameraId
```

---

# 10. Storage Requirement

Example infrastructure:

```text
Server Storage:
1 TB

Customers:
10

Customer/Tenant Storage Quota:
2 GB each

Camera Limit:
5 cameras per customer

Retention:
Last 15 Days
```

At 10 tenants × 2 GB:

```text
Total allocated tenant quota:
20 GB
```

The remaining server capacity should NOT automatically be considered available to those tenants.

Always keep:

- OS/database space.
- Application space.
- Logs.
- Temporary video processing files.
- Export files.
- Backup headroom.
- Growth headroom.

Prefer a dedicated data volume for CCTV evidence.

Example:

```text
D:\CustSearchStorage\
```

instead of storing evidence inside application deployment folders.

---

# 11. Critical Storage Decision — Event Evidence vs Continuous Recording

With:

```text
2 GB Tenant Storage
5 Cameras
15 Days
```

continuous CCTV recording is not practical.

2 GB / 15 days is approximately:

```text
136 MB per tenant per day
```

Across 5 cameras:

```text
~27 MB per camera per day
```

Therefore this quota is best for:

```text
Motion snapshots
+
Short motion clips
+
Security incident evidence
```

not full 24×7 recording.

Recommended baseline:

```text
Normal live stream:
Not stored continuously

Motion event:
Snapshot saved

Important motion/security event:
5-20 second clip saved

Confirmed security incident:
Evidence retention according to policy
```

If future requirement is 24×7 recording, introduce a separate large recording quota/NVR storage plan.

---

# 12. Storage Architecture

Recommended:

```text
Camera
   ↓
RTSP
   ↓
Python AI
   ↓
Motion / Security Event
   ↓
Create Snapshot / Clip
   ↓
Evidence Storage Service
   ↓
Tenant Storage Quota Check
   ↓
Tenant Folder/Object Namespace
   ↓
Database Evidence Record
```

Do not save raw binary video into normal SQL Server columns.

SQL Server should store metadata/reference only.

---

# 13. Tenant Storage Folder Structure

Example:

```text
D:\CustSearchStorage\
│
├── tenants\
│   │
│   ├── TEN-000001\
│   │   ├── STO-000001\
│   │   │   ├── CAM-001\
│   │   │   │   ├── 2026\
│   │   │   │   │   ├── 08\
│   │   │   │   │   │   ├── 28\
│   │   │   │   │   │   │   ├── snapshots\
│   │   │   │   │   │   │   └── clips\
│   │   │   ├── CAM-002\
│   │   │   └── CAM-003\
│   │   └── security-incidents\
│   │
│   └── TEN-000002\
```

Do not use customer email address as physical folder name.

Use immutable IDs/codes:

```text
TenantCode
TenantId
StoreId
CameraId
```

Reason:

- Email can change.
- Folder traversal/security is easier to control.
- Tenant isolation remains clear.
- File paths remain stable.

---

# 14. Storage Object Key

Database should store a relative object key:

```text
tenants/TEN-000001/STO-000001/CAM-002/2026/08/28/clips/event-12345.mp4
```

Do not store publicly accessible URLs.

Generate short-lived authorized access.

---

# 15. Tenant Storage Configuration

Recommended table:

```text
TenantStoragePolicy

TenantStoragePolicyId
TenantId

StorageQuotaBytes

DefaultRetentionDays

MotionSnapshotRetentionDays
MotionClipRetentionDays
FalsePositiveRetentionDays
UnreviewedEvidenceRetentionDays
ConfirmedIncidentRetentionDays

WarningPercent
CriticalPercent

AllowSnapshots
AllowMotionClips

AutoCleanupEnabled

CreatedUtc
UpdatedUtc
```

Example:

```text
TenantId:
TEN-000001

StorageQuotaBytes:
2147483648

DefaultRetentionDays:
15

MotionSnapshotRetentionDays:
15

MotionClipRetentionDays:
15

FalsePositiveRetentionDays:
3

UnreviewedEvidenceRetentionDays:
15

ConfirmedIncidentRetentionDays:
30

WarningPercent:
80

CriticalPercent:
90

AutoCleanupEnabled:
true
```

If business requirement is strict 15 days for everything, set all normal retention values to 15.

Confirmed incidents can optionally have a different retention policy because the existing security addendum already distinguishes evidence types.

---

# 16. Tenant Storage Usage Table

Recommended:

```text
TenantStorageUsage

TenantId

QuotaBytes
UsedBytes

SnapshotBytes
MotionClipBytes
SecurityEvidenceBytes
OtherBytes

LastCalculatedUtc
LastCleanupUtc
```

For correctness, also keep per-object size in evidence records.

Do not depend only on folder scanning for every Admin page request.

---

# 17. Evidence Record

Extend/use Security Incident Evidence concept:

```text
CameraEvidence

CameraEvidenceId

TenantId
StoreId
CameraId

MotionEventId nullable
SecurityIncidentId nullable

EvidenceType
StorageObjectKey

FileSizeBytes
ContentType
ContentHash

CapturedUtc

RetentionUntilUtc

IsRestricted
IsPinned
IsDeleted

DeletedUtc nullable
DeleteReason nullable

CreatedUtc
```

Evidence types:

```text
MotionSnapshot
MotionClip
SecuritySnapshot
SecurityClip
RecognitionReviewSnapshot
Other
```

---

# 18. Storage Quota Enforcement

Before saving a new clip:

```text
Tenant quota
      ↓
Current used bytes
      ↓
Estimated incoming bytes
      ↓
Used + Incoming <= Quota ?
```

If YES:

```text
Save evidence
Update usage
```

If NO:

Recommended flow:

```text
1. Run cleanup of already-expired files.
2. Recalculate available storage.
3. If still full:
   delete eligible oldest normal motion evidence according to policy.
4. Never silently delete retention-locked/pinned evidence.
5. If still full:
   save snapshot only if possible.
6. If still impossible:
   create event without video and raise StorageQuotaExceeded alert.
```

Do not make camera live monitoring fail only because recording storage is full.

---

# 19. Storage Thresholds

Recommended:

```text
0-79%
Normal

80%
Warning

90%
Critical

100%
Hard Limit
```

Customer Admin UI:

```text
Storage

1.65 GB / 2.00 GB
82.5%

Status:
Warning

Oldest Evidence:
14 Days

Auto Cleanup:
Enabled
```

Platform Admin should see storage per Tenant.

---

# 20. Automatic 15-Day Cleanup

Use existing `CustSearch.Worker`.

Recommended job:

```text
EvidenceRetentionCleanupWorker
```

Run:

```text
Every 1 hour
```

or every few hours depending on scale.

Logic:

```text
Current UTC
   ↓
Find evidence
WHERE RetentionUntilUtc <= CurrentUtc
AND IsDeleted = false
AND IsPinned = false
   ↓
Delete physical file/object
   ↓
Mark DB record deleted
   ↓
Subtract/recalculate TenantStorageUsage
   ↓
Audit cleanup
```

Prefer setting:

```text
RetentionUntilUtc
```

at evidence creation time.

Example:

```text
CapturedUtc:
2026-08-28 10:00 UTC

Retention:
15 Days

RetentionUntilUtc:
2026-09-12 10:00 UTC
```

This is better than repeatedly calculating age from folder names.

---

# 21. Cleanup Priority When Quota Is Full Before 15 Days

This is important.

If a tenant generates too much motion and 2 GB fills before 15 days, the system needs a defined policy.

Recommended deletion priority:

```text
Priority 1
Expired temporary files

Priority 2
Expired false-positive evidence

Priority 3
Expired normal motion snapshots

Priority 4
Expired normal motion clips

Priority 5
Oldest non-pinned normal motion evidence
if tenant policy allows quota-pressure cleanup

Never automatically delete:
Pinned/Retention-Locked evidence
before its defined retention policy
unless an explicit Platform policy allows it
```

Recommended configuration:

```text
QuotaPressurePolicy:
DeleteOldestNormalMotion
```

Alternative:

```text
QuotaPressurePolicy:
StopNewClips
```

For your project, recommended default:

```text
Delete expired evidence first.
If still full, stop creating normal clips and continue event metadata/snapshot where possible.
Do not unexpectedly destroy important security evidence.
```

---

# 22. Recommended Storage Behaviour for 2 GB Customer Quota

For 5-camera tenants:

### General motion

```text
Snapshot:
Optional

Video Clip:
OFF by default
```

### Person entry/exit

```text
Snapshot:
ON

Video Clip:
Optional
```

### Dwell event

```text
Snapshot:
Optional

Video Clip:
OFF
```

### Restricted zone

```text
Snapshot:
ON

Video Clip:
ON
```

### Suspected unpaid exit

```text
Snapshot:
ON

Video Clip:
ON

Pre Event:
5 sec

Post Event:
10 sec
```

### Camera offline

```text
No video evidence required
```

This prevents unnecessary storage consumption.

---

# 23. Clip Encoding Recommendation

For evidence clips:

```text
Codec:
H.264

Resolution:
Use sub-stream where adequate

Example:
640×360 or 720p

FPS:
5-10 FPS for analysis/evidence when acceptable

Clip Length:
5-20 sec event-based
```

Keep live viewing quality separate from evidence storage quality.

Example:

```text
Main Stream
1080p
→ Live Monitoring

Sub Stream
640×360 / 720p
→ AI Detection + Short Evidence Clips
```

---

# 24. Storage Admin Page

Add:

```text
Platform Admin
→ Tenant
→ Storage
```

Fields:

```text
Storage Enabled

Tenant Storage Quota:
[ 2 ] [GB]

Default Retention:
[ 15 ] Days

Motion Snapshot Retention:
[ 15 ] Days

Motion Clip Retention:
[ 15 ] Days

False Positive Retention:
[ 3 ] Days

Confirmed Security Incident Retention:
[ 30 ] Days

Auto Cleanup:
[ON]

Quota Warning:
[80] %

Quota Critical:
[90] %

Quota Pressure Behaviour:
[Stop New Clips ▼]
```

---

# 25. Customer Admin Storage Page

Customer Admin can view:

```text
Storage Usage:
1.25 GB / 2 GB

Available:
0.75 GB

Usage:
62.5%

Snapshots:
250 MB

Motion Clips:
600 MB

Security Evidence:
400 MB

Retention:
15 Days

Auto Cleanup:
Enabled

Oldest Normal Evidence:
14 Days
```

Customer Admin should not be able to increase its own licensed quota unless your commercial plan explicitly allows self-service upgrades.

---

# 26. Motion Rule UI + Storage UI Together

Recommended camera configuration:

```text
Camera:
CAM-002 Main Gate

--------------------------------

Live Camera
Enabled: YES

--------------------------------

Motion Rules
Enabled: YES

Rule:
Person Detected

Sensitivity:
70%

Minimum Confidence:
80%

--------------------------------

Detection Zone
Use Zone:
YES

Zone:
Main Gate Entry

--------------------------------

Evidence
Save Snapshot:
YES

Save Clip:
YES

Pre Event:
5 sec

Post Event:
10 sec

--------------------------------

Retention
Use Tenant Default:
YES

Tenant Default:
15 Days
```

---

# 27. Database Planning

Recommended additions/extensions.

## Tenant / Subscription

Existing planning already contains `MaxCameras`.

Extend storage policy through either plan + tenant override tables.

```text
SubscriptionPlan

MaxStores
MaxUsers
MaxCameras

DefaultStorageQuotaBytes
DefaultEvidenceRetentionDays
```

Optional tenant overrides:

```text
TenantQuotaOverride

TenantId
MaxCameras nullable
StorageQuotaBytes nullable
EvidenceRetentionDays nullable
```

---

## Camera

```text
Camera

CameraId
TenantId
StoreId

CameraCode
CameraName

RtspSecretReference

Status
IsActive

MotionRulesEnabled
EnableMotionZone

LastHeartbeatUtc
CreatedUtc
UpdatedUtc
```

Avoid storing unencrypted camera password directly.

---

## CameraMotionRule

```text
CameraMotionRule

CameraMotionRuleId
TenantId
StoreId
CameraId

RuleType
IsEnabled

Sensitivity
MinimumConfidence
MinimumDurationSeconds
CooldownSeconds

StartTime
EndTime
DaysOfWeek

ZoneRequired
ZoneId nullable

SnapshotEnabled
ClipEnabled

PreEventSeconds
PostEventSeconds

Severity
CreateAlert
RealtimeNotificationEnabled

CreatedUtc
UpdatedUtc
```

---

## CameraZone

```text
CameraZone

CameraZoneId
TenantId
StoreId
CameraId

ZoneType
ZoneCode
ZoneName

CoordinatesJson

IsEnabled

CreatedUtc
UpdatedUtc
```

---

## CameraMotionEvent

```text
CameraMotionEvent

CameraMotionEventId
TenantId
StoreId
CameraId

RuleId
RuleType

PersonTrackId nullable
VisitId nullable
ZoneId nullable

StartedUtc
EndedUtc

Confidence
Severity

EventId
CorrelationId

CreatedUtc
```

---

## CameraEvidence

```text
CameraEvidence

CameraEvidenceId
TenantId
StoreId
CameraId

CameraMotionEventId nullable
SecurityIncidentId nullable

EvidenceType

StorageObjectKey
FileSizeBytes
ContentHash

CapturedUtc
RetentionUntilUtc

IsRestricted
IsPinned

IsDeleted
DeletedUtc
DeleteReason

CreatedUtc
```

---

## TenantStoragePolicy

```text
TenantStoragePolicy

TenantStoragePolicyId
TenantId

StorageQuotaBytes

DefaultRetentionDays
SnapshotRetentionDays
MotionClipRetentionDays
FalsePositiveRetentionDays
ConfirmedIncidentRetentionDays

WarningPercent
CriticalPercent

QuotaPressurePolicy

AutoCleanupEnabled

CreatedUtc
UpdatedUtc
```

---

## TenantStorageUsage

```text
TenantStorageUsage

TenantId

QuotaBytes
UsedBytes

SnapshotBytes
MotionClipBytes
SecurityEvidenceBytes

LastCalculatedUtc
LastCleanupUtc
```

---

# 28. Stored Procedure Planning

Project rule says schema changes via versioned SQL scripts and Dapper for complex reads/event ingestion where appropriate.

Recommended:

```text
CAMERA_LIST_BY_TENANT_GET
CAMERA_CREATE_VALIDATE_QUOTA
CAMERA_LIVE_LIST_GET

CAMERA_MOTION_RULE_LIST_GET
CAMERA_MOTION_RULE_UPSERT

CAMERA_ZONE_LIST_GET
CAMERA_ZONE_UPSERT

CAMERA_MOTION_EVENT_INSERT
CAMERA_MOTION_EVENT_LIST_GET

CAMERA_EVIDENCE_INSERT
CAMERA_EVIDENCE_EXPIRED_LIST_GET
CAMERA_EVIDENCE_MARK_DELETED

TENANT_STORAGE_POLICY_GET
TENANT_STORAGE_POLICY_UPDATE
TENANT_STORAGE_USAGE_GET
TENANT_STORAGE_USAGE_RECALCULATE
```

Every tenant-owned stored procedure must receive/enforce the server-resolved TenantId.

---

# 29. API Planning

## Camera Limit

```http
GET /api/tenant-operations/cameras/quota
```

Response example:

```json
{
  "maxCameras": 5,
  "configuredCameras": 5,
  "remainingCameras": 0
}
```

---

## Live Monitoring

```http
GET /api/tenant-operations/cameras/live
POST /api/tenant-operations/cameras/{cameraId}/stream-token
```

---

## Motion Rules

```http
GET /api/tenant-operations/cameras/{cameraId}/motion-rules
POST /api/tenant-operations/cameras/{cameraId}/motion-rules
PUT /api/tenant-operations/motion-rules/{ruleId}
```

---

## Zones

```http
GET /api/tenant-operations/cameras/{cameraId}/zones
POST /api/tenant-operations/cameras/{cameraId}/zones
PUT /api/tenant-operations/camera-zones/{zoneId}
```

---

## Motion Events

```http
GET /api/tenant-operations/motion-events
GET /api/tenant-operations/motion-events/{eventId}
```

---

## Storage

```http
GET /api/tenant-operations/storage/usage
GET /api/tenant-operations/storage/policy
```

Platform Admin:

```http
GET /api/platform/tenants/{tenantId}/storage
PUT /api/platform/tenants/{tenantId}/storage-policy
```

---

# 30. SignalR Events

Reuse existing real-time architecture.

Suggested events:

```text
CameraOnline
CameraOffline

MotionDetected
PersonDetected
EntryCrossed
ExitCrossed
DwellThresholdReached

RestrictedZoneEntered

SecurityIncidentCreated
SecurityIncidentUpdated

StorageWarning
StorageCritical
StorageQuotaExceeded

EvidenceDeleted
```

Groups:

```text
tenant:{TenantId}
store:{StoreId}
```

Server assigns authorized groups.

Angular must never choose arbitrary Tenant groups as authorization.

---

# 31. Worker Jobs

Use `CustSearch.Worker`.

Recommended jobs:

```text
CameraHealthWorker
EvidenceRetentionCleanupWorker
StorageUsageReconciliationWorker
SecurityIncidentEscalationWorker
NotificationOutboxWorker
StaleMotionEventCleanupWorker
```

## EvidenceRetentionCleanupWorker

Responsibilities:

```text
Find expired evidence
Delete physical object
Mark DB row deleted
Update tenant storage usage
Write audit/diagnostic logs
```

## StorageUsageReconciliationWorker

Run periodically to verify:

```text
Database UsedBytes
vs
actual storage objects
```

Correct inconsistencies safely.

---

# 32. File Delete Safety

Recommended deletion process:

```text
1. Select eligible DB record.
2. Confirm TenantId/ObjectKey mapping.
3. Delete physical object.
4. If delete succeeds:
      mark DB deleted.
5. Update usage counters.
6. Audit result.
```

Handle missing physical files safely.

Do not delete files only by accepting arbitrary frontend paths.

---

# 33. Security Requirements

1. `TenantId` must be server-derived.
2. Camera access must validate Tenant + Store scope.
3. Storage object key must be tenant scoped.
4. Never expose physical server path to Angular.
5. Never expose raw RTSP credentials.
6. Evidence view/download needs permission check.
7. Evidence should be encrypted at rest and in transit.
8. Evidence access URL should be short-lived.
9. Evidence export should be audited.
10. Internal Python ingestion should use service authentication.
11. Camera IDs sent by Python must be validated against Tenant/Store ownership.
12. Recognition must keep existing consent requirements.
13. Security AI output remains suspicion/observation until human review.

---

# 34. Recommended Permissions

```text
Cameras.View
Cameras.ViewLive
Cameras.Manage
Cameras.ManageRules
Cameras.ManageZones
Cameras.ViewEvents
Cameras.ViewHealth

Storage.ViewUsage

Security.Incidents.View
Security.Incidents.Review
Security.Evidence.View
Security.Evidence.Export

Platform.TenantStorage.Manage
Platform.TenantQuota.Manage
```

---

# 35. Recommended Implementation Phases

## Phase A — Camera Quota Enforcement

Implement:

```text
MaxCameras
Camera count API
Server-side camera quota validation
5-camera Customer Admin UI limit
```

Test:

```text
Max = 5
Add camera 1-5 → PASS
Add camera 6 → DENIED
```

---

## Phase B — Common Live Monitoring

Implement:

```text
5 camera grid
Camera health
Full screen
Filters
Secure streaming token
```

---

## Phase C — Motion Rule Engine

Implement initial categories:

```text
Person Detected
Entry Crossed
Exit Crossed
Dwell Threshold
Restricted Zone Entry
Camera Offline
```

Then add advanced categories.

---

## Phase D — Optional Zones

Implement:

```text
EnableMotionZone = false default

OFF:
Full-frame processing

ON:
At least one zone required
```

---

## Phase E — Evidence Storage

Implement:

```text
Tenant folder/object namespace
Camera evidence metadata
2 GB quota
Usage calculation
Storage UI
```

---

## Phase F — 15-Day Retention Worker

Implement:

```text
RetentionUntilUtc
Background cleanup
Usage update
Audit
```

---

## Phase G — Retail Security Rules

Implement:

```text
Shelf Interaction
Pickup Candidate
Put-Back Candidate
Checkout Correlation
Suspected Unpaid Exit
Human Review
Evidence
```

Do this only after basic CCTV tracking/zone/event flow is stable.

---

# 36. Smoke Test Plan

## Camera Quota

```text
Tenant quota:
5 Cameras

Create Camera 1 → PASS
Create Camera 2 → PASS
Create Camera 3 → PASS
Create Camera 4 → PASS
Create Camera 5 → PASS
Create Camera 6 → FAIL
```

---

## Tenant Isolation

```text
Tenant A Camera
cannot be viewed by
Tenant B Admin
```

Test:

```text
Change CameraId manually
→ 403/404
```

---

## Optional Zone

```text
EnableMotionZone = false
No Zone
→ Motion works full-frame

EnableMotionZone = true
No Zone
→ Validation fails

EnableMotionZone = true
Valid Zone
→ PASS
```

---

## Storage Quota

```text
Quota:
2 GB

Used:
1.90 GB

New Clip:
150 MB

Expected:
Cleanup/check runs.

If insufficient:
Clip creation rejected/stopped according to policy.
Motion event metadata remains recorded.
```

---

## Retention

Create:

```text
Evidence A:
RetentionUntilUtc = past

Evidence B:
RetentionUntilUtc = future
```

Worker:

```text
Evidence A → delete
Evidence B → keep
```

---

## Security Incident

Simulate:

```text
Person Entered
→ Shelf Interaction
→ Pickup Candidate
→ No Put Back
→ No Matching Payment
→ Exit Crossed
```

Expected:

```text
Suspected Unpaid Exit Candidate
Security Incident
SignalR Alert
Evidence
Needs Human Review
```

---

# 37. Recommended Final Configuration for Your Example

```text
Tenant:
Bhavsar Office

Customer Admin:
bhavsarkrutarth88@gmail.com

Store:
Surat Main Office

Max Cameras:
5

Live Monitoring:
Enabled

Motion Rules:
Enabled

Detection Zone:
Optional

Default Detection:
Full Frame

Storage Quota:
2 GB

Default Evidence Retention:
15 Days

Auto Cleanup:
Enabled

Quota Warning:
80%

Quota Critical:
90%

Quota Pressure Policy:
Stop New Normal Motion Clips

Security Evidence:
Keep according to configured security retention

Motion Snapshot:
Enabled selectively

Normal General-Motion Video:
Disabled by default

Restricted Zone Video:
Enabled

Suspected Unpaid Exit Evidence:
Enabled
```

---

# 38. Recommended MVP Motion Rules

Do not enable every AI category from day one.

Start production implementation with:

```text
1. Person Detected
2. Entry Crossed
3. Exit Crossed
4. Dwell Threshold
5. Restricted Zone Entry
6. Camera Offline
7. Camera Occluded
```

Then add:

```text
8. Shelf Interaction
9. Pickup Candidate
10. Put-Back Candidate
11. Checkout Entered/Exited
12. Suspected Unpaid Exit
```

Reason:

Security/shoplifting logic depends on reliable person tracking, zones, POS correlation and evidence handling. It should not be built as a single simplistic motion rule.

---

# 39. Final Recommended Architecture

```text
                       PLATFORM ADMIN
                             │
                  Tenant Subscription/Quota
                             │
              Max Cameras = 5 / Storage = 2 GB
                             │
                             ▼
                       CUSTOMER ADMIN
                             │
                       Tenant Context
                             │
                ┌────────────┴────────────┐
                │                         │
          Camera Management       Storage Usage
                │                         │
           Max 5 Cameras                  │
                │                         │
                ▼                         │
       Live Camera Monitoring             │
                │                         │
         RTSP / Media Gateway             │
                │                         │
          Python CCTV AI                  │
                │                         │
       Person / Motion / Zones            │
                │                         │
         Motion Rule Engine               │
                │                         │
    ┌───────────┼──────────────┐          │
    │           │              │          │
 Entry/Exit   Dwell       Security Rule   │
    │           │              │          │
    └───────────┴──────┬───────┘          │
                       │                  │
                 Motion Event             │
                       │                  │
               Snapshot / Clip            │
                       │                  │
                       ▼                  │
               Evidence Storage ──────────┘
                       │
               Tenant Folder/Object
                       │
               Quota Enforcement
                       │
                RetentionUntilUtc
                       │
                       ▼
               CustSearch.Worker
                       │
              15-Day Auto Cleanup
                       │
                       ▼
                 Storage Freed
```

---

# 40. Final Decision

For the current CustSearch AI architecture, the recommended approach is:

1. Keep `MaxCameras` as the Platform/Tenant subscription camera authority.
2. Set the example tenant to maximum **5 active cameras**.
3. Customer Admin Live Monitoring shows only authorized cameras of the authenticated tenant/store.
4. Use a **rule-based motion engine**, not only one generic motion toggle.
5. Make Camera Zones **optional by default**.
6. If `EnableMotionZone = true`, require valid zone configuration.
7. Reuse existing Entry/Exit/Checkout/Shelf/Restricted/High-Value zone concepts.
8. Reuse existing security observation/incident architecture for shoplifting-related signals.
9. Use **event-based evidence storage**, not continuous recording for a 2 GB quota.
10. Give every Tenant its own storage namespace/folder.
11. Track storage quota/usage in SQL Server.
12. Set `RetentionUntilUtc` per evidence object.
13. Use Worker-based automatic retention cleanup.
14. Default normal motion evidence retention to **15 days**.
15. Do not let storage-full conditions stop live camera monitoring.
16. Do not delete important security evidence unexpectedly just to make room.
17. Use snapshots/short clips selectively according to rule severity.
18. Keep all tenant, store, camera and evidence authorization server-side.
