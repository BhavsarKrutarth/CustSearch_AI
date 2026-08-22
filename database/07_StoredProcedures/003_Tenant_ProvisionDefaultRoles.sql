/*
==============================================================
Script        : 003_Tenant_ProvisionDefaultRoles.sql
Purpose       : Gives a newly created tenant its eight isolated default roles and safe grants.
Safety        : Repeat-safe and transactional; never grants platform permissions.
==============================================================
*/
USE [CustSearch_AI];
GO

-- Call this procedure inside the tenant-creation transaction immediately after the tenant row is saved.
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
        SELECT @TenantId, 2, source.Name, UPPER(source.Name), source.Description, 1, 1, SYSUTCDATETIME()
        FROM @TenantRoles AS source
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.Roles AS target
            WHERE target.TenantId = @TenantId AND target.NormalizedName = UPPER(source.Name)
        );

        -- These predicates mirror the reviewed Phase 3 least-privilege role defaults.
        INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
        SELECT role.Id, permission.Id
        FROM dbo.Roles AS role
        INNER JOIN dbo.Permissions AS permission ON permission.Scope = 2 AND permission.IsActive = 1
        WHERE role.TenantId = @TenantId AND role.Scope = 2 AND role.IsActive = 1
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
                  (permission.Name = N'Customers.View' OR permission.Name LIKE N'Invoices.%' OR permission.Name LIKE N'Payments.%'))
              OR (role.NormalizedName = N'CAMERAOPERATOR' AND
                  (permission.Name LIKE N'Cameras.%' OR permission.Name LIKE N'Recognition.%'
                   OR permission.Name IN (N'Visitors.View', N'Visits.View', N'Alerts.View', N'Alerts.Acknowledge')))
              OR (role.NormalizedName = N'INTEGRATIONADMIN' AND
                  (permission.Name LIKE N'Integrations.%' OR permission.Name LIKE N'Webhooks.%' OR permission.Name = N'Settings.View'))
              OR (role.NormalizedName = N'AUDITOR' AND
                  (permission.Name LIKE N'%.View' OR permission.Name IN
                      (N'TenantReports.Export', N'Reports.Export', N'VoiceCommands.Audit', N'AuditLogs.View')))
          )
          AND NOT EXISTS
              (SELECT 1 FROM dbo.RolePermissions AS grantRow WHERE grantRow.RoleId = role.Id AND grantRow.PermissionId = permission.Id);

        IF @StartedTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @StartedTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
