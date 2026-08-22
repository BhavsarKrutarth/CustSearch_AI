/*
==============================================================
Script        : 004_PlatformTenantManagement_Indexes.sql
Purpose       : Adds Phase 4 tenant search, subscription, usage, quota and audit lookup paths.
Safety        : Creates only missing indexes.
==============================================================
*/
USE [CustSearch_AI];
GO

-- Plan codes are stable external business keys.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SubscriptionPlans') AND name=N'UX_SubscriptionPlans_PlanCode')
    CREATE UNIQUE INDEX UX_SubscriptionPlans_PlanCode ON dbo.SubscriptionPlans (PlanCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SubscriptionPlans') AND name=N'IX_SubscriptionPlans_IsActive_PlanName')
    CREATE INDEX IX_SubscriptionPlans_IsActive_PlanName ON dbo.SubscriptionPlans (IsActive, PlanName);
GO

-- Platform tenant lists filter lifecycle and subscription state together.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Tenants') AND name=N'IX_Tenants_LifecycleSubscription')
    CREATE INDEX IX_Tenants_LifecycleSubscription ON dbo.Tenants (IsActive, IsSuspended, SubscriptionStatus) INCLUDE (TenantCode, DisplayName, SubscriptionPlanId, UpdatedUtc);
GO

-- Subscription history is read newest-first for one tenant.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TenantSubscriptions') AND name=N'IX_TenantSubscriptions_TenantId_Status_StartsUtc')
    CREATE INDEX IX_TenantSubscriptions_TenantId_Status_StartsUtc ON dbo.TenantSubscriptions (TenantId, Status, StartsUtc DESC) INCLUDE (SubscriptionPlanId, EndsUtc, AutoRenew);
GO

-- A reporting period may be captured only once for each tenant.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TenantUsageSnapshots') AND name=N'UX_TenantUsageSnapshots_Tenant_Period')
    CREATE UNIQUE INDEX UX_TenantUsageSnapshots_Tenant_Period ON dbo.TenantUsageSnapshots (TenantId, PeriodStartUtc, PeriodEndUtc);
GO

-- Quota history and expiry indexes support effective-limit resolution.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TenantQuotaOverrides') AND name=N'IX_TenantQuotaOverrides_TenantId_CreatedUtc')
    CREATE INDEX IX_TenantQuotaOverrides_TenantId_CreatedUtc ON dbo.TenantQuotaOverrides (TenantId, CreatedUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TenantQuotaOverrides') AND name=N'IX_TenantQuotaOverrides_ExpiresUtc')
    CREATE INDEX IX_TenantQuotaOverrides_ExpiresUtc ON dbo.TenantQuotaOverrides (ExpiresUtc);
GO

-- Audit indexes support tenant timelines, action reports and request correlation.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditLogs') AND name=N'IX_AuditLogs_TenantId_CreatedUtc')
    CREATE INDEX IX_AuditLogs_TenantId_CreatedUtc ON dbo.AuditLogs (TenantId, CreatedUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditLogs') AND name=N'IX_AuditLogs_Action_CreatedUtc')
    CREATE INDEX IX_AuditLogs_Action_CreatedUtc ON dbo.AuditLogs (Action, CreatedUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditLogs') AND name=N'IX_AuditLogs_CorrelationId')
    CREATE INDEX IX_AuditLogs_CorrelationId ON dbo.AuditLogs (CorrelationId);
GO
