# AI Phase 05: Staff-to-Customer Assistance Sessions

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

A staff member can start, transfer and close a confirmed customer assistance session before billing.

Dependencies: 01 plus existing customers, staff and store authorization. Can be delivered independently of 02-04.

## Existing code to reuse

Existing StaffProfiles, shifts/presence, Customers, CustomerStoreAssignments and visit records.

## Database work

- [ ] Extend the customer-only assistance proposal for anonymous VisitorSessions as specified in the [voice visitor/category plan](AI_VOICE_VISITOR_CATEGORY_INVOICE_PLAN.md). Use an explicit guest-or-customer-visit subject, nullable confirmed CustomerId, multiple category-interest rows, primary/supporting staff, target revisions and audited registration/transfer. Never create fake customer identities for unknown people.

- [ ] Add CustomerStaffAssistanceSessions with tenant/store/customer/staff, optional visit/track reference, state, start/end actors and times, source and concurrency token.
- [ ] Track transfers/participants as history. Define one primary active assistant per customer visit and explicit supporting assistants.
- [ ] Link camera tracks optionally; ordinary manual assistance must work without cameras or recognition.

## .NET and API work

- [ ] Implement staff/device target leases, atomic multi-category commands, idempotent add/remove/undo and target-switch/revocation checks from the [voice visitor plan](AI_VOICE_VISITOR_CATEGORY_INVOICE_PLAN.md). Preserve the existing registered-customer voice contract through a versioned migration.

- [ ] Add authorized start/list/transfer/end operations under /api/tenant/assistance-sessions.
- [ ] Validate staff active/store assignment and customer visibility; serialize simultaneous start/transfer attempts.
- [ ] Use separate permissions for self-service assistance, manager reassignment and history viewing.

## Python work

- [ ] No AI required to create a confirmed manual session.
- [ ] Later co-presence observations may suggest a pairing; keep suggestions separate from confirmed assignments and handle unknown/ambiguous tracks.

## Admin UI work

- [ ] Staff page: Assist Customer using customer code/phone/QR lookup, current queue, transfer and finish actions.
- [ ] Customer detail: current primary/supporting assistants and permitted assistance history.
- [ ] Show Suggested and Confirmed distinctly; do not imply that proximity proves customer service.

## Acceptance gate

- [ ] Concurrent starts cannot assign two primary assistants accidentally.
- [ ] Cross-tenant/store customer/staff combinations are rejected.
- [ ] Transfer retains the original history; staff without manager rights cannot take another user's session.
- [ ] Manual flow works when AI service is offline.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Invoice Creation with Salesperson Attribution](AI_PHASE_06_INVOICE_STAFF_ATTRIBUTION.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
