# Post-Login Setup Guide

Use a dedicated non-production tenant/store for UAT. Every action should appear in the relevant
audit view and remain inaccessible to unrelated tenants.

1. Sign in as Platform Admin and immediately change the bootstrap password.
2. Configure non-secret platform settings; put secret values in the approved secret store.
3. Create/verify a subscription plan.
4. Create and activate the tenant and its subscription.
5. Create the Tenant Owner/Admin account and communicate credentials out of band.
6. Sign in as Tenant Admin and configure the tenant profile.
7. Create the store with canonical address, time zone and optional verified coordinates/geofence.
8. Create tenant users and assign least-privilege roles.
9. Assign authoritative stores; verify unassigned stores are denied.
10. Create staff profiles and operational shifts/presence settings.
11. Create store product categories and aliases.
12. Configure the dynamic voice trigger, language, confirmation and confidence behavior.
13. Configure alerts and notifications.
14. Configure integrations/webhooks using secret references, not raw secrets.
15. Configure cameras and zones; begin in CCTV Demo Mode.
16. Add customers/products and, where consent is lawful, explicit recognition consent/enrollment.
17. Create factual visits, verified households and retail invoices/payments.
18. Verify customer, household, billing and operational reports plus requester-bound exports.
19. Start Worker and verify heartbeats, leases, queue state and retention dry-run expectations.
20. Replace Demo Mode with real cameras only after calibration, privacy, retention and security review.

Phase 18 suspected-unpaid-exit screens are not available in the selected source branch. Do not use
the live-only security tables manually as a substitute for an authorized application workflow.
