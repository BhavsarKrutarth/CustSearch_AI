# AI Phase 09: Retail Categories, Object and Assistance Observations

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

Enabled retail capabilities generate reviewable zone/object observations and optional staff/customer assistance suggestions.

Dependencies: 02-04. Staff/customer identity suggestions additionally require confirmed identity or manual track associations.

## Existing code to reuse

Product/category catalog, store assignments, camera zones, security_observations.py and .NET security/alert/evidence workflows.

## Database work

- [ ] Add versioned AI vision taxonomy and explicit tenant category/zone mappings. Catalog category and model class are different concepts.
- [ ] Support retail profiles such as grocery, fashion, electronics and jewellery by configured capabilities; unsupported model classes remain unavailable.
- [ ] Persist deduplicated event/review references, model/rule version and attribution provenance.

## .NET and API work

- [ ] Validate object/action contracts, mappings and camera zone version before applying tenant alert rules.
- [ ] Reuse alert outbox, evidence authorization/quota and retention; do not introduce a second incident authority.
- [ ] AI co-presence creates an assistance suggestion for user confirmation; no automatic commission or invoice finalization.

## Python work

- [ ] Follow the optional [voice processing extension](AI_VOICE_VISITOR_CATEGORY_INVOICE_PLAN.md) for speech-to-text and bounded category-command parsing in a separate voice worker. Spoken category interests are staff-reported observations, not visual object detections or purchase evidence. .NET validates target, category IDs and writes.

- [ ] Implement supported generic object detection/tracking and temporal person/object/zone observations.
- [ ] Start with occupancy, dwell and queue proxies; validate the definitions and camera geometry with the store.
- [ ] Pickup/put-back and staff-handling suggestions require evaluated temporal evidence; proximity alone is insufficient.
- [ ] An exact SKU, price or purchase cannot be inferred merely from a generic product box; use POS/barcode catalog selection for invoicing.

## Admin UI work

- [ ] Tenant admin selects capabilities/categories/zones and thresholds the active pack actually supports.
- [ ] Live feed labels probable actions as observations; provide object/action review and evidence links.
- [ ] Staff assistance queue can accept/reject a suggestion; accepted session feeds phase 06.

## Acceptance gate

- [ ] Unknown model classes never map automatically to arbitrary products.
- [ ] Repeat observations within cooldown produce one alert; denied evidence grants block images/downloads.
- [ ] Crowds, occlusion, customers standing near unrelated staff and object put-back scenarios are evaluated.
- [ ] No billing amount or confirmed loss changes based only on a vision event.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Separate Python Training Project and Datasets](AI_PHASE_10_PYTHON_TRAINING_DATASETS.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
