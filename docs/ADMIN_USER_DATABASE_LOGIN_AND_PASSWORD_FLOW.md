# CustSearch AI — Admin/User Database, Login and Password Flow

Last verified: 2026-08-26 (Asia/Calcutta)  
Verified database: `KRUTARTH-BHAVSA / CustSearch_AI`  
Verified branch: `audit/all-phases-database-smoke`

## 1. Short answer

- Platform Admin, Tenant Admin, Shop Owner, Staff and Camera Operator ke liye alag-alag login tables nahi hain. Sabhi login identities `dbo.Users` mein store hoti hain.
- Account ka type `Users.Scope`, `Users.TenantId`, `dbo.UserRoles`, `dbo.Roles`, `dbo.RolePermissions` aur tenant users ke liye `dbo.UserStoreAssignments` decide karte hain.
- Shopper/customer record `dbo.Customers` mein hota hai. `Customers` login table nahi hai aur customer ko sirf customer record banane se Admin portal login nahi milta.
- Database mein plaintext password nahi hota. `dbo.Users.PasswordHash` one-way ASP.NET Core Identity hash rakhta hai.
- Password hash ko **decode/decrypt nahi kiya ja sakta aur nahi karna chahiye**. Login par entered password ko stored hash ke against verify kiya jata hai. Password bhoolne par password **reset/rotate** hota hai, old password recover nahi hota.

## 2. Current local UAT login identities

The following rows were read from the live local database without selecting `PasswordHash`.

| User ID | Tenant code at login | Username | Scope | Role | Authorized store |
| ---: | --- | --- | --- | --- | --- |
| 10034 | leave blank | `smoke.platform` | Platform | `PlatformSuperAdmin` | Not applicable |
| 10035 | `SMOKE-TENANT-001` | `smoke.tenantadmin` | Tenant | `TenantAdmin` | 11 / `SMOKE-STORE-001` |
| 10036 | `SMOKE-TENANT-001` | `smoke.staff` | Tenant | `Staff` | 11 / `SMOKE-STORE-001` |
| 10037 | `SMOKE-TENANT-002` | `smoke.tenantbadmin` | Tenant | `TenantAdmin` | 12 / `SMOKE-STORE-002` |
| 10038 | `SMOKE-TENANT-001` | `office.camera.operator` | Tenant | `CameraOperator` | 11 / `SMOKE-STORE-001` |
| 10039 | `SMOKE-TENANT-002` | `random.no.camera` | Tenant | `CameraOperator` | 12 / `SMOKE-STORE-002` |

The password is intentionally not present in this document or Git. The current password cannot be read back from SQL Server. The four deterministic `smoke.*` accounts can be given a new local-only password using the reset/seed procedure in section 10.

### Browser login values

Platform Admin:

```text
Tenant code: (blank)
Username: smoke.platform
Password: locally configured UAT password
```

Tenant Admin:

```text
Tenant code: SMOKE-TENANT-001
Username: smoke.tenantadmin
Password: locally configured UAT password
```

Tenant B isolation Admin:

```text
Tenant code: SMOKE-TENANT-002
Username: smoke.tenantbadmin
Password: locally configured UAT password
```

Do not use these smoke identities or a shared smoke password in production.

## 3. Authentication and authorization tables

### 3.1 `dbo.Users` — the login identity table

| Column | Purpose |
| --- | --- |
| `Id` | Internal login user ID. |
| `TenantId` | `NULL` for platform identities; required tenant owner boundary for tenant identities. |
| `Scope` | `1 = Platform`, `2 = Tenant`. |
| `UserName`, `NormalizedUserName` | Displayed username and normalized lookup value. |
| `Email`, `NormalizedEmail` | Email and normalized uniqueness/search value. |
| `DisplayName` | UI display name. |
| `PasswordHash` | One-way Identity V3 password hash; never return through API or UI. |
| `SecurityStamp` | Session/security version. Rotation invalidates old security context. |
| `IsActive` | Disabled users cannot log in. |
| `CreatedUtc`, `LastLoginUtc` | Account lifecycle timestamps. |

Database uniqueness is tenant-aware. The same normalized username may be valid in different tenant scopes, but not twice inside the same tenant. Platform users are selected only when tenant code is blank.

### 3.2 `dbo.Tenants` — tenant boundary

