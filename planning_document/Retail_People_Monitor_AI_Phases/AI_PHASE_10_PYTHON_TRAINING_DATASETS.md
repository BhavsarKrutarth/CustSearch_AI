# AI Phase 10: Separate Python Training Project and Datasets

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

A reproducible offline training job produces an evaluated immutable artifact eligible for controlled deployment.

Dependencies: 02 and 09 taxonomy; authorized labelled pilot data must be available.

## Existing code to reuse

Inference project remains src/CustSearch.AI; create src/CustSearch.AI.Training for offline training. Reuse model registry and tenant storage patterns without mixing production evidence and training access.

## Database work

- [ ] Add dataset manifests/versions, annotation revisions, training jobs, evaluation records and artifact provenance.
- [ ] Record dataset owner/scope, permitted usage, retention, source hashes, split manifests, model baseline and parameter/seed/environment versions.
- [ ] Training inclusion requires separate permitted use; operational evidence or recognition consent does not automatically authorize training.

## .NET and API work

- [ ] Platform-governed dataset import, validation, job submit/cancel, evaluation review and model registration APIs.
- [ ] Use queued training jobs with resource limits, progress and retry policy; production camera workers do not execute training.
- [ ] Prevent cross-tenant dataset mixing unless explicit scope/rights permit it.

## Python work

- [ ] Structure training project as datasets/, taxonomy/, labeling/, trainers/, evaluation/, export/ and scripts/ with pinned dependencies.
- [ ] First support reproducible offline execution; track arguments, environment and artifact hashes.
- [ ] Split by site/camera/time/subject where appropriate to avoid neighbouring frame leakage.
- [ ] Record per-class precision/recall and relevant detection/tracking metrics, false positives, lighting/crowd/occlusion results.
- [ ] Export ONNX and compare predictions/accuracy to the training runtime before registry submission.

## Admin UI work

- [ ] Platform Training Jobs and Dataset Versions pages with lineage, progress, failures and evaluation comparisons.
- [ ] Tenant users see only permitted contribution/job summaries; enable controls only when backend support exists.

## Acceptance gate

- [ ] The same pinned inputs/configuration reproduce results within declared tolerance.
- [ ] Held-out pilot test data is excluded from training and threshold tuning.
- [ ] Cancelled/failed jobs do not publish artifacts; ONNX contract/parity and checksum checks pass.
- [ ] Promotion requires recorded evaluation and an available rollback model.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Pilot Validation, Capacity and Retail Release](AI_PHASE_11_PILOT_PERFORMANCE_RELEASE.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
