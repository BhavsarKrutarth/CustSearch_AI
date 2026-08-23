# Phase 5 — Tenant Users, Stores, Shop Owner & Staff

Status: In Progress
Started/continued: 2026-08-23 (Asia/Kolkata)
Source of truth: `../CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`

## Scope

Tenant users/roles, ShopOwner/TenantOwner, staff profiles and store assignments, operational shifts/presence signals, stores/quotas/categories, canonical address/map coordinates/geofence/time-zone/location verification, dynamic per-store voice configuration and the Customer Admin dashboard base.

## Sub-phase Progress

| Sub-phase | Scope | Status | Evidence / remaining gate |
|---|---|---|---|
| 5A | Tenant user CRUD, TenantOwner/ShopOwner roles, role assignment | Implemented | API/service/UI exist; security decorator now blocks store-scoped tenant-wide role escalation. Final CI pending. |
| 5B | User-store assignment, authoritative StoreIds, user/store quotas | Implemented | Store assignment is server-authoritative; reactivation quota bypass is fixed at service boundary. Final CI/SQL pending. |
| 5C | Store CRUD, canonical address, coordinates, geofence, time zone, location verification, lifecycle | Implemented | Backend + Angular create/edit/verify/activate/deactivate controls. SQL V1.4.0 + canonical merge pending CI execution. |
| 5D | Staff CRUD, store assignment, shift/presence operational APIs | Implemented | Staff CRUD + shift/presence commands exist. Staff detail route added. Presence remains informational, not payroll/employment-decision truth. Final CI pending. |
| 5E | Store category taxonomy | Implemented | Tenant/store-scoped category APIs/UI; decorator prevents cross-store category access. Final CI pending. |
| 5F | Dynamic per-store voice trigger, aliases, confirmation settings | Implemented | `Aasha Add` remains default/example only and is configurable per store. Actual speech-to-preference processing remains Phase 10. |
| 5G | Customer Admin dashboard base | Implemented | Live tenant summary endpoint/UI exists. Final CI pending. |
| 5H | Completion gates, canonical SQL, repeat-safe DB runner, docs/artifact | In Progress | `run-phase5.ps1`, reproducible Angular lock usage, SQL Server 2022 upgrade/fresh-install CI and canonical persistence added; awaiting green workflow evidence. |

## Phase 5 Security / Correctness Fixes Added 2026-08-23

- Store-scoped administrators can only view users/staff whose assignments overlap their authorized stores.
- Unassigned users/staff are not exposed to store-scoped administrators.
- Store-scoped administrators cannot assign `TenantAdmin`, `TenantOwner` or `ShopOwner` roles even if a custom permission grant is misconfigured.
- Store-scoped user/staff store assignments must remain inside the caller's authoritative store scope.
- Reactivating an inactive user/staff account re-checks `Tenant.MaxUsers`, closing the quota bypass where deactivate → replacement create → reactivate could exceed the tenant limit.
- Category list/create/update is constrained to tenant-wide scope or the caller's assigned stores.
- Existing store CRUD/location/lifecycle checks remain in the underlying Phase 5 service; the decorator adds defense-in-depth without replacing the business implementation.

## Database

Primary upgrade: `../../database/09_Upgrade/V1.4.0_Phase5_TenantStoresStaff.sql`

Direct Windows runner:

```powershell
.\database\run-phase5.ps1 -ServerInstance 'KRUTARTH-BHAVSA' -ValidateIdempotency
```

The runner uses Windows Integrated Security (`sqlcmd -E`) and validates exactly one V1.4.0 version row plus all Phase 5 tables/procedures. The GitHub runner also normalizes `database/CustSearchAi.sql` to a portable SQL Server 2022 bootstrap, appends the Phase 5 canonical block with comments, applies V1.4.0 twice to an upgrade database, and tests the final canonical script as a fresh install.

## Completion Gates

A Phase 5 `Completed` status requires all of the following on the final commit:

- .NET restore/build/tests green.
- Angular uses the checked-in pinned lock through `npm ci`; lint, tests and production build green.
- Python baseline Ruff/tests green so Phase 5 does not regress the AI foundation.
- V1.4.0 SQL applies twice successfully on SQL Server 2022.
- Final `database/CustSearchAi.sql` installs successfully on a fresh SQL Server 2022 database and contains the commented Phase 5 canonical block.
- No runtime EF migrations or schema auto-creation.
- Phase 5 PR merged only after final green evidence.

## Done Summary

Pending final CI evidence and canonical SQL persistence. After green completion, Phase 6 starts automatically under the user's 2026-08-23 sequential-execution authorization; no additional approval gate is required.