`TenantCode` is entered during tenant login. The table also owns tenant lifecycle, active/suspended status, subscription state and quotas. A correct password is still rejected when the account or its tenant is inactive/suspended.

### 3.3 `dbo.Roles` and `dbo.UserRoles` — role assignment

- `Roles.TenantId = NULL` and `Roles.Scope = 1` represent platform roles.
- A tenant role has the same `TenantId` as its user and `Scope = 2`.
- `UserRoles` links one user to one or more roles and records who assigned them.
- A role name by itself is not a tenant boundary. Scope and TenantId must also match.

Current platform system roles are:

- `PlatformSuperAdmin`
- `PlatformOperationsAdmin`
- `PlatformBillingAdmin`
- `PlatformSupportAdmin`
- `PlatformAuditor`

Current tenant role families include:

- `TenantAdmin`, `TenantOwner`, `ShopOwner`
- `StoreAdmin`, `StoreManager`, `Manager`
- `Staff`, `SalesStaff`, `CRMStaff`, `BillingStaff`
- `CameraOperator`, `IntegrationAdmin`, `Auditor`

Actual permission grants must be read from `RolePermissions`; the role name must not be treated as the only authorization check.

### 3.4 `dbo.Permissions` and `dbo.RolePermissions` — feature permission assignment

`Permissions` is the permission catalog. `RolePermissions` grants permissions to roles. API endpoints use policies such as:

- `TenantUsers.View`
- `TenantUsers.Create`
- `TenantUsers.Edit`
- `TenantUsers.AssignRoles`
- `TenantStores.View`
- `Staff.View` / `Staff.Manage`
- platform tenant, billing, report and operations permissions

### 3.5 `dbo.UserStoreAssignments` and `dbo.Stores` — store authorization

`UserStoreAssignments` links a tenant user to authorized stores and records the primary store. The API reloads this server-owned list. A browser-supplied StoreId is never sufficient authorization.

`Stores.TenantId` is also checked. A Tenant A user cannot gain Tenant B access by submitting Tenant B's StoreId.

### 3.6 `dbo.StaffProfiles` — staff business profile

`StaffProfiles.UserId` links a login identity to employee details such as employee code, first/last name and mobile number. A row in `dbo.Users` is not automatically a staff record; `StaffProfiles` is created only for staff workflows.

### 3.7 `dbo.RefreshTokens` — login session continuation

The table stores a **SHA-256 hash** of the refresh token, not the raw cookie. It tracks token family, expiry, revocation, replacement, IP metadata and the security stamp at issuance. Rotation/reuse detection protects against replay.

### 3.8 `dbo.AuthenticationEvents` — authentication audit

Records login/refresh/logout success or failure, user/tenant when known, failure code, timestamp, IP and correlation ID. It must not record plaintext passwords or raw tokens.

### 3.9 `dbo.AuditLogs` — administration/business audit

Records actor, action, entity, tenant/store scope, safe before/after JSON, correlation ID and time for management changes. Role/store/user changes are audited here. The Phase 16 database protection prevents normal mutation of existing audit entries.

### 3.10 Complete current admin-visible table inventory

The Admin SPA does not have one physical "Admin table". It is a secured UI over module-specific tables. The following inventory was checked against the live local SQL Server table list and the current controllers/services. Only the first two groups participate directly in login and access control.

