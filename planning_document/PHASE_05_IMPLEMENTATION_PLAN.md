# Phase 5 — Tenant Users, Stores, Shop Owner & Staff Implementation Plan

Created: 2026-08-23 (Asia/Kolkata)

Status: Completed

Source of truth: `CustSearch_AI_Final_Planning_ShopOwner_Staff_CCTV_AashaDynamic.md`

Detailed implementation record: `phase_implementation/PHASE_05_TENANT_USERS_STORES_STAFF.md`

## Objective

Deliver the complete Customer Admin operational foundation required before shopper/customer intelligence: tenant users and roles, ShopOwner/TenantOwner access, authoritative user-store assignments, store master and location data, staff management, shifts/presence, product categories, dynamic per-store voice settings, permissions, audit behavior and the base Customer Admin dashboard.

Phase 5 is the authorization and store-operations foundation used by Phase 6 and later customer/CCTV intelligence phases.

## Completion Status

Phase 5 implementation is complete and merged into `AIMainBranch`.

| Sub-phase | Scope | Status |
|---|---|---|
| 5A | Tenant users, roles, TenantOwner/ShopOwner and role assignment | Completed |
| 5B | User-store assignments and authoritative store scope | Completed |
| 5C | Store CRUD, address/location, coordinates, geofence, timezone and lifecycle | Completed |
| 5D | Staff profiles, store assignment, shifts and operational presence | Completed |
| 5E | Store/category taxonomy | Completed |
| 5F | Dynamic per-store voice trigger/settings/aliases | Completed |
| 5G | Customer Admin dashboard base and tenant summary | Completed |
| 5H | SQL, validation, security hardening and completion gates | Completed |

## Implemented Backend Scope

- Tenant-scoped user CRUD and lifecycle.
- Tenant role assignment with protected `TenantAdmin`, `TenantOwner` and `ShopOwner` boundaries.
- Store-scoped authorization based on server-authoritative store assignments.
- Store-scoped administrators cannot see or modify users/staff outside their assigned stores.
- User/staff reactivation re-checks tenant user quotas.
- Store CRUD with tenant ownership, address, city/state/country, latitude/longitude, geofence radius, timezone and location verification metadata.
- Staff profiles and store relationships.
- Staff shift and presence-session operational models/APIs.
- Category CRUD with tenant/store isolation.
- Dynamic voice command configuration per store. `Aasha Add` remains the default/example and is not a hard-coded global requirement.
- Tenant dashboard summary and tenant-safe store/staff search.
- Audit logging and permission-protected operations.

## Database Implementation

Primary upgrade:

`database/09_Upgrade/V1.4.0_Phase5_TenantStoresStaff.sql`

PowerShell runner:

`database/run-phase5.ps1`

Direct SQL Server / SSMS runner:

`database/run-phase5.sql`

Canonical database:

`database/CustSearchAi.sql`

### Phase 5 Tables

- `Stores`
- `UserStoreAssignments`
- `StaffProfiles`
- `StaffShifts`
- `StaffPresenceSessions`
- `ProductCategories`
- `StoreVoiceCommandSettings`
- `StoreVoiceCommandAliases`

Phase 5 also reuses the existing identity/authorization tables:

- `Users`
- `Roles`
- `Permissions`
- `UserRoles`
- `RolePermissions`

### Required Stored Procedures

- `Tenant_ProvisionDefaultRoles`
- `TenantDashboard_GetSummary`
- `Store_Search`
- `Staff_Search`

### Database Rules

- Version ledger: `V1.4.0`.
- SQL changes are repeat-safe/idempotent.
- Existing rows are preserved.
- Objects are not recreated when already present.
- Procedures use `CREATE OR ALTER` where appropriate.
- Required indexes and foreign keys are created only when missing.
- No EF Core migration workflow is used for production schema deployment.

## Security and Tenant Isolation

- TenantId is resolved and enforced by server context.
- Store-scoped users are constrained to authoritative assigned StoreIds.
- Store-scoped administrators cannot escalate another user into tenant-wide owner/admin roles.
- Cross-store user, staff and category access is blocked.
- Reactivation cannot bypass `MaxUsers` quota enforcement.
- Presence/CCTV-derived operational signals are not treated as authoritative payroll or disciplinary truth.
- Voice command settings are store-specific and tenant-owned.

## Angular Customer Admin Scope

- Customer Admin dashboard base.
- Tenant user management.
- Store management.
- Staff management.
- Staff detail and operational shift/presence workflows.
- Product category administration.
- Voice settings and aliases.
- Permission-aware navigation and route behavior.
- Dark/light/system theme compatibility on the repository Angular baseline.

## Validation / Completion Evidence

Phase 5 completion included the following gate categories:

- .NET restore/build and automated tests.
- Angular dependency install through checked-in lockfile, lint, tests and production build.
- Playwright Customer Admin regression coverage.
- Python baseline regression checks.
- SQL Server 2022 V1.4.0 repeat-apply/idempotency validation.
- Fresh canonical database validation.
- Tenant/store authorization regression checks.
- No production EF migration or automatic schema creation.

Phase 5 was merged as the completed baseline used by Phase 6.

## Completion Summary

Phase 5 is complete. The project now has a tenant-safe Customer Admin operational layer covering users, roles, ShopOwner/TenantOwner access, stores, staff, shifts/presence, categories and configurable store voice settings. The database schema is versioned through V1.4.0 and maintained in both upgrade scripts and the canonical `database/CustSearchAi.sql` file.

The Phase 5 store and authorization foundation is the required dependency for Phase 6 shopper customers and anonymous visitors.
