# All-Phase Smoke Data

This directory creates a deterministic, connected UAT tenant/store for implemented Phases 1–16
plus a minimal second tenant/store/customer for cross-tenant denial tests.
It never deletes or modifies unrelated records and does not seed biometric templates or Phase 18
security incidents. IDs are discovered from stable smoke codes; reruns are guarded.

```powershell
$env:CUSTSEARCH_SMOKE_PASSWORD = '<local-strong-password>'
./database/10_TestData/run-smoke-data.ps1 -ServerInstance 'KRUTARTH-BHAVSA'
sqlcmd -S 'KRUTARTH-BHAVSA' -d 'CustSearch_AI' -E -C -b -i database/10_TestData/AllPhases_SmokeData_Verify.sql
```

Reusable usernames are `smoke.platform`, `smoke.tenantadmin`, and `smoke.staff`; their password is
only the local environment value supplied to the runner. The API login contract uses `UserName`, not
email: tenant users must also send `SMOKE-TENANT-001`, while the platform user leaves `TenantCode`
empty. A rerun rotates only the deterministic smoke accounts to the newly supplied value.
The isolation login is `smoke.tenantbadmin@custsearch.local` (username `smoke.tenantbadmin`) and is
rotated to the same local smoke password.

Cleanup is opt-in and targets only deterministic smoke identifiers. Review it before use:

```powershell
sqlcmd -S 'KRUTARTH-BHAVSA' -d 'CustSearch_AI' -E -C -b `
  -v ConfirmCleanup='DELETE-SMOKE-TENANT-001' `
  -i database/10_TestData/AllPhases_SmokeData_Cleanup.sql
```

Do not use broad tenant deletes or manual cascading cleanup on a shared database.