| Area | Current tables | Admin purpose |
| --- | --- | --- |
| Login/session | `Users`, `RefreshTokens`, `AuthenticationEvents` | Identity, rotating session and authentication audit. |
| Roles/access scope | `Roles`, `Permissions`, `UserRoles`, `RolePermissions`, `UserStoreAssignments` | Platform/tenant role, permission and store authorization. |
| Platform tenant administration | `Tenants`, `SubscriptionPlans`, `TenantSubscriptions`, `TenantQuotaOverrides`, `TenantUsageSnapshots`, `AuditLogs` | Tenant lifecycle, plan/quota/usage and management audit. |
| Platform billing | `PlatformInvoices`, `PlatformInvoiceItems`, `PlatformPayments` | CustSearch-to-tenant billing; separate from shop retail billing. |
| Tenant/store/staff setup | `Stores`, `StaffProfiles`, `StaffShifts`, `StaffPresenceSessions`, `ProductCategories`, `StoreVoiceCommandSettings`, `StoreVoiceCommandAliases` | Tenant Admin setup and staff/store operations. |
| Shopper/customer | `Customers`, `CustomerStoreAssignments`, `AnonymousVisitors` | Shopper records and store visibility; these rows cannot log in. |
| Household/visit | `Households`, `HouseholdMembers`, `VisitParties`, `VisitPartyMembers`, `CustomerVisits` | Household and in-store visit workflows. |
| Products/retail | `Products`, `ProductStoreAvailabilities`, `RetailInvoices`, `RetailInvoiceItems`, `RetailInvoicePayments`, `RetailInvoiceParticipants`, `RetailInvoiceItemAttributions` | Shop product, invoice, payment and attribution data. |
| Preferences/voice | `CustomerPreferenceSignals`, `CustomerPreferenceScores`, `HouseholdPreferenceTags`, `PreferenceWeightVersions`, `ProductCategoryAliases`, `StoreVoiceCommandRuntimeSettings`, `VoiceCommandSessions` | Preference scoring and confirmation-controlled voice workflow. |
| Alerts/realtime | `Alerts`, `RealtimeEvents`, `NotificationOutbox` | Notification center, durable realtime recovery and delivery outbox. |
| Integrations | `IntegrationConfigurations`, `IntegrationInboundEvents`, `IntegrationOutbox`, `IntegrationDeliveryLogs` | Opaque integration configuration, idempotency, outbound delivery and audit. |
| Cameras/tracking | `Cameras`, `CameraZoneConfigurations`, `PersonTrackSessions`, `CameraTrackHandoffs`, `CameraOperationalEvents` | Camera configuration references, zones and anonymous-first tracking. |
| Recognition | `CustomerRecognitionConsents`, `BiometricTemplates`, `RecognitionCandidates` | Consent, encrypted derived templates and human review. |
| Reports/exports | `ExportJobs` | Current source-mapped asynchronous report/export jobs. |
| Platform operations | `OperationalSettings`, `OperationalSecretReferences`, `WorkerControls`, `WorkerLeases`, `WorkerHeartbeats`, `RetentionPolicies`, `RetentionRuns` | Platform operations dashboard, worker control/readiness and retention. |
| Foundation | `DatabaseVersions` | Applied database version history. |

The live local database also currently contains compatibility/live-only tables: `SystemSettings`, `ReportExportJobs`, `ReportExportEvents`, and the Phase 18 `Security*` tables (`SecurityRules`, `SecurityObservations`, `SecurityIngestionRequests`, `SecurityIncidents`, `SecurityIncidentItems`, `SecurityIncidentEvidence`, `SecurityIncidentActions`, `SecurityPaymentCorrelations`, `SecurityNotificationDeliveries`). They are not part of the current login table and are not current `CustSearchDbContext`/controller consumers. Do not build an Admin login assumption around them until their source/database reconciliation is completed.

In practical terms:

- Platform Admin pages primarily operate on platform tenant, subscription, billing, audit, reports and operations groups.
- Tenant Admin pages operate on the tenant-owned groups allowed by current permissions and store assignments.
- A row being present in any business table does not create a login. A login exists only when the identity exists in `Users` and has valid role/permission assignments.

## 4. Table relationship map

```text
Tenants
  ├── Users ── UserRoles ── Roles ── RolePermissions ── Permissions
  │      ├── RefreshTokens
  │      ├── AuthenticationEvents
  │      ├── UserStoreAssignments ── Stores
  │      └── StaffProfiles (only when this login is also a staff member)
  └── Stores

Users / Tenants / Stores ── AuditLogs

Customers = shopper/customer business records, not Admin portal login identities
```

## 5. Platform Admin login flow

1. Angular `/login` shows Tenant code, Username and Password.
2. Platform Admin leaves Tenant code blank.
3. Angular sends `POST /api/auth/login` with `tenantCode: null`.
4. API searches only a `dbo.Users` row where:
   - `Scope = Platform`
   - `TenantId IS NULL`
   - normalized username matches
5. ASP.NET Core PasswordHasher verifies the entered password against `PasswordHash`.
6. API confirms the user is active.
7. Roles and permissions are loaded from `UserRoles -> Roles -> RolePermissions -> Permissions`.
8. API returns a short-lived JWT and sets the rotating refresh token in a Secure, HttpOnly, SameSite=Strict cookie.
9. Angular routes the user to platform features allowed by roles/permissions.
10. Protected requests are checked again by API policies. Platform Admin does not become a tenant user merely by sending a TenantId.

