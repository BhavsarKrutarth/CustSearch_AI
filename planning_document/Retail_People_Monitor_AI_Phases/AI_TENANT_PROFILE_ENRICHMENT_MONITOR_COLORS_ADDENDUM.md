# Tenant AI Approval, Voluntary Profile Connections and Monitor Colors

Status: Planned only. Added 2026-09-06; no application, schema, training or deployment changes are asserted.

[Roadmap](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Scope

Extend tenant/user AI access, voluntary customer/staff profile connections and monitoring bounding-box colors. This is a configuration and integration feature; a new vision model is not required for colors or connected profile fields.

Unknown CCTV faces must not be searched against Google, Facebook or other public/social profiles to identify people or collect their details. Admin approval and an API key do not authorize that behavior. The supported alternative is a person voluntarily connecting an account through an approved provider flow, or sharing profile links/details through a tenant enrollment form. Existing face recognition remains limited to the consented tenant gallery in phases 07-08.

## Tenant approval and credentials (Phase 01)

- [ ] Add per-capability approval states: NotRequested, Pending, Approved, Rejected, Suspended and Revoked. Record requesting tenant/user, deciding platform admin, timestamps, reason and version.
- [ ] Platform admin approval permits the tenant admin to enable that capability; tenant enablement and individual user/store/camera grants remain separate. Default capabilities to off.
- [ ] Effective access intersects subscription, platform approval, tenant enablement, role, user grant and resource scope. Profile imports additionally require subject consent and a supported provider connection.
- [ ] Configure tenant-scoped provider connections with provider ID, purpose, enabled state, allowed fields/scopes, connection health, quota and secret reference. API keys are required only for providers that use them; a generic AI key does not grant access to social accounts.
- [ ] Keep credentials encrypted in a server-side secret store. Return masked status only; never expose secrets in browser responses, monitoring events, logs or training datasets. Support rotation/revocation and audited configuration changes.
- [ ] Suspend queued work and stop active access on approval, consent, key or grant revocation. Recheck scope before provider calls and before storing results.

## Voluntary profile connections (extension after Phases 01 and 07)

- [ ] Let a customer/staff member voluntarily connect a supported account, or submit their own profile URL/basic details. Validate customer and staff subject ownership separately.
- [ ] Use a provider adapter registry so additional platforms can be added without changing the customer table for every platform. Initial provider choices and actual available fields must be verified against official provider documentation during implementation.
- [ ] Do not promise support for every social network or discovery of all accounts belonging to a person. Each integration depends on available APIs, permitted scopes and the person's explicit connection. Manually shared links remain marked unverified until verified through a supported flow.
- [ ] Import only allowed and returned fields, such as display name, profile URL, avatar reference and explicitly shared contact fields. Missing email/phone/location remains empty; do not infer or fabricate details.
- [ ] Show a field preview and let the person approve the selected fields before importing. Record provider/source, consent, import time and verification status per field. Existing verified values are not silently overwritten; conflicts require review.
- [ ] Display multiple connected profiles in the authorized customer/staff profile page. Provide disconnect, refresh and deletion/retention actions. Social-profile consent is separate from face enrollment and training permission.
- [ ] Keep all profile enrichment out of anonymous track messages. No automatic social lookup or customer creation is triggered by an Unknown track.

## Proposed data model and extra fields

These are logical schema proposals. Reuse suitable existing tables, validate current conventions and choose unused migration numbers during implementation.

| Entity | Proposed fields / responsibility |
|---|---|
| TenantAiCapabilityApprovals | TenantId, CapabilityKey, Status, RequestedBy, RequestedUtc, DecidedBy, DecidedUtc, Reason, RowVersion |
| TenantAiProviderConnections | TenantId, ProviderKey, Purpose, SecretReference, AllowedScopes, Enabled, HealthStatus, LastValidatedUtc, RowVersion |
| SubjectProfileConsents | TenantId, SubjectType, SubjectId, ProviderKey, Purpose, AllowedFields, GrantedUtc, ExpiresUtc, RevokedUtc, ConsentVersion |
| SubjectSocialProfiles | TenantId, SubjectType, SubjectId, ProviderKey, ProviderSubjectId nullable, ProfileUrl, DisplayName, AvatarReference, Source, VerificationStatus, ConsentId, ImportedUtc, LastRefreshedUtc |
| TenantProfileFieldDefinitions | TenantId, FieldKey, Label, DataType, ValidationRules, AllowedSources, VisibilityPermission, RetentionPolicy, Enabled |
| SubjectProfileFieldValues | TenantId, SubjectType, SubjectId, FieldDefinitionId, TypedValue, SourceProfileId nullable, ConsentId, VerificationStatus, UpdatedUtc |
| TenantMonitoringStyles | TenantId, NewPersonColor, ReturningCustomerColor, StaffColor, UnknownColor, CandidateColor, StyleVersion, UpdatedBy, UpdatedUtc, RowVersion |

- [ ] Use typed tenant-defined extra fields for additional provider attributes; expose them as configurable profile-table columns. Do not execute arbitrary ALTER TABLE for each imported attribute.
- [ ] Enforce tenant ownership on every subject, consent, provider profile and field reference; validate type, length, URL scheme, field count and payload size. Provider account IDs are namespaced by provider/connection context.
- [ ] Treat imported strings and URLs as untrusted input; encode display content and prevent arbitrary server fetches from submitted profile/avatar URLs.
- [ ] Audit import/merge/delete/configuration actions without recording secrets or unnecessary raw provider payloads.

## Monitoring bounding-box colors (Phase 04; reviewed roles from Phase 08)

These colors apply to person bounding-box borders and matching legend badges, not to the whole video or page background.

| Display category | Default color | Meaning |
|---|---|---|
| New / unidentified person | Green `#22C55E` | Newly observed anonymous person or confirmed first-time customer; Unknown label remains explicit when identity is not known |
| Returning customer | Blue `#3B82F6` | Confirmed tenant customer with a recorded visit before the current visit |
| Staff | Purple `#A855F7` | Explicitly bound active tenant staff identity, based on permitted reviewed recognition or an authorized manual association |
| Pending identity review | Amber `#F59E0B` | Unconfirmed candidate; do not display a confirmed name or returning/staff status |

- [ ] Offer tenant admin color pickers, live preview, reset-to-default and a visible legend. Unknown defaults to the same green as New but may be configured independently.
- [ ] Define NewObserved, Unknown, NewCustomer, ReturningCustomer, Staff and Candidate as distinct semantic states even when they share a color. Unknown does not prove the person has never visited before. Reconnects/new tracker IDs do not establish first visits or returning identity.
- [ ] Determine returning status from an authorized confirmed identity and a prior recorded tenant visit, excluding the current visit. Track IDs and similarity scores alone are insufficient.
- [ ] Display precedence: confirmed active Staff, confirmed ReturningCustomer, confirmed NewCustomer, Candidate, then Unknown/NewObserved. Clear confirmed categories when binding, consent or access becomes invalid.
- [ ] For anonymous-only viewers, strip identity and identity-derived roles/visit status server-side; show generic green boxes and anonymous track labels. Staff/returning colors must not disclose hidden identity information.
- [ ] Python emits observations, not tenant branding. The .NET service derives permitted semantic states; the UI applies the authorized tenant palette to matching frame/track boxes.
- [ ] Store validated opaque hex colors with concurrency/version checks. Warn about indistinguishable selections and preserve text labels, line patterns and a high-contrast border so color is not the only cue.
- [ ] Propagate palette changes to permitted active monitors through a versioned settings event/snapshot refresh without restarting inference. Tenant A's palette must never affect Tenant B.

## API and UI planning

- [ ] Platform approval actions under platform AI capability administration; tenant settings show approval, enablement and provider health separately.
- [ ] Tenant endpoints for provider configuration/test/rotate/disconnect, voluntary profile connection/import preview/confirmation and extra-field definition/value management. Return safe DTOs with bounded pagination and field-level permissions.
- [ ] Proposed GET/PUT `/api/tenant/ai-monitoring/styles` with a dedicated configure permission, server-derived tenant context, optimistic concurrency and reset action.
- [ ] Add sections for AI approval status, provider connections, profile connections/extra columns and Monitoring Box Colors to the existing tenant/customer/staff screens.
- [ ] Queue provider work separately from camera inference; use bounded retries, rate limits, idempotency and per-tenant budgets. Provider failure must not interrupt camera monitoring.

## Acceptance checks and delivery

- [ ] Phase 01: approval-off, revoked, invalid credentials and wrong-tenant requests are denied; API keys alone never enable a capability.
- [ ] Phase 04: default new/unknown green, returning blue and staff purple render on aligned boxes; tenant customization persists, resets and updates active sessions without cross-tenant leakage.
- [ ] Phase 08: returning/staff colors require valid confirmed associations; ambiguous faces remain unknown/candidates and anonymous viewers receive no role/visit-history information.
- [ ] Profile extension: no import occurs without voluntary connection/sharing and scoped consent; an Unknown detection never calls a social provider.
- [ ] Provider integrations handle unavailable fields, unverified links, expired tokens, duplicate callbacks, conflicting values and disconnect/deletion correctly.
- [ ] Extra fields enforce tenant/type/visibility limits and do not cause dynamic physical schema changes. Revocation is checked for delayed jobs and cached results.

Implement approval foundations first; colors can ship with anonymous monitoring before recognition. Add reviewed customer/staff states with phase 08. Deliver each voluntary provider adapter independently after its actual capabilities are verified. This addendum does not add a mandatory social integration dependency to the basic monitoring milestone.
