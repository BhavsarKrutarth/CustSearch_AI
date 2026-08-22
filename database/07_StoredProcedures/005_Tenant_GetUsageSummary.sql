/*
==============================================================
Script        : 005_Tenant_GetUsageSummary.sql
Purpose       : Returns latest usage beside effective plan and override limits for one t.
Safety        : Read-only and explicitly scoped by TenantId.
==============================================================
*/
USE [CustSearch_AI];
GO

-- Each quota uses the newest non-expired override that supplied that specific limit.
CREATE OR ALTER PROCEDURE dbo.Tenant_GetUsageSummary
    @TenantId BIGINT,
    @AsOfUtc DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @AsOfUtc = COALESCE(@AsOfUtc, SYSUTCDATETIME());

    SELECT
        t.Id AS TenantId,
        t.TenantCode,
        latestUsage.PeriodStartUtc,
        latestUsage.PeriodEndUtc,
        latestUsage.StoreCount,
        latestUsage.UserCount,
        latestUsage.CameraCount,
        latestUsage.RecognitionCount,
        latestUsage.ApiCallCount,
        COALESCE(storeOverride.MaxStores, t.MaxStores, subscriptionPlan.MaxStores) AS MaxStores,
        COALESCE(userOverride.MaxUsers, t.MaxUsers, subscriptionPlan.MaxUsers) AS MaxUsers,
        COALESCE(cameraOverride.MaxCameras, t.MaxCameras, subscriptionPlan.MaxCameras) AS MaxCameras,
        COALESCE(recognitionOverride.MaxMonthlyRecognitions, subscriptionPlan.MaxMonthlyRecognitions) AS MaxMonthlyRecognitions,
        COALESCE(apiOverride.MaxMonthlyApiCalls, subscriptionPlan.MaxMonthlyApiCalls) AS MaxMonthlyApiCalls,
        latestUsage.CapturedUtc
    FROM dbo.Tenants AS t
    LEFT JOIN dbo.SubscriptionPlans AS subscriptionPlan ON subscriptionPlan.Id = t.SubscriptionPlanId
    OUTER APPLY (SELECT TOP (1) * FROM dbo.TenantUsageSnapshots AS item WHERE item.TenantId=t.Id ORDER BY item.PeriodEndUtc DESC, item.Id DESC) AS latestUsage
    OUTER APPLY (SELECT TOP (1) item.MaxStores FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxStores IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS storeOverride
    OUTER APPLY (SELECT TOP (1) item.MaxUsers FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxUsers IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS userOverride
    OUTER APPLY (SELECT TOP (1) item.MaxCameras FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxCameras IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS cameraOverride
    OUTER APPLY (SELECT TOP (1) item.MaxMonthlyRecognitions FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxMonthlyRecognitions IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS recognitionOverride
    OUTER APPLY (SELECT TOP (1) item.MaxMonthlyApiCalls FROM dbo.TenantQuotaOverrides AS item WHERE item.TenantId=t.Id AND item.MaxMonthlyApiCalls IS NOT NULL AND (item.ExpiresUtc IS NULL OR item.ExpiresUtc>@AsOfUtc) ORDER BY item.CreatedUtc DESC, item.Id DESC) AS apiOverride
    WHERE t.Id = @TenantId;
END;
GO
