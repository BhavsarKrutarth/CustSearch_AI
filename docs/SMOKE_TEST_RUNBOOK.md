# Smoke Test Runbook

1. Read the all-phase execution checkpoint and confirm the audit branch.
2. Set the SQL connection and local signing secrets through environment variables.
3. Run the Phase 16 runner with `-ValidateIdempotency`.
4. Set a local `CUSTSEARCH_SMOKE_PASSWORD` and run the smoke-data wrapper twice.
5. Run `AllPhases_SmokeData_Verify.sql` and confirm `DBCC CHECKCONSTRAINTS` has no errors.
6. Start API/Worker/Angular and Python Demo Mode.
7. Execute invalid login, login, `/me`, refresh, logout and revoked refresh checks.
8. Validate Platform, Tenant Admin and Staff permissions/assigned stores.
9. Use Tenant A/Staff A credentials against Tenant B customer/store IDs; expect 404/403.
10. Exercise customer, household, visit, retail invoice/payment/preferences and reports through APIs.
11. Process notification/integration/export queues only with test adapters; never call the
    `example.invalid` smoke endpoint as a real provider.
12. Verify SignalR group denial/reconnect and authoritative REST recovery.
13. Use camera Demo Mode; do not enable recognition without explicit consent/key configuration.
14. Record command times, exit codes/status codes and observed counts in `SMOKE_TEST_RESULTS.md`.

Cleanup is optional and must use the exact confirmation token documented in the test-data README.
Review dependencies first and never adapt it into a broad tenant delete.
