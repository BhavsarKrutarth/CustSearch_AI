/*
==============================================================
Script        : 004_CreatePlatformTenantManagementTables.sql
Purpose       : Adds Phase 4 tenant profile, plan, quota, usage and audit persistence.
Safety        : Repeat-safe; preserves and backfills all existing tenant data.
==============================================================
*/
USE [CustSearch_AI];
GO

SET XACT_ABORT ON;
GO

-- SubscriptionPlans stores reusable pricing and default resource limits.
IF OBJECT_ID(N'dbo.SubscriptionPlans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubscriptionPlans
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_SubscriptionPlans PRIMARY KEY,
        PlanCode NVARCHAR(30) NOT NULL,
        PlanName NVARCHAR(100) NOT NULL,
        MonthlyPrice DECIMAL(19, 4) NOT NULL,
        AnnualPrice DECIMAL(19, 4) NULL,
        MaxStores INT NOT NULL,
        MaxUsers INT NOT NULL,
        MaxCameras INT NOT NULL,
        MaxMonthlyRecognitions BIGINT NULL,
        MaxMonthlyApiCalls BIGINT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SubscriptionPlans_IsActive DEFAULT (1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SubscriptionPlans_CreatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SubscriptionPlans_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        RowVersion BINARY(16) NOT NULL CONSTRAINT DF_SubscriptionPlans_RowVersion DEFAULT CONVERT(BINARY(16), NEWID()),
        CONSTRAINT CK_SubscriptionPlans_Prices CHECK (MonthlyPrice >= 0 AND (AnnualPrice IS NULL OR AnnualPrice >= 0)),
        CONSTRAINT CK_SubscriptionPlans_Limits CHECK
            (MaxStores > 0 AND MaxUsers > 0 AND MaxCameras > 0
             AND (MaxMonthlyRecognitions IS NULL OR MaxMonthlyRecognitions > 0)
             AND (MaxMonthlyApiCalls IS NULL OR MaxMonthlyApiCalls > 0))
    );
END;
GO

-- These additive tenant columns complete the platform-management profile without replacing existing rows.
IF COL_LENGTH(N'dbo.Tenants', N'PrimaryContactName') IS NULL
    ALTER TABLE dbo.Tenants ADD PrimaryContactName NVARCHAR(150) NOT NULL CONSTRAINT DF_Tenants_PrimaryContactName DEFAULT (N'Unknown') WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'PrimaryEmail') IS NULL
    ALTER TABLE dbo.Tenants ADD PrimaryEmail NVARCHAR(254) NOT NULL CONSTRAINT DF_Tenants_PrimaryEmail DEFAULT (N'unknown@invalid.local') WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'PrimaryMobile') IS NULL
    ALTER TABLE dbo.Tenants ADD PrimaryMobile NVARCHAR(30) NOT NULL CONSTRAINT DF_Tenants_PrimaryMobile DEFAULT (N'') WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'CountryCode') IS NULL
    ALTER TABLE dbo.Tenants ADD CountryCode CHAR(2) NOT NULL CONSTRAINT DF_Tenants_CountryCode DEFAULT ('XX') WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'CurrencyCode') IS NULL
    ALTER TABLE dbo.Tenants ADD CurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_Tenants_CurrencyCode DEFAULT ('USD') WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'SubscriptionPlanId') IS NULL
    ALTER TABLE dbo.Tenants ADD SubscriptionPlanId BIGINT NULL;
IF COL_LENGTH(N'dbo.Tenants', N'SubscriptionStatus') IS NULL
    ALTER TABLE dbo.Tenants ADD SubscriptionStatus TINYINT NOT NULL CONSTRAINT DF_Tenants_SubscriptionStatus DEFAULT (1) WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'TrialStartsUtc') IS NULL ALTER TABLE dbo.Tenants ADD TrialStartsUtc DATETIME2(7) NULL;
