/*
==============================================================
Script        : 003_SeedAuthorizationCatalog.sql
Purpose       : Seeds the approved Phase 3 roles, permissions and safe default grants.
Safety        : Repeat-safe; inserts only missing catalog and grant rows.
==============================================================
*/
USE [CustSearch_AI];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

-- This in-memory catalog keeps the shared API/UI permission names in one reviewable list.
DECLARE @Permissions TABLE
(
    Scope TINYINT NOT NULL,
    Name NVARCHAR(150) NOT NULL
);

INSERT INTO @Permissions (Scope, Name)
VALUES
    (1, N'Tenants.View'), (1, N'Tenants.Create'), (1, N'Tenants.Edit'),
    (1, N'Tenants.Activate'), (1, N'Tenants.Suspend'), (1, N'Tenants.ViewUsage'),
    (1, N'Tenants.ViewOperationalSummary'), (1, N'PlatformBilling.View'),
    (1, N'PlatformBilling.Manage'), (1, N'SubscriptionPlans.View'),
    (1, N'SubscriptionPlans.Manage'), (1, N'PlatformReports.View'),
    (1, N'PlatformReports.Export'), (1, N'PlatformAudit.View'),
    (1, N'PlatformSupport.AccessTenant'),
    (2, N'TenantDashboard.View'), (2, N'TenantUsers.View'), (2, N'TenantUsers.Create'),
    (2, N'TenantUsers.Edit'), (2, N'TenantUsers.Deactivate'), (2, N'TenantUsers.AssignRoles'),
    (2, N'TenantStores.View'), (2, N'TenantStores.Create'), (2, N'TenantStores.Edit'),
    (2, N'TenantBilling.View'), (2, N'TenantReports.View'), (2, N'TenantReports.Export'),
    (2, N'TenantAudit.View'),
    (2, N'Customers.View'), (2, N'Customers.Create'), (2, N'Customers.Edit'),
    (2, N'Visitors.View'), (2, N'Visitors.Convert'),
    (2, N'Households.View'), (2, N'Households.Create'), (2, N'Households.Edit'),
    (2, N'Households.ManageMembers'), (2, N'Visits.View'), (2, N'Visits.Edit'),
    (2, N'Invoices.View'), (2, N'Invoices.Create'), (2, N'Invoices.Edit'),
    (2, N'Payments.View'), (2, N'Payments.Create'),
    (2, N'Cameras.View'), (2, N'Cameras.Manage'), (2, N'Cameras.Control'),
    (2, N'Recognition.View'), (2, N'Recognition.Review'),
    (2, N'Preferences.View'), (2, N'Preferences.Manage'),
    (2, N'Alerts.View'), (2, N'Alerts.Acknowledge'), (2, N'Alerts.Configure'),
    (2, N'Consents.View'), (2, N'Consents.Manage'),
    (2, N'Integrations.View'), (2, N'Integrations.Manage'),
    (2, N'Webhooks.View'), (2, N'Webhooks.Manage'),
    (2, N'Reports.View'), (2, N'Reports.Export'),
    (2, N'Users.View'), (2, N'Users.Manage'),
    (2, N'Staff.View'), (2, N'Staff.Manage'), (2, N'StaffTracking.View'),
    (2, N'StaffPerformance.View'), (2, N'StaffPerformance.Export'),
    (2, N'StaffCustomerInteractions.View'),
    (2, N'StoreCategories.View'), (2, N'StoreCategories.Manage'),
    (2, N'VoiceCommands.Use'), (2, N'VoiceCommands.View'),
    (2, N'VoiceCommands.Configure'), (2, N'VoiceCommands.Audit'),
    (2, N'CustomerJourneys.View'), (2, N'DwellAnalytics.View'),
    (2, N'Roles.Manage'), (2, N'Settings.View'), (2, N'Settings.Manage'),
    (2, N'AuditLogs.View');

INSERT INTO dbo.Permissions (Scope, Name, Description, IsActive, CreatedUtc)
SELECT source.Scope, source.Name, CONCAT(N'Allows ', source.Name, N' operations.'), 1, SYSUTCDATETIME()
FROM @Permissions AS source
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions AS target WHERE target.Name = source.Name);

-- Platform roles are global system roles and therefore deliberately have no TenantId.
DECLARE @PlatformRoles TABLE (Name NVARCHAR(100), Description NVARCHAR(300));
INSERT INTO @PlatformRoles (Name, Description)
VALUES
    (N'PlatformSuperAdmin', N'Full platform administration access.'),
    (N'PlatformOperationsAdmin', N'Tenant lifecycle, health and usage operations.'),
    (N'PlatformBillingAdmin', N'Platform billing and subscription administration.'),
    (N'PlatformSupportAdmin', N'Limited and audited tenant support access.'),
    (N'PlatformAuditor', N'Read-only platform reporting and audit access.');

INSERT INTO dbo.Roles (TenantId, Scope, Name, NormalizedName, Description, IsSystem, IsActive, CreatedUtc)
SELECT NULL, 1, source.Name, UPPER(source.Name), source.Description, 1, 1, SYSUTCDATETIME()
FROM @PlatformRoles AS source
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Roles AS target
    WHERE target.TenantId IS NULL AND target.NormalizedName = UPPER(source.Name)
);

