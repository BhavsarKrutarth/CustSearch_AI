# AI Phase 01: Foundation, Tenant Profiles and User-wise AI Access

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

A tenant admin can enable approved AI features and grant each user access to specific stores/cameras. Viewing anonymous tracks and viewing identity are separate actions.

Dependencies: Start here; reuse existing authentication, permission and camera-preview grants.

## Existing code to reuse

Reuse AdminShell, permission guards, CameraUserPreviewGrants, server-issued tenant/store context and the existing permission catalog.

## Database work

- [ ] Apply the [tenant approval/provider configuration addendum](AI_TENANT_PROFILE_ENRICHMENT_MONITOR_COLORS_ADDENDUM.md): platform-admin capability approvals, tenant enablement, scoped provider secret references, rotation/revocation and effective-access checks. Approval and a key alone never authorize subject profile imports.

- [ ] Add versioned TenantAiProfiles and UserAiAccessGrants with tenant/user/store/camera scope, validity period, actor, reason and concurrency token.
- [ ] Preserve existing preview grants. New grants cannot broaden subscription, role, store or camera permissions. Default new AI privileges to denied.
- [ ] Use composite tenant/store ownership constraints and indexed effective-access lookups; select the next unused database upgrade number during implementation.

## .NET and API work

- [ ] Add Application/AiMonitoring contracts and an effective-access resolver in Infrastructure/AiMonitoring.
- [ ] GET /api/tenant/ai-monitoring/access; authorized profile/grant management endpoints under the same namespace.
- [ ] Resolve tenant and user from the session. Validate target staff/user/store/camera ownership server-side.
- [ ] Recheck permissions for REST and active realtime sessions; publish revocation/disconnect signals and enforce short authorization leases.

## Python work

- [ ] Define CameraRuntimeManifest v1, service credential scope and signed request/reply validation.
- [ ] Use short-lived server-authorized manifests; deny unassigned camera IDs, expired manifests and unsupported contract versions.

## Admin UI work

- [ ] Apply the [voice settings plan](AI_VOICE_VISITOR_CATEGORY_INVOICE_PLAN.md): tenant defaults/store overrides, effective voice permissions, paired staff-device target leases, settings version/revocation and atomic settings saves. Voice capture/model deployment health is separate from configuration being saved.

- [ ] Add features/ai-monitoring models/API client, AI Profile and User AI Access pages.
- [ ] Show separate options for preview, anonymous tracks, identity, enrollment, review, configuration, evidence and alerts.
- [ ] Keep profile enablement distinct from user authorization; explain why an unavailable capability is disabled.

## Acceptance gate

- [ ] Tenant A cannot read/change Tenant B's profile or grants, including guessed IDs.
- [ ] Role, subscription, profile, camera grant and user grant are intersected; an expired/revoked grant stops active delivery within the authorization lease.
- [ ] Anonymous-only viewers never receive identity fields, even when the UI is bypassed.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Model Registry and Controlled Deployment](AI_PHASE_02_MODEL_REGISTRY_DEPLOYMENT.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
