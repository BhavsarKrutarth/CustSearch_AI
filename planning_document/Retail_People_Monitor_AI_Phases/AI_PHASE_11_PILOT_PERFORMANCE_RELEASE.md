# AI Phase 11: Pilot Validation, Capacity and Retail Release

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

A measured tenant pilot demonstrates usable monitoring, reliable access control and recoverable operation.

Dependencies: 01-10 for the full proposed Retail release. A smaller anonymous People Monitor pilot can gate 01-04 earlier.

## Existing code to reuse

Existing automated API/UI/Python tests, operational workers, audit, alert delivery and evidence maintenance.

## Database work

- [ ] Validate upgrades on a staging copy and reconcile retention, event indexes, aggregates and storage accounting.
- [ ] Document backup/restore and forward-fix procedures; preserve existing finalized invoices and tenant data.

## .NET and API work

- [ ] Exercise revoked credentials/grants, ingestion replay, service outage, reconnect and authorization leases.
- [ ] Measure per-tenant event throughput, queue delay and recovery; introduce resource quotas based on measurements.
- [ ] Confirm alert/review/invoice paths remain available when AI is unavailable.

## Python work

- [ ] Benchmark camera resolution, sampled FPS, detector cost, tracker switches, CPU/GPU memory and simultaneous cameras on named hardware.
- [ ] Choose supported camera capacity and freshness targets from results; do not promise a fixed capacity from code inspection.
- [ ] Test prolonged disconnects, blocked capture, model failure, frame loss and process restart.

## Admin UI work

- [ ] Validate representative tenant roles and user grants with two or more isolated tenants.
- [ ] Exercise dashboard, camera monitor, enrollment/review, assistance and invoice flows at laptop/mobile sizes.
- [ ] Publish camera stale/offline states and runtime-versus-preview status clearly.

## Acceptance gate

- [ ] Record pass/fail evidence for tenant isolation, grant expiry, recovery, data retention and invoice allocation.
- [ ] Measure entry/exit accuracy, ID switches, occupancy definition, alert precision and recognition false accepts/rejects on held-out clips.
- [ ] Release checklist records unresolved defects, dataset/model versions, hardware, capacity and rollback drill.
- [ ] No phase is labelled complete merely because its page or endpoint exists.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Expansion Beyond Retail](AI_PHASE_12_MULTI_DOMAIN_EXPANSION.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
