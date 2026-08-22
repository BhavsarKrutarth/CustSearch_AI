/*
==============================================================
Script        : 004_Tenant_GetDetailSummary.sql
Purpose       : Returns one platform-facing tenant profile and its latest operational totals.
Safety        : Read-only and explicitly scoped by TenantId.
==============================================================
*/
USE [CustSearch_AI];
GO

-- Platform APIs pass an authorized tenant identifier and receive no other tenant's data.
CREATE OR ALTER PROCEDURE dbo.Tenant_GetDetailSummary
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.Id,
        t.TenantCode,
        t.LegalName,
        t.DisplayName,
        t.PrimaryContactName,
        t.PrimaryEmail,
        t.PrimaryMobile,
        t.CountryCode,
        t.TimeZone,
        t.CurrencyCode,
        t.SubscriptionStatus,
        subscriptionPlan.PlanCode,
        subscriptionPlan.PlanName,
        t.MaxStores,
        t.MaxUsers,
        t.MaxCameras,
        t.IsActive,
        t.IsSuspended,
        t.SuspensionReason,
        t.UpdatedUtc,
        (SELECT COUNT_BIG(*) FROM dbo.Users AS appUser WHERE appUser.TenantId = t.Id) AS UserCount,
        (SELECT COUNT_BIG(*) FROM dbo.Roles AS role WHERE role.TenantId = t.Id AND role.IsActive = 1) AS ActiveRoleCount,
        latestUsage.StoreCount,
        latestUsage.CameraCount,
        latestUsage.RecognitionCount,
        latestUsage.ApiCallCount,
        latestUsage.CapturedUtc AS UsageCapturedUtc
    FROM dbo.Tenants AS t
    LEFT JOIN dbo.SubscriptionPlans AS subscriptionPlan ON subscriptionPlan.Id = t.SubscriptionPlanId
    OUTER APPLY
    (
        SELECT TOP (1) snapshot.StoreCount, snapshot.CameraCount, snapshot.RecognitionCount,
            snapshot.ApiCallCount, snapshot.CapturedUtc
        FROM dbo.TenantUsageSnapshots AS snapshot
        WHERE snapshot.TenantId = t.Id
        ORDER BY snapshot.PeriodEndUtc DESC, snapshot.Id DESC
    ) AS latestUsage
    WHERE t.Id = @TenantId;
END;
GO
