# AI Phase 08: Stable Recognition Candidates and Reviewed Identity

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

Live tracks can produce consent-gated candidates; permitted reviewers confirm identity and authorized viewers see the reviewed label.

Dependencies: 03, 04 and 07, with identity-view grants from 01.

## Existing code to reuse

Existing candidate creation/review workflow. ReviewAsync currently records a decision with TrackAssociationChanged=false; identity binding requires an explicit implementation.

## Database work

- [ ] Record candidate/gallery/model versions, quality and competing similarity, scoped track reference and review audit.
- [ ] Add or extend an explicit reviewed track identity binding with effective interval and provenance. Preserve anonymous observations.
- [ ] Define consent-expiry and withdrawal effects on active display/bindings and historical records.

## .NET and API work

- [ ] Derive authorized monitoring states using the [profile/color addendum](AI_TENANT_PROFILE_ENRICHMENT_MONITOR_COLORS_ADDENDUM.md): returning requires a confirmed customer and prior tenant visit; staff requires an active confirmed staff association. Unknown is not proof of a first visit. Strip identity-derived color categories from anonymous DTOs and clear them on binding/consent/grant revocation.

- [ ] Add authenticated service-result ingestion separate from anonymous CCTV events; do not give Python a tenant admin browser token.
- [ ] Resolve scope and active consent on receipt, review, association and identity read; use an idempotent review-to-binding transaction.
- [ ] Identity DTOs require AiIdentity.View plus camera/store grants. Do not send hidden identity fields in ordinary track messages.

## Python work

- [ ] Sample good faces per track, search only the scoped gallery, apply model-specific configurable thresholds and temporal stabilization.
- [ ] Emit Unknown/Candidate/NeedsReview/ConsentBlocked/RecognitionDisabled as appropriate; do not turn a similarity such as 0.89 into an asserted 89% correctness probability.
- [ ] Re-evaluate after gallery/profile/model change and track identity uncertainty.

## Admin UI work

- [ ] Review queue with evidence access, model score, quality, competing candidate and reason-required accept/reject.
- [ ] Monitor displays candidate as unconfirmed; reviewed name only when policy/grant/consent allow.
- [ ] Recognition-assisted customer selection remains an explicit user action for assistance/invoice workflows.

## Acceptance gate

- [ ] Review and binding cannot race consent withdrawal or overwrite a different confirmed identity silently.
- [ ] Anonymous viewers receive no name/customer ID in payloads or recovery responses.
- [ ] Ambiguous/poor quality samples stay unconfirmed; labels clear when permission/consent expires.
- [ ] False matches, unknown people and multiple faces are included in pilot evaluation.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Retail Categories, Object and Assistance Observations](AI_PHASE_09_RETAIL_CATEGORIES_OBJECT_EVENTS.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
