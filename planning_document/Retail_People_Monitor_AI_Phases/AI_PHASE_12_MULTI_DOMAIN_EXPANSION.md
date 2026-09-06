# AI Phase 12: Expansion Beyond Retail

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

A new business domain can reuse capture, tracking, tenant access, model lifecycle and monitoring while enabling its own evaluated capabilities.

Dependencies: Retail pilot gate in 11; build one additional domain at a time.

## Existing code to reuse

Shared core, domain pack registry, tenant profiles, vision taxonomy, review workflow and training project.

## Database work

- [ ] Extend domain/capability/category registries by versioned data and migrations where necessary.
- [ ] Keep grocery/fashion/jewellery as retail business profiles; warehouse/factory/office/restaurant/parking are broader domain packs.
- [ ] Allow multiple packs for a tenant only with explicit store/camera assignments and resource budgets.

## .NET and API work

- [ ] Resolve capabilities per camera manifest and validate domain-specific rule contracts.
- [ ] Introduce domain reports without forcing retail invoice/checkout assumptions on every business.

## Python work

- [ ] Load only required domain models and mappings; keep shared capture/tracking interfaces stable.
- [ ] Add and evaluate one pack: e.g. warehouse forklift/pallet/zone observations or factory PPE observations.
- [ ] Cross-camera continuity needs a separate evaluated identity/handoff design; prohibit cross-tenant linkage.

## Admin UI work

- [ ] Show pack-aware settings, monitoring labels and review tasks based on subscription and user grants.
- [ ] Tenant admin assigns approved packs to permitted stores/cameras; platform admin governs versions and rollout.

## Acceptance gate

- [ ] A warehouse camera cannot expose retail-only unsupported controls.
- [ ] New model/category mappings cannot silently reinterpret older events.
- [ ] Shared Retail regression tests pass; domain-specific held-out pilot results meet agreed acceptance gates.
- [ ] Concurrent packs meet measured worker capacity and maintain tenant isolation.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Expansion remains iterative: approve a new domain scope and acceptance dataset before scheduling its implementation.