Relevant code:

- `src/CustSearch.Admin/src/app/features/auth/login-page.ts`
- `src/CustSearch.API/Controllers/AuthController.cs`
- `src/CustSearch.Infrastructure/Security/AuthenticationService.cs`
- `src/CustSearch.Infrastructure/Security/JwtTokenService.cs`
- `src/CustSearch.API/Program.cs`

## 6. Tenant Admin and staff login flow

1. User enters the authoritative tenant code, username and password.
2. Angular sends all three values to `POST /api/auth/login`.
3. API finds `dbo.Tenants.TenantCode`, then only a `dbo.Users` row with:
   - `Scope = Tenant`
   - matching `TenantId`
   - matching normalized username
4. Password is verified using the stored one-way hash.
5. User must be active. Tenant must exist, be active and not suspended.
6. Roles and permissions are loaded from the database.
7. `PhaseFiveAuthenticationServiceDecorator` loads active store assignments from `UserStoreAssignments`.
8. JWT contains the authorized identity snapshot; refresh cookie is HttpOnly.
9. During JWT validation, the API reloads the live user, roles, permissions and stores. A changed role/store/security stamp invalidates stale authority.
10. Every business service derives TenantId from the authenticated server context and validates StoreId against the server-owned store list.

Wrong tenant code + correct username/password must fail because the user lookup is tenant-scoped.

## 7. Refresh, `/me` and logout flow

| Endpoint | Purpose |
| --- | --- |
| `POST /api/auth/login` | Verify credentials and issue JWT + refresh cookie. |
| `POST /api/auth/refresh` | Atomically consume/rotate the refresh token and return a new JWT/cookie. |
| `GET /api/auth/me` | Reload current authoritative user, roles, permissions and stores. |
| `POST /api/auth/logout` | Revoke the presented refresh token and delete the browser cookie. |
| `POST /api/auth/change-password` | Verify current password, set a new hash, revoke every session and delete the refresh cookie. |

Refresh token reuse revokes the token family. A changed security stamp, deactivated account, suspended tenant, expired token or authorization mismatch rejects the session.

## 8. Platform administration flow

The current Platform Admin flow is:

```text
Platform login
  -> platform dashboard
  -> subscription plan management
  -> tenant creation / activation / suspension
  -> subscription and quota assignment
  -> tenant usage, billing and audit review
```

Tenant creation provisions the tenant and its default tenant roles/permission grants. The current `POST /api/platform/tenants` request does **not** accept an owner password and does not create the first Tenant Admin login in the same operation.

This is an important onboarding gap: production onboarding needs an audited first-admin invitation/bootstrap endpoint with a short-lived, single-use token. Until that is implemented, local deterministic tenant admins are provisioned only by the local smoke-data runner. Do not solve production onboarding with a committed default password or an ad-hoc SQL password update.

## 9. Tenant user administration flow

Tenant management endpoints are under `/api/tenant` and require a tenant session plus the exact permission:

| Operation | Endpoint | Required permission |
| --- | --- | --- |
| List users | `GET /api/tenant/users` | `TenantUsers.View` |
| Read user | `GET /api/tenant/users/{id}` | `TenantUsers.View` |
| Create user | `POST /api/tenant/users` | `TenantUsers.Create` |
| Update profile/active state | `PUT /api/tenant/users/{id}` | `TenantUsers.Edit` |
| Replace roles | `PUT /api/tenant/users/{id}/roles` | `TenantUsers.AssignRoles` |
| Replace store assignments | `PUT /api/tenant/users/{id}/stores` | `TenantUsers.Edit` |
| Set another user's new password | `PUT /api/tenant/users/{id}/password` | `TenantUsers.Edit` |
| Create staff login + profile | `POST /api/tenant/staff` | `Staff.Manage` |

User creation validates tenant quota and uniqueness, hashes the supplied password, assigns roles/stores in a transaction and writes an audit entry. Store-scoped administrators can assign only their own stores and cannot grant tenant-wide roles such as TenantAdmin/TenantOwner/ShopOwner.

Role/store/profile changes revoke active refresh sessions. The user must sign in again with their newly authorized context.

The Angular user-management page is:

```text
/customer-admin/users
```

The current UI/API supports password during new user/staff creation and provides two explicit security flows:

