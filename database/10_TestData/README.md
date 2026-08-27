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

## Tenant TEN-35D77F00D7F0 page and camera UAT data

`Tenant35D77F_UatData.sql` adds repeat-safe connected records for the existing active
`smoke.platform` tenant administrator. It covers store/dashboard, customer, household, visit,
visit-party, category, voice audit, product, retail, alert, integration, camera, consent and report
views. It also assigns that user to the UAT store and grants the opaque office camera live preview.

The script does not create or reset credentials, does not store an RTSP URL, and does not create a
biometric template. Run the seed plus verifier with Windows authentication:

```powershell
& database/10_TestData/run-tenant35d77f-uat.ps1
```

The camera row uses `env:CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP`. Configure the matching environment
secret only on the Python AI host and restart that process before physical live-preview validation.
