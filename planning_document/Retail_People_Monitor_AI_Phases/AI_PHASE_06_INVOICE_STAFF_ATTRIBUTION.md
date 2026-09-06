# AI Phase 06: Invoice Creation with Salesperson Attribution

Status: Planned — no implementation or test completion is asserted by this file.

[Phase index](AI_PHASE_00_ROADMAP.md) | [Master plan](../CustSearch_AI_Tenant_Vision_AI_Implementation_Plan.md)

## Outcome and dependencies

An invoice records the customer, confirmed assisting salesperson(s) and cashier/creator separately.

Dependencies: 05 and the existing retail invoice create/finalize/payment flows.

## Existing code to reuse

RetailInvoice.CustomerId, CustomerVisitId, CreatedByUserId, existing invoice participants/item customer attribution and server-calculated totals.

## Database work

- [ ] Apply the [voice-to-invoice context plan](AI_VOICE_VISITOR_CATEGORY_INVOICE_PLAN.md): persist requested-category snapshots and exact guest/assistance provenance separately from purchased item categories. Carry context into a draft automatically; actual SKU selection determines billable lines and server catalog categories. Refresh draft context explicitly and freeze it at finalization.

- [ ] Add RetailInvoiceStaffAttributions linked to tenant/store/invoice/staff and optional assistance session.
- [ ] Separate sales allocation from cashier/referral roles. Define an allocation base and amount/percentage rounding rules; cashier rows do not share the sales-allocation total.
- [ ] Preserve attribution snapshots on finalization; use audited adjustments/reversals for approved corrections, returns and cancellations.

## .NET and API work

- [ ] Extend draft invoice commands/details with validated assistance/session and salesperson attribution inputs.
- [ ] Resolve current user for CreatedByUserId. Never infer the salesperson solely from whoever created the invoice.
- [ ] Validate scope, active staff and session/customer/visit consistency; snapshot allocations atomically with finalization.
- [ ] Add sales-by-staff reporting that avoids double-counting shared sales and excludes/reverses cancelled records.

## Python work

- [ ] AI may suggest an assistance session for confirmation. Python does not select prices/taxes or finalize the invoice.
- [ ] Keep co-presence scores out of confirmed sales allocation calculations.

## Admin UI work

- [ ] Invoice editor: customer, optional assistance session, salesperson(s), cashier and allocation preview.
- [ ] Example: Ravi assists Customer C45; Meena creates the invoice. Record Ravi as salesperson and Meena as creator/cashier.
- [ ] Manager can confirm split allocation. Invoice detail/report shows allocation source and adjustment history.

## Acceptance gate

- [ ] Single assistant, multiple assistants, different cashier and invoice without assistance all work.
- [ ] Sales allocations cannot exceed the agreed base; rounding reconciles exactly and retries do not duplicate.
- [ ] Forged assistance/staff/customer IDs cannot cross store/tenant boundaries.
- [ ] Existing pricing, payment, customer item attribution and finalization tests remain valid.

## Handoff record

Fill in during implementation: changed files, migration identifier, API/event contract versions, test commands/results, representative fixture or pilot evidence, unresolved limitations and rollback steps.

Next planned phase: [Consent, Face Enrollment and Tenant Gallery](AI_PHASE_07_CONSENT_ENROLLMENT_GALLERY.md). Follow its dependency requirements; adjacent numbering alone does not authorize a rollout.