- Any authenticated Platform or Tenant user can open `/account/change-password` from the profile area in the left sidebar. Current password is verified; success revokes all sessions and returns the browser to login.
- A tenant administrator with `TenantUsers.Edit` can open `/customer-admin/users`, select **Reset password** for another visible/in-scope user, set and confirm the new password, and revoke all of that user's sessions. Administrators cannot use this route to bypass current-password verification for their own account.

## 10. Password hash: what it is and how to safely recover access

### 10.1 Why it cannot be decoded

The application uses `PasswordHasher<UserAccount>` with ASP.NET Core Identity V3 compatible PBKDF2-SHA512 data. The local smoke runner generates the compatible V3 payload with a random salt, 100,000 iterations and a 32-byte subkey.

Hash verification works conceptually as follows:

```text
entered password + salt + work factor
  -> PBKDF2 calculation
  -> constant-time comparison with stored subkey
  -> Success / Failed / SuccessRehashNeeded
```

There is no decryption key. Salt is not a secret; it prevents identical passwords from producing identical stored values. Attempting to crack/dump password hashes is not a supported administration procedure.

### 10.2 Safe local reset for deterministic smoke accounts

This runner sets a newly chosen password for the deterministic smoke identities it owns. It does not print or commit the password/hash. Use a credential prompt so the password is not typed into the PowerShell command history:

```powershell
Set-Location "D:\Project\AdminCore\CustSearch_AI\CustSearch_AI"

$uatCredential = Get-Credential -UserName "local-uat" -Message "Enter a new local UAT password (12+ characters)"
$env:CUSTSEARCH_SMOKE_PASSWORD = $uatCredential.GetNetworkCredential().Password

.\database\10_TestData\run-smoke-data.ps1 `
  -ServerInstance "KRUTARTH-BHAVSA" `
  -DatabaseName "CustSearch_AI"

Remove-Item Env:\CUSTSEARCH_SMOKE_PASSWORD
$uatCredential = $null
```

The runner currently owns and rotates these accounts:

- `smoke.platform`
- `smoke.tenantadmin`
- `smoke.staff`
- `smoke.tenantbadmin`

Run the smoke verifier after rotation:

```powershell
sqlcmd -S "KRUTARTH-BHAVSA" -d "CustSearch_AI" -E -C -b `
  -i ".\database\10_TestData\AllPhases_SmokeData_Verify.sql"
```

Do not use `AllPhases_SmokeData_Cleanup.sql` on a reusable UAT dataset unless deterministic smoke-data removal is explicitly intended.

### 10.3 Other tenant users

`office.camera.operator` and `random.no.camera` are not rotated by the deterministic smoke runner. Their old password still cannot be recovered from the database. An authorized tenant administrator can now set a new password from `/customer-admin/users`; the API verifies tenant/store visibility, hashes the new value, rotates the security stamp, revokes active refresh tokens and writes an audit record without password material.

The admin-selected password is a definitive new credential, not a one-time invitation. For production onboarding/recovery, the remaining recommended flow is a short-lived, single-use invitation/reset token delivered through a verified notification channel. It should force the recipient to choose their own password and must never place the password, token or hash in logs.

Do not manually copy another user's hash, attempt hash decoding, or update `PasswordHash` with a plain string.

## 11. Read-only SQL diagnostics (no secret columns)

### Find login identities and roles

```sql
SELECT
    u.Id,
    CASE u.Scope WHEN 1 THEN 'Platform' WHEN 2 THEN 'Tenant' END AS UserScope,
    t.TenantCode,
    u.UserName,
    u.Email,
    u.DisplayName,
    u.IsActive,
    u.LastLoginUtc,
    STRING_AGG(r.Name, ', ') AS Roles
FROM dbo.Users AS u
LEFT JOIN dbo.Tenants AS t ON t.Id = u.TenantId
LEFT JOIN dbo.UserRoles AS ur ON ur.UserId = u.Id
LEFT JOIN dbo.Roles AS r ON r.Id = ur.RoleId
GROUP BY u.Id, u.Scope, t.TenantCode, u.UserName, u.Email,
         u.DisplayName, u.IsActive, u.LastLoginUtc
ORDER BY t.TenantCode, u.UserName;
```

### Find authorized stores

```sql
SELECT
    u.Id AS UserId,
    t.TenantCode,
    u.UserName,
    s.Id AS StoreId,
    s.StoreCode,
    s.StoreName,
    usa.IsPrimary
