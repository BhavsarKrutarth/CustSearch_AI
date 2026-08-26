# Platform Admin Functional Test Report

Date: 2026-08-26  
Environment: local development database and API  
Account scope: Platform Admin (tenant code intentionally blank)

## Completed fixes

- Restored the deterministic platform smoke account credential using an ASP.NET Identity-compatible salted hash and revoked its previous refresh sessions. No plaintext password or reusable hash was written to source, SQL scripts, logs, screenshots, or this report.
- Replaced the disabled `Tenant Users` navigation item with a permission-protected, paged cross-tenant user overview.
- Replaced the disabled `Stores` navigation item with a permission-protected, paged cross-tenant store overview.
- Connected `Platform Billing` to the platform billing plans workspace and corrected that workspace to render in Platform Admin context.
- Connected `Reports` to the platform-scoped reports/export API instead of the tenant API.
- Implemented `Log out` against `POST /api/auth/logout`, followed by in-memory access-token clearing and navigation to login even if the network call fails.
- Added automated coverage for logout and the requested Platform Admin navigation paths.

## Real functional UAT

The supplied Platform Admin account was used through the real Angular UI and local SQL-backed API. The following flows passed:

1. Login with blank tenant code redirected to `/admin/dashboard`.
2. `Tenant Users` opened `/admin/tenant-users` and displayed the smoke tenant administrator.
3. `Stores` opened `/admin/stores` and displayed the smoke main store.
4. `Platform Billing` opened `/platform-admin/billing/plans` and loaded subscription plans.
5. `Reports` opened `/admin/reports` and loaded the platform report catalog and export queue.
6. `Log out` returned to `/login`; refreshing remained on login, confirming that the refresh session was revoked.
7. The browser run recorded no failed HTTP responses and no console errors.

Screenshots are stored in the ignored local evidence directory `artifacts/platform-admin-uat/`.

## Automated verification

- `.NET Release build`: passed with 0 warnings and 0 errors.
- `.NET unit tests`: 104 passed.
- `.NET integration tests`: 234 passed.
- Angular lint: passed.
- Angular unit tests: 86 passed.
- Angular production build: passed. The pre-existing AdminShell component-style budget warning remains (151 bytes over its 4 kB warning threshold); it does not block the build.
- Targeted Platform Admin Playwright tests: 6 passed.
- Full Playwright suite: 51 passed.

## Security boundaries retained

- Platform user/store list responses exclude password hashes, security stamps, refresh tokens, and camera credentials.
- All new endpoints require platform scope plus `Tenants.View`.
- Platform reports use `/api/platform/reports`; tenant report routes remain isolated.
- Browser requests do not supply a tenant identity for platform operations.

## Tenant create and password-edit fix (2026-08-26)

### Root cause and database fix

- Real create requests failed while inserting the initial subscription because `dbo.TenantSubscriptions` has the enabled trigger `TR_TenantSubscriptions_OneCurrent`, while EF Core was generating an `OUTPUT` clause that SQL Server does not allow for that trigger shape.
- The EF mappings for `TenantSubscriptions`, `TenantQuotaOverrides`, and `UserRoles` now use trigger-compatible SQL (`UseSqlOutputClause(false)`). No database trigger was disabled or weakened.
- Tenant creation remains one transaction: tenant, default roles/permissions, initial Tenant Admin, optional trial subscription, optional quota override, and audit evidence either all commit or all roll back.

### Tenant Admin onboarding and password edit

- Create Tenant now requires an initial Tenant Admin username, password, and confirmation.
- The first user is stored tenant-wise in `dbo.Users`, receives the tenant's `TenantAdmin` role through `dbo.UserRoles`, and receives all permissions provisioned for that role.
- Edit Tenant now loads the tenant's primary active Tenant Admin and provides a new-password/confirmation form with Show/Hide control.
- A password edit replaces the salted ASP.NET Identity hash, rotates the security stamp, revokes active refresh sessions, and writes a password-reset audit event.
- Existing passwords are intentionally never displayed: the database contains only a one-way salted hash, not recoverable plaintext. Neither create nor reset API responses contain password material.

### Lifecycle wording

- `Lifecycle / Tenant access state` was renamed to `Tenant activation / Activate or suspend tenant`.
- `Suspension reason` is explained as required only for suspension. Suspending blocks tenant-user access until reactivation.

### Real SQL-backed verification

- Platform login: passed.
- Tenant create with active plan plus quota overrides: HTTP 201 passed against the real `CustSearch_AI` SQL Server database.
- Created UAT evidence tenant: ID `10024`, code `TEN-7AB53FDCD10F`, display name prefix `UAT Create Verify`.
- Tenant Admin lookup: passed.
- Platform-side Tenant Admin password edit/reset: passed.
- Login using the reset tenant credentials and tenant code: HTTP 200 passed.
- Test passwords were generated in memory and were not written to source, SQL scripts, screenshots, or this report.

### Database write sequence (no plaintext credential script)

1. Insert tenant and obtain its generated ID.
2. Execute `dbo.Tenant_ProvisionDefaultRoles` for that tenant.
3. Hash the submitted one-time password in application memory and insert only the resulting hash in `dbo.Users`.
4. Assign the tenant-owned `TenantAdmin` role in `dbo.UserRoles`.
5. Insert the optional subscription and quota override using trigger-compatible EF SQL.
6. Insert the safe `TenantCreated` audit record and commit the transaction.

This sequence stays in application code rather than a direct SQL password script so credentials cannot be accidentally stored in `.sql` files or deployment logs.
