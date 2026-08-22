/*
==============================================================
Script        : 005_SeedPhase4Defaults.sql
Purpose       : Adds a zero-cost trial plan and provisions roles for existing tenants.
Safety        : Repeat-safe; does not overwrite commercial pricing or existing grants.
==============================================================
*/
USE [CustSearch_AI];
GO

-- The trial plan supplies conservative defaults until a platform admin selects a paid plan.
IF NOT EXISTS (SELECT 1 FROM dbo.SubscriptionPlans WHERE PlanCode = N'TRIAL')
BEGIN
    INSERT INTO dbo.SubscriptionPlans
        (PlanCode, PlanName, MonthlyPrice, AnnualPrice, MaxStores, MaxUsers, MaxCameras,
         MaxMonthlyRecognitions, MaxMonthlyApiCalls, IsActive, CreatedUtc, UpdatedUtc, RowVersion)
    VALUES
        (N'TRIAL', N'Trial', 0, NULL, 1, 5, 5, 10000, 10000, 1,
         SYSUTCDATETIME(), SYSUTCDATETIME(), CONVERT(BINARY(16), NEWID()));
END;
GO

-- Existing tenants receive the same role provisioning path used for newly created tenants.
DECLARE @TenantId BIGINT;
DECLARE tenant_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Tenants;
OPEN tenant_cursor;
FETCH NEXT FROM tenant_cursor INTO @TenantId;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId = @TenantId;
    FETCH NEXT FROM tenant_cursor INTO @TenantId;
END;
CLOSE tenant_cursor;
DEALLOCATE tenant_cursor;
GO