FROM dbo.UserStoreAssignments AS usa
JOIN dbo.Users AS u ON u.Id = usa.UserId
JOIN dbo.Tenants AS t ON t.Id = usa.TenantId
JOIN dbo.Stores AS s ON s.Id = usa.StoreId
ORDER BY t.TenantCode, u.UserName, usa.IsPrimary DESC, s.StoreCode;
```

### Find effective permissions

```sql
DECLARE @UserId bigint = 10035;

SELECT DISTINCT p.Name AS Permission
FROM dbo.UserRoles AS ur
JOIN dbo.Roles AS r ON r.Id = ur.RoleId AND r.IsActive = 1
JOIN dbo.RolePermissions AS rp ON rp.RoleId = r.Id
JOIN dbo.Permissions AS p ON p.Id = rp.PermissionId AND p.IsActive = 1
JOIN dbo.Users AS u ON u.Id = ur.UserId
WHERE ur.UserId = @UserId
  AND r.Scope = u.Scope
  AND (r.TenantId = u.TenantId OR (r.TenantId IS NULL AND u.TenantId IS NULL))
ORDER BY p.Name;
```

These diagnostic queries deliberately do not select `PasswordHash`, raw token material or sensitive customer data.

## 12. Swagger login examples

Platform login request:

```json
{
  "tenantCode": null,
  "userName": "smoke.platform",
  "password": "<enter local UAT password at runtime>"
}
```

Tenant login request:

```json
{
  "tenantCode": "SMOKE-TENANT-001",
  "userName": "smoke.tenantadmin",
  "password": "<enter local UAT password at runtime>"
}
```

Do not save a real password in Swagger examples, `.http` files, Postman collections, screenshots or Git.

## 13. Security rules for implementation and support

- Never decode, export or display `PasswordHash`.
- Never store a plaintext password in SQL, `appsettings*.json`, Angular environment files, logs or documentation.
- Never use a customer-supplied TenantId as the trusted scope.
- Never accept a StoreId without checking current server-side assignments.
- Use generic invalid-credentials responses so username/tenant discovery is not leaked.
- Rate-limit login and password-reset endpoints before production rollout.
- Rotate `SecurityStamp` and revoke refresh sessions after password, role, store or high-risk security changes.
- Audit admin changes, but redact password/reset token/cookie/JWT values.
- Use HTTPS in deployment; Secure refresh cookies will not be protected correctly over plain HTTP.
- Platform Admin and Tenant Admin should use separate named accounts; do not share one production credential.

## 14. Verified implementation traceability

| Area | Source |
| --- | --- |
| Angular login form | `src/CustSearch.Admin/src/app/features/auth/login-page.ts` and `.html` |
| Angular self-service password UI | `src/CustSearch.Admin/src/app/features/auth/change-password-page.ts` |
| Angular session/refresh | `src/CustSearch.Admin/src/app/core/auth/` |
| Authentication endpoints | `src/CustSearch.API/Controllers/AuthController.cs` |
| JWT authoritative revalidation | `src/CustSearch.API/Program.cs` |
| Password verification/change/session rotation | `src/CustSearch.Infrastructure/Security/AuthenticationService.cs` |
| Store authorization enrichment | `src/CustSearch.Infrastructure/Security/PhaseFiveAuthenticationServiceDecorator.cs` |
| Tenant user endpoints | `src/CustSearch.API/Controllers/TenantOperationsController.cs` |
| Tenant user creation/role/store flow | `src/CustSearch.Infrastructure/TenantOperations/TenantOperationsService.cs` |
| Store-scoped privilege protection | `src/CustSearch.Infrastructure/TenantOperations/TenantOperationsSecurityDecorator.cs` |
| Entity/table mappings | `src/CustSearch.Infrastructure/Persistence/Configurations/` |
| Local smoke identity provisioning | `database/10_TestData/AllPhases_SmokeData.sql` |
| Safe local password hash runner | `database/10_TestData/run-smoke-data.ps1` |

## 15. Current verified gaps

1. No email/notification-based forgot-password single-use token flow.
2. Platform tenant creation does not create/invite the first Tenant Admin.
3. Admin reset currently sets a definitive password; production onboarding should use a recipient-owned one-time invitation flow.

Self-service change password and permission-protected tenant-admin reset are implemented in API and Angular. The remaining gaps do not make hash decoding acceptable; they define the next authentication work around recipient-owned, token-based onboarding and recovery.