IF COL_LENGTH(N'dbo.Tenants', N'TrialEndsUtc') IS NULL ALTER TABLE dbo.Tenants ADD TrialEndsUtc DATETIME2(7) NULL;
IF COL_LENGTH(N'dbo.Tenants', N'SubscriptionStartsUtc') IS NULL ALTER TABLE dbo.Tenants ADD SubscriptionStartsUtc DATETIME2(7) NULL;
IF COL_LENGTH(N'dbo.Tenants', N'SubscriptionEndsUtc') IS NULL ALTER TABLE dbo.Tenants ADD SubscriptionEndsUtc DATETIME2(7) NULL;
IF COL_LENGTH(N'dbo.Tenants', N'MaxStores') IS NULL
    ALTER TABLE dbo.Tenants ADD MaxStores INT NOT NULL CONSTRAINT DF_Tenants_MaxStores DEFAULT (1) WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'MaxUsers') IS NULL
    ALTER TABLE dbo.Tenants ADD MaxUsers INT NOT NULL CONSTRAINT DF_Tenants_MaxUsers DEFAULT (5) WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'MaxCameras') IS NULL
    ALTER TABLE dbo.Tenants ADD MaxCameras INT NOT NULL CONSTRAINT DF_Tenants_MaxCameras DEFAULT (5) WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'SuspensionReason') IS NULL ALTER TABLE dbo.Tenants ADD SuspensionReason NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.Tenants', N'UpdatedUtc') IS NULL
    ALTER TABLE dbo.Tenants ADD UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Tenants_UpdatedUtc DEFAULT SYSUTCDATETIME() WITH VALUES;
IF COL_LENGTH(N'dbo.Tenants', N'RowVersion') IS NULL
    ALTER TABLE dbo.Tenants ADD RowVersion BINARY(16) NOT NULL CONSTRAINT DF_Tenants_RowVersion DEFAULT CONVERT(BINARY(16), NEWID()) WITH VALUES;
GO

-- This corrected lifecycle rule allows inactive or suspended tenants but rejects the contradictory combination.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'CK_Tenants_ActiveSuspended')
    ALTER TABLE dbo.Tenants DROP CONSTRAINT CK_Tenants_ActiveSuspended;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'CK_Tenants_ActiveSuspended')
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_ActiveSuspended CHECK (NOT (IsActive = 0 AND IsSuspended = 1));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'CK_Tenants_SubscriptionStatus')
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_SubscriptionStatus CHECK (SubscriptionStatus BETWEEN 1 AND 6);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'CK_Tenants_Quotas')
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_Quotas CHECK (MaxStores > 0 AND MaxUsers > 0 AND MaxCameras > 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'CK_Tenants_TrialPeriod')
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_TrialPeriod CHECK (TrialEndsUtc IS NULL OR TrialStartsUtc IS NULL OR TrialEndsUtc > TrialStartsUtc);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'CK_Tenants_SubscriptionPeriod')
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_SubscriptionPeriod CHECK (SubscriptionEndsUtc IS NULL OR SubscriptionStartsUtc IS NULL OR SubscriptionEndsUtc > SubscriptionStartsUtc);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'FK_Tenants_SubscriptionPlans_SubscriptionPlanId')
    ALTER TABLE dbo.Tenants ADD CONSTRAINT FK_Tenants_SubscriptionPlans_SubscriptionPlanId FOREIGN KEY (SubscriptionPlanId) REFERENCES dbo.SubscriptionPlans (Id);
GO

-- TenantSubscriptions retains plan assignment history rather than overwriting commercial periods.
IF OBJECT_ID(N'dbo.TenantSubscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantSubscriptions
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_TenantSubscriptions PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        SubscriptionPlanId BIGINT NOT NULL,
        BillingCycle TINYINT NOT NULL,
        Status TINYINT NOT NULL,
        StartsUtc DATETIME2(7) NOT NULL,
        EndsUtc DATETIME2(7) NULL,
        AutoRenew BIT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_TenantSubscriptions_CreatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_TenantSubscriptions_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        RowVersion BINARY(16) NOT NULL CONSTRAINT DF_TenantSubscriptions_RowVersion DEFAULT CONVERT(BINARY(16), NEWID()),
        CONSTRAINT FK_TenantSubscriptions_Tenants_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT FK_TenantSubscriptions_SubscriptionPlans_SubscriptionPlanId FOREIGN KEY (SubscriptionPlanId) REFERENCES dbo.SubscriptionPlans (Id),
        CONSTRAINT CK_TenantSubscriptions_BillingCycle CHECK (BillingCycle IN (1, 2)),
        CONSTRAINT CK_TenantSubscriptions_Status CHECK (Status BETWEEN 1 AND 6),
        CONSTRAINT CK_TenantSubscriptions_Period CHECK (EndsUtc IS NULL OR EndsUtc > StartsUtc)
    );
END;
GO

