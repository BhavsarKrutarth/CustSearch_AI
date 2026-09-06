# AI Phase 07: Consent, Face Enrollment and Tenant Gallery

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

A permitted user can collect authorized enrollment samples, receive quality feedback and manage revocable templates.

Dependencies: 01, 02 and a validated face model contract. Customer identity first; staff identity requires an explicit equivalent schema/policy extension.

## Existing code to reuse

RecognitionController, RecognitionService, CustomerRecognitionConsents, BiometricTemplates and encrypted-template protector. Existing enrollment accepts derived bytes; a photo-to-embedding pipeline is additional work.

## Database work

- [ ] Version templates by face model/embedding schema, dimension, purpose and subject scope.
- [ ] Existing active-template constraints permit one current template per store/customer; design a versioned aggregate or sample child table for multiple samples rather than inserting conflicting active rows.
- [ ] Add gallery version/invalidation records. Customer-only consent/template schema must not be silently reused for staff IDs.

## .NET and API work

- [ ] Add bounded authorized enrollment upload/capture sessions with content validation, quality processing and progress.
- [ ] Return safe quality/template metadata; keep raw vectors within the protected service boundary.
- [ ] Implement authenticated gallery retrieval/decryption boundary with purpose/scope checks and revocation notifications; current protection-only interface needs a reviewed read design.
- [ ] Recheck consent during extraction, activation and gallery publication; define temporary sample deletion and encryption/key rotation.

## Python work

- [ ] Add face detection, alignment, quality validation and embedding generation using a pinned compatible model.
- [ ] Maintain tenant/store/purpose/model gallery cache with version and expiry; rebuild or invalidate on template/consent changes.
- [ ] Do not retrain a face network when adding a customer.

## Admin UI work

- [ ] Apply the optional [voluntary profile connection addendum](AI_TENANT_PROFILE_ENRICHMENT_MONITOR_COLORS_ADDENDUM.md): separate profile-sharing consent, provider import preview/confirmation, multiple shared/connected profiles and typed tenant-defined extra columns. Social-profile connection does not grant face enrollment or training permission; never use unknown CCTV faces for public profile discovery.

- [ ] Consent-first enrollment wizard, capture/sample quality feedback, active template metadata, revoke and re-enroll actions.
- [ ] Raw embeddings never appear in the UI. Authorized preview/evidence images remain separate from biometric template metadata.

## Acceptance gate

- [ ] Expired/withdrawn consent cannot activate or retrieve a template; cached entries expire/invalidate within a defined bound.
- [ ] Tenant A never searches Tenant B's gallery; two incompatible embedding versions cannot be mixed.
- [ ] Malformed/oversized samples fail safely; temporary samples and revoked template material follow retention rules.
- [ ] Staff enrollment remains unavailable until staff-specific consent, identity and authorization are implemented.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Stable Recognition Candidates and Reviewed Identity](AI_PHASE_08_RECOGNITION_REVIEW_IDENTITY.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
