# Tenant Vision AI: Phase Roadmap

Status: Planning only. Created 2026-09-06. Application behaviour is not changed by these files.

This phase set covers the requested Retail People Monitor, Python inference/training projects, .NET APIs, tenant Admin UI, user-wise AI access, retail categories and staff/customer invoice attribution.

Sources: [master implementation plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md) and [multi-domain vision document](../CustSearch_AI_Multi_Domain_AI_Training_Tracking_Planning.md).

## Execution files

| Phase | Scope | Main dependency |
|---|---|---|
| 01 | [Foundation, Tenant Profiles and User-wise AI Access](AI_PHASE_01_FOUNDATION_USER_AI_ACCESS.md) | Start here; reuse existing authentication, permission and camera-preview grants |
| 02 | [Model Registry and Controlled Deployment](AI_PHASE_02_MODEL_REGISTRY_DEPLOYMENT.md) | 01 |
| 03 | [Python Person Detection and Tracking Runtime](AI_PHASE_03_PYTHON_PERSON_TRACKING_RUNTIME.md) | 01 and 02 |
| 04 | [Admin Live People Monitor, Zones and Events](AI_PHASE_04_ADMIN_LIVE_PEOPLE_MONITOR.md) | 03 and effective-access enforcement from 01 |
| 05 | [Staff-to-Customer Assistance Sessions](AI_PHASE_05_STAFF_CUSTOMER_ASSISTANCE.md) | 01 plus existing customers, staff and store authorization |
| 06 | [Invoice Creation with Salesperson Attribution](AI_PHASE_06_INVOICE_STAFF_ATTRIBUTION.md) | 05 and the existing retail invoice create/finalize/payment flows |
| 07 | [Consent, Face Enrollment and Tenant Gallery](AI_PHASE_07_CONSENT_ENROLLMENT_GALLERY.md) | 01, 02 and a validated face model contract |
| 08 | [Stable Recognition Candidates and Reviewed Identity](AI_PHASE_08_RECOGNITION_REVIEW_IDENTITY.md) | 03, 04 and 07, with identity-view grants from 01 |
| 09 | [Retail Categories, Object and Assistance Observations](AI_PHASE_09_RETAIL_CATEGORIES_OBJECT_EVENTS.md) | 02-04 |
| 10 | [Separate Python Training Project and Datasets](AI_PHASE_10_PYTHON_TRAINING_DATASETS.md) | 02 and 09 taxonomy; authorized labelled pilot data must be available |
| 11 | [Pilot Validation, Capacity and Retail Release](AI_PHASE_11_PILOT_PERFORMANCE_RELEASE.md) | 01-10 for the full proposed Retail release |
| 12 | [Expansion Beyond Retail](AI_PHASE_12_MULTI_DOMAIN_EXPANSION.md) | Retail pilot gate in 11; build one additional domain at a time |

## Delivery milestones

### Tenant approval, profile connections and box colors

See the [tenant AI/profile/color addendum](AI_TENANT_PROFILE_ENRICHMENT_MONITOR_COLORS_ADDENDUM.md) for platform-admin capability approval, tenant-scoped provider credentials, voluntary social-profile connections, typed extra profile columns and tenant-customizable bounding-box colors (new/unknown green, returning customer blue, staff purple). Apply its Phase 01/04/07/08 extensions and acceptance checks. Public social identity lookup from unknown CCTV faces is outside scope; voluntary provider integrations are optional and do not block anonymous monitoring.

1. Anonymous People Monitor: phases 01-04 plus the relevant access, reliability and UI checks from 11. Real camera input produces anonymous tracks, zone events and authorized overlays.
2. Staff/customer billing: phases 05-06 after 01 and existing retail billing. This manual workflow can ship without AI recognition or training.
3. Optional reviewed identity: phases 07-08. Preserve active consent, model compatibility and per-user identity permissions.
4. Retail observations and custom training: phases 09-10 after validated tracking and suitable permitted data.
5. Retail release: phase 11 integrates the selected release scope. Additional domains follow phase 12.

## Shared implementation rules

The [voice visitor/category/invoice plan](AI_VOICE_VISITOR_CATEGORY_INVOICE_PLAN.md) extends phases 01, 04, 05, 06 and 09 with effective tenant/store voice settings, explicit guest assistance targets, multiple category interests per visit, optional Python speech processing and invoice context carry-forward. Implement it in its stated order; real audio and guest support are not already delivered by the existing text-based voice test screen.

- Tenant and acting user come from the authenticated server context. Browser-selected IDs are requested resources that must be validated, never authority to broaden scope.
- User access is the intersection of subscription, tenant/camera enablement, role permissions, camera/store scope and active user grant. Identity additionally requires the subject's active permitted consent and appropriate review policy.
- Reuse existing preview, camera grants, zones, consent/template/review, invoice and alert foundations. Add compatible contracts; do not rebuild the same feature under a second name.
- Python handles vision observations and never directly connects to SQL or creates/finalizes financial records. Heavy training runs separately from camera inference.
- Approved live frames and evidence may reach authorized viewers through the existing preview/evidence services. Embeddings and source credentials do not. Anonymous event messages contain no identity material.
- Canvas overlay coordinates must match the displayed frame's dimensions and time. Camera-local track IDs and occupancy do not automatically represent store-wide unique customers.
- Catalog categories, vision model classes and domain packs are separate mappings. Adding a business category does not train a model or make an unsupported class detectable.
- Invoices retain customer, salesperson allocation and cashier/creator separately. AI suggestions require confirmation; finalization and corrections follow the existing financial workflow.
- Use the existing dark theme and compact shell. Validate 1366x768, approximately 1366x600 usable height, 1024x600 and mobile layouts without hiding necessary fields.
- Select migration numbers against the current database upgrade history at implementation time; existing PHASE_01-18 plans are not renumbered.

## Progress tracking

| Phase | Status | Implementation commit | Evidence / blocker |
|---|---|---|---|
| 01 | Planned | — | Not started |
| 02 | Planned | — | Not started |
| 03 | Planned | — | Not started |
| 04 | Planned | — | Not started |
| 05 | Planned | — | Not started |
| 06 | Planned | — | Not started |
| 07 | Planned | — | Not started |
| 08 | Planned | — | Not started |
| 09 | Planned | — | Not started |
| 10 | Planned | — | Not started |
| 11 | Planned | — | Not started |
| 12 | Planned | — | Not started |

Mark complete only after its acceptance checks pass on the declared environment. Existing source files are evidence of foundation, not proof of production accuracy or deployment readiness.

## Planning caveats and decisions

The older master document's approximate percentages and schedules are preliminary judgments, not measured completion metrics or delivery commitments. Estimate each phase after selecting the first store, camera footage, hardware, model artifact, team and acceptance dataset. Dataset collection/annotation and field evaluation are separate work.

These phase files refine the master plan where needed: establish a minimal model registry before production inference; permit manual assistance/invoices independently; distinguish authorized preview images from biometric data; implement reviewed identity binding explicitly; keep per-frame overlays transient; enforce grant revocation for already-connected clients.

Before a phase starts, record its selected model/license/artifact contract, target hardware and store/camera scope where relevant, supported event/version migration, and measurable acceptance targets. Pending choices should not block independent work on authorization and manual assistance.