-- TenantUsageSnapshots stores immutable period totals used by quota and dashboard queries.
IF OBJECT_ID(N'dbo.TenantUsageSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantUsageSnapshots
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_TenantUsageSnapshots PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        PeriodStartUtc DATETIME2(7) NOT NULL,
        PeriodEndUtc DATETIME2(7) NOT NULL,
        StoreCount INT NOT NULL,
        UserCount INT NOT NULL,
        CameraCount INT NOT NULL,
        RecognitionCount BIGINT NOT NULL,
        ApiCallCount BIGINT NOT NULL,
        CapturedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_TenantUsageSnapshots_CapturedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TenantUsageSnapshots_Tenants_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id) ON DELETE CASCADE,
        CONSTRAINT CK_TenantUsageSnapshots_Period CHECK (PeriodEndUtc > PeriodStartUtc),
        CONSTRAINT CK_TenantUsageSnapshots_Counts CHECK
            (StoreCount >= 0 AND UserCount >= 0 AND CameraCount >= 0 AND RecognitionCount >= 0 AND ApiCallCount >= 0)
    );
END;
GO

-- TenantQuotaOverrides preserves who changed limits, why, and for how long.
IF OBJECT_ID(N'dbo.TenantQuotaOverrides', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantQuotaOverrides
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_TenantQuotaOverrides PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        MaxStores INT NULL,
        MaxUsers INT NULL,
        MaxCameras INT NULL,
        MaxMonthlyRecognitions BIGINT NULL,
        MaxMonthlyApiCalls BIGINT NULL,
        Reason NVARCHAR(500) NOT NULL,
        CreatedByUserId BIGINT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_TenantQuotaOverrides_CreatedUtc DEFAULT SYSUTCDATETIME(),
        ExpiresUtc DATETIME2(7) NULL,
        CONSTRAINT FK_TenantQuotaOverrides_Tenants_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantQuotaOverrides_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_TenantQuotaOverrides_AnyLimit CHECK
            (MaxStores IS NOT NULL OR MaxUsers IS NOT NULL OR MaxCameras IS NOT NULL OR MaxMonthlyRecognitions IS NOT NULL OR MaxMonthlyApiCalls IS NOT NULL),
        CONSTRAINT CK_TenantQuotaOverrides_Limits CHECK
            ((MaxStores IS NULL OR MaxStores > 0) AND (MaxUsers IS NULL OR MaxUsers > 0) AND (MaxCameras IS NULL OR MaxCameras > 0)
             AND (MaxMonthlyRecognitions IS NULL OR MaxMonthlyRecognitions > 0) AND (MaxMonthlyApiCalls IS NULL OR MaxMonthlyApiCalls > 0)),
        CONSTRAINT CK_TenantQuotaOverrides_Expiry CHECK (ExpiresUtc IS NULL OR ExpiresUtc > CreatedUtc)
    );
END;
GO

-- AuditLogs is append-only evidence for platform actions and tenant-targeted administration.
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        TenantId BIGINT NULL,
        StoreId BIGINT NULL,
        UserId BIGINT NULL,
        ActorType NVARCHAR(50) NOT NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(100) NULL,
        BeforeJson NVARCHAR(4000) NULL,
        AfterJson NVARCHAR(4000) NULL,
        IpAddress VARCHAR(64) NULL,
        UserAgent NVARCHAR(500) NULL,
        CorrelationId VARCHAR(64) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_AuditLogs_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_AuditLogs_Tenants_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT FK_AuditLogs_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
    );
END;
GO

-- This trigger restricts quota overrides to platform identities even for direct database writes.
CREATE OR ALTER TRIGGER dbo.TR_TenantQuotaOverrides_RequirePlatformActor
ON dbo.TenantQuotaOverrides
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT 1 FROM inserted AS item
        INNER JOIN dbo.Users AS actor ON actor.Id = item.CreatedByUserId
        WHERE actor.Scope <> 1 OR actor.TenantId IS NOT NULL
    )
        THROW 51010, 'Tenant quota overrides require a platform user.', 1;
END;
GO

-- This trigger guarantees at most one current commercial subscription per tenant.
CREATE OR ALTER TRIGGER dbo.TR_TenantSubscriptions_OneCurrent
ON dbo.TenantSubscriptions
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT subscription.TenantId
        FROM dbo.TenantSubscriptions AS subscription
        INNER JOIN (SELECT DISTINCT TenantId FROM inserted) AS changed ON changed.TenantId = subscription.TenantId
        WHERE subscription.Status IN (1, 2, 3, 4)
        GROUP BY subscription.TenantId
        HAVING COUNT_BIG(*) > 1
    )
        THROW 51011, 'A tenant can have only one current subscription.', 1;
END;
GO
