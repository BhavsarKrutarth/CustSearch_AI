:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Corrects role-provisioning drift found during the localhost office-camera UAT.
  CameraOperator remains tenant/store scoped: this script grants no platform permission and
  camera queries must still enforce JWT TenantId plus authoritative store assignments.
*/
IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Roles', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
    THROW 55195, 'Role-provisioning prerequisites are missing.', 1;
GO

CREATE OR ALTER PROCEDURE dbo.Tenant_ProvisionDefaultRoles
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = @TenantId)
        THROW 51020, 'Tenant does not exist.', 1;

    DECLARE @StartedTransaction BIT = 0;
    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @StartedTransaction = 1;
    END;

    BEGIN TRY
        DECLARE @TenantRoles TABLE (Name NVARCHAR(100), Description NVARCHAR(300));
        INSERT @TenantRoles (Name, Description) VALUES
            (N'TenantAdmin', N'Full administration access inside one tenant.'),
            (N'TenantOwner', N'Business owner with full tenant operations.'),
            (N'ShopOwner', N'Shop owner with full tenant operations.'),
            (N'StoreAdmin', N'Assigned-store operations without tenant-wide security settings.'),
            (N'StoreManager', N'Assigned-store staff and customer operations.'),
            (N'Manager', N'Day-to-day customer, visit, invoice, alert and report operations.'),
            (N'SalesStaff', N'Assigned-store staff operations.'),
            (N'CRMStaff', N'Customer, household, preference and consent operations.'),
            (N'BillingStaff', N'Invoice, payment and purchase-related operations.'),
            (N'CameraOperator', N'Assigned-store camera, recognition and live visitor operations.'),
            (N'IntegrationAdmin', N'Integration, webhook and synchronization operations.'),
            (N'Auditor', N'Read-only tenant operations and audit access.');

        INSERT dbo.Roles (TenantId, Scope, Name, NormalizedName, Description, IsSystem, IsActive, CreatedUtc)
        SELECT @TenantId, 2, source.Name, UPPER(source.Name), source.Description, 1, 1, SYSUTCDATETIME()
        FROM @TenantRoles source
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.Roles target
            WHERE target.TenantId = @TenantId AND target.NormalizedName = UPPER(source.Name)
        );

        INSERT dbo.RolePermissions (RoleId, PermissionId)
        SELECT role.Id, permission.Id
        FROM dbo.Roles role
        JOIN dbo.Permissions permission ON permission.Scope = 2 AND permission.IsActive = 1
        WHERE role.TenantId = @TenantId AND role.Scope = 2 AND role.IsActive = 1
          AND
          (
              role.NormalizedName IN (N'TENANTADMIN', N'TENANTOWNER', N'SHOPOWNER')
              OR (role.NormalizedName IN (N'STOREADMIN', N'STOREMANAGER') AND permission.Name NOT IN
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
                  (permission.Name = N'Customers.View' OR permission.Name LIKE N'Invoices.%' OR permission.Name LIKE N'Payments.%'))
              OR (role.NormalizedName = N'SALESSTAFF' AND permission.Name IN
                  (N'TenantDashboard.View', N'Staff.View', N'StoreCategories.View', N'VoiceCommands.Use',
                   N'VoiceCommands.View', N'Customers.View', N'Customers.Create', N'Customers.Edit', N'Visits.View'))
              OR (role.NormalizedName = N'CAMERAOPERATOR' AND
                  (permission.Name LIKE N'Cameras.%' OR permission.Name LIKE N'Recognition.%'
                   OR permission.Name IN (N'TenantDashboard.View', N'Visitors.View', N'Visits.View', N'Alerts.View', N'Alerts.Acknowledge')))
              OR (role.NormalizedName = N'INTEGRATIONADMIN' AND
                  (permission.Name LIKE N'Integrations.%' OR permission.Name LIKE N'Webhooks.%' OR permission.Name = N'Settings.View'))
              OR (role.NormalizedName = N'AUDITOR' AND
                  (permission.Name LIKE N'%.View' OR permission.Name IN
                      (N'TenantReports.Export', N'Reports.Export', N'VoiceCommands.Audit', N'AuditLogs.View')))
          )
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.RolePermissions grantRow
              WHERE grantRow.RoleId = role.Id AND grantRow.PermissionId = permission.Id
          );

        IF @StartedTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @StartedTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

DECLARE @TenantId BIGINT;
DECLARE TenantCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Tenants;
OPEN TenantCursor;
FETCH NEXT FROM TenantCursor INTO @TenantId;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId = @TenantId;
    FETCH NEXT FROM TenantCursor INTO @TenantId;
END;
CLOSE TenantCursor;
DEALLOCATE TenantCursor;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber = N'V1.16.1')
    INSERT dbo.DatabaseVersions (VersionNumber, Description, AppliedUtc, AppliedBy)
    VALUES (N'V1.16.1', N'Correct complete tenant role grants including CameraOperator localhost/server UAT access', SYSUTCDATETIME(), SUSER_SNAME());
GO

IF EXISTS
(
    SELECT 1
    FROM dbo.Roles role
    JOIN dbo.RolePermissions grantRow ON grantRow.RoleId = role.Id
    JOIN dbo.Permissions permission ON permission.Id = grantRow.PermissionId
    WHERE role.NormalizedName = N'CAMERAOPERATOR' AND permission.Scope <> 2
)
    THROW 55196, 'CameraOperator received a non-tenant permission.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.Tenants tenant
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Roles role
        JOIN dbo.RolePermissions grantRow ON grantRow.RoleId = role.Id
        JOIN dbo.Permissions permission ON permission.Id = grantRow.PermissionId
        WHERE role.TenantId = tenant.Id AND role.NormalizedName = N'CAMERAOPERATOR'
          AND permission.Name = N'TenantDashboard.View'
    )
    OR NOT EXISTS
    (
        SELECT 1
        FROM dbo.Roles role
        JOIN dbo.RolePermissions grantRow ON grantRow.RoleId = role.Id
        JOIN dbo.Permissions permission ON permission.Id = grantRow.PermissionId
        WHERE role.TenantId = tenant.Id AND role.NormalizedName = N'CAMERAOPERATOR'
          AND permission.Name = N'Cameras.View'
    )
)
    THROW 55197, 'CameraOperator grants were not provisioned for every tenant.', 1;
GO
