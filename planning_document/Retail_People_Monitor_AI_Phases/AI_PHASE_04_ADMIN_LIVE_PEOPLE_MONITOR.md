# AI Phase 04: Admin Live People Monitor, Zones and Events

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

Users see authorized camera previews with aligned person overlays, zone activity, camera health and a recoverable event feed.

Dependencies: 03 and effective-access enforcement from 01.

## Existing code to reuse

Extend features/cameras/live-camera-monitoring-page.ts, cameras-api.service.ts, existing preview sessions and alert/outbox recovery patterns. Avoid creating a competing preview transport.

## Database work

- [ ] Add only necessary monitoring summary/recovery projections; retain lifecycle data and aggregate occupancy samples according to policy.
- [ ] Store camera occupancy separately from store-wide unique visitors. Overlapping camera views must not be summed as unique people.

## .NET and API work

- [ ] GET /api/tenant/ai-monitoring/summary, cameras/{id}/tracks and events with bounded cursor pagination.
- [ ] Serve an initial authorized snapshot and transient overlay updates; durable feed recovery uses sequence/cursor and reload fallback.
- [ ] If adding a new SignalR hub, update JWT hub-path handling, tenant/store/camera authorization, reconnect and deployment configuration.
- [ ] Use separate safe DTOs for anonymous monitoring and identity. Per-camera authorization is required even inside tenant/store groups.

## Python work

- [ ] Send normalized box coordinates plus source dimensions, capture/frame reference, stream epoch and observation timestamp.
- [ ] Throttle overlay delivery independently of inference and durable events; publish stale/degraded status when capture stalls.

## Admin UI work

- [ ] Add explicit Assist this visitor selection and authorized visit-interest chips per the [voice visitor plan](AI_VOICE_VISITOR_CATEGORY_INVOICE_PLAN.md). Bind a stable guest session through a confirmed selection; do not infer command recipients from proximity or newly reused track IDs. Interest access requires separate permissions from anonymous monitoring.

- [ ] Implement the [monitor box color addendum](AI_TENANT_PROFILE_ENRICHMENT_MONITOR_COLORS_ADDENDUM.md): new/unknown green, confirmed returning customer blue, confirmed staff purple; tenant color pickers, reset, legend, versioned updates and accessible labels. Returning/staff states depend on authorized identity bindings from Phase 08 or an explicit authorized manual association; anonymous-only viewers retain generic boxes.

- [ ] Add features/ai-monitoring dashboard, active-track list, event feed and camera-health/occupancy components.
- [ ] Draw boxes on a canvas aligned to the preview's actual content rectangle, including object-fit letterboxing, resize and fullscreen.
- [ ] Match overlay capture time to the preview frame. Hide or mark boxes stale rather than showing old boxes on newer frames.
- [ ] User flow: select permitted store, select cameras, inspect tracks/zones, open a permitted event/evidence detail.
- [ ] Limit concurrent tiles, reduce polling for hidden tabs, clear subscriptions/object URLs on navigation and permission changes.
- [ ] Preserve the dark theme; validate 1366x768, 1366x600, 1024x600 and narrow screens. Keep preview, filters and primary actions usable.

## Acceptance gate

- [ ] Boxes align at different aspect ratios, resizing/fullscreen and delayed frame delivery.
- [ ] Expired preview/user grants stop frames and overlays; reconnect cannot join unauthorized cameras.
- [ ] Lost events recover without duplicate entries; stale video and stale metadata are visibly distinguished.
- [ ] Camera count, occupancy and unique-visitor labels reflect their actual metric definitions.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Staff-to-Customer Assistance Sessions](AI_PHASE_05_STAFF_CUSTOMER_ASSISTANCE.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