-- Every existing tenant receives its own isolated copy of the approved tenant roles.
DECLARE @TenantRoles TABLE (Name NVARCHAR(100), Description NVARCHAR(300));
INSERT INTO @TenantRoles (Name, Description)
VALUES
    (N'TenantAdmin', N'Full administration access inside one tenant.'),
    (N'StoreAdmin', N'Assigned-store operations without tenant-wide security settings.'),
    (N'Manager', N'Day-to-day customer, visit, invoice, alert and report operations.'),
    (N'CRMStaff', N'Customer, household, preference and consent operations.'),
    (N'BillingStaff', N'Invoice, payment and purchase-related operations.'),
    (N'CameraOperator', N'Camera, recognition and live visitor operations.'),
    (N'IntegrationAdmin', N'Integration, webhook and synchronization operations.'),
    (N'Auditor', N'Read-only tenant operations and audit access.');

INSERT INTO dbo.Roles (TenantId, Scope, Name, NormalizedName, Description, IsSystem, IsActive, CreatedUtc)
SELECT tenant.Id, 2, source.Name, UPPER(source.Name), source.Description, 1, 1, SYSUTCDATETIME()
FROM dbo.Tenants AS tenant
CROSS JOIN @TenantRoles AS source
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Roles AS target
    WHERE target.TenantId = tenant.Id AND target.NormalizedName = UPPER(source.Name)
);

-- PlatformSuperAdmin receives all platform capabilities; other roles receive least-privilege defaults.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT role.Id, permission.Id
FROM dbo.Roles AS role
INNER JOIN dbo.Permissions AS permission ON permission.Scope = 1 AND permission.IsActive = 1
WHERE role.Scope = 1 AND role.IsActive = 1
  AND
  (
      role.NormalizedName = N'PLATFORMSUPERADMIN'
      OR (role.NormalizedName = N'PLATFORMOPERATIONSADMIN' AND permission.Name IN
          (N'Tenants.View', N'Tenants.Create', N'Tenants.Edit', N'Tenants.Activate', N'Tenants.Suspend',
           N'Tenants.ViewUsage', N'Tenants.ViewOperationalSummary', N'PlatformReports.View', N'PlatformReports.Export'))
      OR (role.NormalizedName = N'PLATFORMBILLINGADMIN' AND permission.Name IN
          (N'Tenants.View', N'PlatformBilling.View', N'PlatformBilling.Manage',
           N'SubscriptionPlans.View', N'SubscriptionPlans.Manage'))
      OR (role.NormalizedName = N'PLATFORMSUPPORTADMIN' AND permission.Name IN
          (N'Tenants.View', N'Tenants.ViewOperationalSummary', N'PlatformSupport.AccessTenant'))
      OR (role.NormalizedName = N'PLATFORMAUDITOR' AND permission.Name IN
          (N'Tenants.View', N'Tenants.ViewUsage', N'Tenants.ViewOperationalSummary',
           N'PlatformReports.View', N'PlatformReports.Export', N'PlatformAudit.View'))
  )
  AND NOT EXISTS
      (SELECT 1 FROM dbo.RolePermissions AS grantRow WHERE grantRow.RoleId = role.Id AND grantRow.PermissionId = permission.Id);

-- Tenant role defaults follow the page access described in planning sections 58 and 59.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT role.Id, permission.Id
FROM dbo.Roles AS role
INNER JOIN dbo.Permissions AS permission ON permission.Scope = 2 AND permission.IsActive = 1
WHERE role.Scope = 2 AND role.IsActive = 1
  AND
  (
      role.NormalizedName = N'TENANTADMIN'
      OR (role.NormalizedName = N'STOREADMIN' AND permission.Name NOT IN
          (N'TenantUsers.Create', N'TenantUsers.Edit', N'TenantUsers.Deactivate', N'TenantUsers.AssignRoles',
           N'TenantStores.Create', N'TenantStores.Edit', N'Roles.Manage', N'Settings.Manage'))
      OR (role.NormalizedName = N'MANAGER' AND
          (permission.Name IN (N'TenantDashboard.View', N'TenantReports.View', N'TenantReports.Export')
           OR permission.Name LIKE N'Customers.%' OR permission.Name LIKE N'Households.%'
           OR permission.Name LIKE N'Visits.%' OR permission.Name LIKE N'Invoices.%'
           OR permission.Name LIKE N'Alerts.%' OR permission.Name LIKE N'Reports.%'
           OR permission.Name LIKE N'Preferences.%'))
      OR (role.NormalizedName = N'CRMSTAFF' AND
          (permission.Name LIKE N'Customers.%' OR permission.Name LIKE N'Households.%'
           OR permission.Name LIKE N'Visitors.%' OR permission.Name LIKE N'Preferences.%'
           OR permission.Name LIKE N'Consents.%' OR permission.Name IN (N'Visits.View', N'CustomerJourneys.View')))
      OR (role.NormalizedName = N'BILLINGSTAFF' AND
          (permission.Name IN (N'Customers.View') OR permission.Name LIKE N'Invoices.%'
           OR permission.Name LIKE N'Payments.%'))
      OR (role.NormalizedName = N'CAMERAOPERATOR' AND
          (permission.Name LIKE N'Cameras.%' OR permission.Name LIKE N'Recognition.%'
           OR permission.Name IN (N'Visitors.View', N'Visits.View', N'Alerts.View', N'Alerts.Acknowledge')))
      OR (role.NormalizedName = N'INTEGRATIONADMIN' AND
          (permission.Name LIKE N'Integrations.%' OR permission.Name LIKE N'Webhooks.%'
           OR permission.Name IN (N'Settings.View')))
      OR (role.NormalizedName = N'AUDITOR' AND
          (permission.Name LIKE N'%.View' OR permission.Name IN
              (N'TenantReports.Export', N'Reports.Export', N'VoiceCommands.Audit', N'AuditLogs.View')))
  )
  AND NOT EXISTS
      (SELECT 1 FROM dbo.RolePermissions AS grantRow WHERE grantRow.RoleId = role.Id AND grantRow.PermissionId = permission.Id);

COMMIT TRANSACTION;
GO
