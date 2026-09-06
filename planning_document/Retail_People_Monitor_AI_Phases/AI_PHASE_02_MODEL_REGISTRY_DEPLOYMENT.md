# AI Phase 02: Model Registry and Controlled Deployment

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

Operators can register, verify, deploy and roll back a person-detection model with a recorded version.

Dependencies: 01. Required before monitored cameras load production model artifacts.

## Existing code to reuse

Extend the existing ONNX loading boundary in src/CustSearch.AI/app/vision_runtime.py; reuse platform authorization and audit services.

## Database work

- [ ] Add AiModels, AiModelArtifacts, AiModelPacks, AiModelPackCapabilities and AiModelDeployments.
- [ ] Record model type/version, immutable artifact reference, checksum, input/output schema, preprocessing specification, class-map version, engine compatibility and deployment history.
- [ ] A deployment can target an authorized tenant/store/camera; keep last-known-good artifact and atomic activation state.

## .NET and API work

- [ ] Platform-only registry/upload-reference, validate, approve, deploy and rollback endpoints.
- [ ] Runtime manifest returns an authorized artifact reference, checksum, model contract and deployment version.
- [ ] Reject arbitrary runtime URLs and incompatible artifacts. Keep model governance permissions separate from tenant identity access.

## Python work

- [ ] Implement inference/model_loader.py with checksum verification, input/output validation and warmup.
- [ ] Load a candidate alongside the active model; activate only after readiness succeeds, otherwise keep the old version.
- [ ] Declare supported execution provider, memory requirement and incompatible tracker-state reset behaviour.

## Admin UI work

- [ ] Platform Model Registry and Deployment Status pages; tenant users get read-only active version/health.
- [ ] Show Requested, Downloading, Warming, Active, Failed and Rolled back states.

## Acceptance gate

- [ ] Corrupt checksum and incompatible output schema cannot activate.
- [ ] A failed rollout preserves the active model; rollback is recorded and affects only the selected scope.
- [ ] Tenant users cannot publish model artifacts or view another tenant's deployments.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Python Person Detection and Tracking Runtime](AI_PHASE_03_PYTHON_PERSON_TRACKING_RUNTIME.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
