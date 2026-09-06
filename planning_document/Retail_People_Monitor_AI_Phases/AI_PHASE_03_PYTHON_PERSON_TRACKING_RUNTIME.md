# AI Phase 03: Python Person Detection and Tracking Runtime

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

An authorized camera produces actual person boxes, stable camera-local tracks, zone transitions and measured runtime health.

Dependencies: 01 and 02. This is inference work; custom training is not required for the first validated detector.

## Existing code to reuse

Preserve app/main.py, camera_source.py, tracking.py and vision_runtime.py contracts until compatibility tests permit a migration. The existing tracking.py normalizes supplied tracks; it is not a tracking algorithm.

## Database work

- [ ] Reuse PersonTrackSessions, CameraOperationalEvents and zone configuration for durable lifecycle data.
- [ ] Define additive mappings/version changes before introducing new lifecycle event types. Keep high-frequency boxes out of durable per-frame SQL writes.

## .NET and API work

- [ ] Implement internal runtime-manifest, heartbeat and versioned monitoring ingestion endpoints.
- [ ] Authenticate services and resolve RuntimeCameraId to authorized tenant/store/camera; cross-check every received context field.
- [ ] Define idempotency, sequence, captured timestamp, camera stream epoch and model/tracker versions. Reuse existing ingestion patterns without weakening anonymous Phase 13 validation.

## Python work

- [ ] Add contracts/, inference/person_detector.py, tracking/person_tracker.py, zones/zone_engine.py, runtime/camera_supervisor.py and events/delivery.py.
- [ ] Resolve the existing tracking.py versus tracking/ package name collision through an explicit import migration; do the same for camera_source.py moves.
- [ ] Implement model-specific resize/letterbox, channel order, normalization, inference, output decode and suppression; test coordinate transforms.
- [ ] Use bounded per-camera capture/inference queues, frame sampling, reconnect backoff and retry limits. Shared model sessions must be concurrency-safe.
- [ ] Track identity key includes camera/stream epoch/local ID; do not assume local Track 1 across two cameras is the same person.
- [ ] Persist lifecycle events with replay-safe IDs; transient overlays can be dropped when stale. Flush ended/lost state and support clean shutdown.
- [ ] Baseline camera events: online/degraded/offline; person events: entered/updated/lost/exited/zone-entered/zone-exited; occupancy snapshots carry scope and timestamp.

## Admin UI work

- [ ] Expose worker state and active deployment version in the AI profile page while the full monitor is built in 04.
- [ ] Keep demo fixtures clearly identified.

## Acceptance gate

- [ ] Recorded footage produces real boxes and tracked paths; deterministic normalization fixtures alone do not pass.
- [ ] Track survives agreed short occlusion; restart cannot reuse an earlier persistent identity.
- [ ] One blocked/offline camera does not stall others; retry retains lifecycle event IDs.
- [ ] No secret source URL or biometric material appears in events/logs; profile revocation stops processing.
- [ ] Measure FPS, latency, dropped frames and track switches on representative pilot clips; record hardware and model version.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Admin Live People Monitor, Zones and Events](AI_PHASE_04_ADMIN_LIVE_PEOPLE_MONITOR.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
