/* Phase 9 Platform Billing — verification only. No schema changes. */
USE [CustSearch_AI];
SET NOCOUNT ON;

PRINT '--- Phase 9 version ---';
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0') <> 1
    THROW 52901, 'V1.8.0 is missing or duplicated.', 1;
SELECT VersionNumber, Description, AppliedUtc, AppliedBy
FROM dbo.DatabaseVersions
WHERE VersionNumber IN (N'V1.7.0', N'V1.8.0')
ORDER BY VersionNumber;

PRINT '--- Phase 9 columns ---';
IF COL_LENGTH('dbo.SubscriptionPlans','Description') IS NULL
 OR COL_LENGTH('dbo.SubscriptionPlans','Currency') IS NULL
 OR COL_LENGTH('dbo.SubscriptionPlans','TrialDays') IS NULL
 OR COL_LENGTH('dbo.SubscriptionPlans','MaxStaff') IS NULL
 OR COL_LENGTH('dbo.SubscriptionPlans','FeatureLimitsJson') IS NULL
 OR COL_LENGTH('dbo.SubscriptionPlans','DisplayOrder') IS NULL
 OR COL_LENGTH('dbo.Tenants','MaxStaff') IS NULL
 OR COL_LENGTH('dbo.TenantSubscriptions','TrialEndUtc') IS NULL
 OR COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodStartUtc') IS NULL
 OR COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodEndUtc') IS NULL
 OR COL_LENGTH('dbo.TenantSubscriptions','CancelAtPeriodEnd') IS NULL
 OR COL_LENGTH('dbo.TenantSubscriptions','CancelledUtc') IS NULL
    THROW 52902, 'One or more Phase 9 columns are missing.', 1;

SELECT
    OBJECT_NAME(c.object_id) AS TableName,
    c.name AS ColumnName,
    TYPE_NAME(c.user_type_id) AS DataType,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
FROM sys.columns c
WHERE (c.object_id=OBJECT_ID(N'dbo.SubscriptionPlans') AND c.name IN(N'Description',N'Currency',N'TrialDays',N'MaxStaff',N'FeatureLimitsJson',N'DisplayOrder'))
   OR (c.object_id=OBJECT_ID(N'dbo.Tenants') AND c.name=N'MaxStaff')
   OR (c.object_id=OBJECT_ID(N'dbo.TenantSubscriptions') AND c.name IN(N'TrialEndUtc',N'CurrentPeriodStartUtc',N'CurrentPeriodEndUtc',N'CancelAtPeriodEnd',N'CancelledUtc'))
ORDER BY TableName, ColumnName;

PRINT '--- Phase 9 tables ---';
IF OBJECT_ID(N'dbo.PlatformInvoices',N'U') IS NULL
 OR OBJECT_ID(N'dbo.PlatformInvoiceItems',N'U') IS NULL
 OR OBJECT_ID(N'dbo.PlatformPayments',N'U') IS NULL
    THROW 52903, 'One or more Phase 9 platform billing tables are missing.', 1;

SELECT name AS Phase9Table
FROM sys.tables
WHERE name IN(N'PlatformInvoices',N'PlatformInvoiceItems',N'PlatformPayments')
ORDER BY name;

PRINT '--- Phase 9 stored procedures ---';
IF OBJECT_ID(N'dbo.PlatformBilling_Plan_List',N'P') IS NULL
 OR OBJECT_ID(N'dbo.PlatformBilling_Subscription_List',N'P') IS NULL
 OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_List',N'P') IS NULL
 OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_Get',N'P') IS NULL
 OR OBJECT_ID(N'dbo.PlatformBilling_Payment_List',N'P') IS NULL
 OR OBJECT_ID(N'dbo.TenantPlatformBilling_Summary_Get',N'P') IS NULL
    THROW 52904, 'One or more Phase 9 stored procedures are missing.', 1;

SELECT name AS Phase9Procedure, create_date, modify_date
FROM sys.procedures
WHERE name IN(
    N'PlatformBilling_Plan_List',
    N'PlatformBilling_Subscription_List',
    N'PlatformBilling_Invoice_List',
    N'PlatformBilling_Invoice_Get',
    N'PlatformBilling_Payment_List',
    N'TenantPlatformBilling_Summary_Get'
)
ORDER BY name;

PRINT '--- Phase 9 permission separation ---';
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=1 AND Name=N'PlatformBilling.Plans.View')
 OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=1 AND Name=N'PlatformBilling.Subscriptions.Manage')
 OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'TenantPlatformBilling.Subscriptions.View')
 OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'TenantPlatformBilling.Invoices.View')
 OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'TenantPlatformBilling.Payments.View')
    THROW 52905, 'Phase 9 platform/tenant permissions are incomplete.', 1;

IF EXISTS(
    SELECT 1 FROM dbo.Permissions
    WHERE Scope=2
      AND Name IN(N'PlatformBilling.Subscriptions.View',N'PlatformBilling.Invoices.View',N'PlatformBilling.Payments.View')
)
    THROW 52906, 'Old duplicate tenant billing permission names remain.', 1;

SELECT Scope, Name, Description, IsActive
FROM dbo.Permissions
WHERE Name LIKE N'PlatformBilling.%' OR Name LIKE N'TenantPlatformBilling.%'
ORDER BY Scope, Name;

PRINT '--- Retail / Platform domain separation ---';
IF EXISTS(
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id IN(OBJECT_ID(N'dbo.PlatformInvoices'),OBJECT_ID(N'dbo.PlatformInvoiceItems'),OBJECT_ID(N'dbo.PlatformPayments'))
      AND referenced_object_id IN(OBJECT_ID(N'dbo.RetailInvoices'),OBJECT_ID(N'dbo.RetailInvoiceItems'),OBJECT_ID(N'dbo.RetailInvoicePayments'))
)
    THROW 52907, 'Platform Billing has an invalid foreign key to Phase 8 Retail Billing.', 1;

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS ParentTable,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys fk
WHERE fk.parent_object_id IN(OBJECT_ID(N'dbo.PlatformInvoices'),OBJECT_ID(N'dbo.PlatformInvoiceItems'),OBJECT_ID(N'dbo.PlatformPayments'))
ORDER BY ParentTable, ForeignKeyName;

PRINT '--- Procedure smoke tests ---';
EXEC dbo.PlatformBilling_Plan_List @IncludeInactive=1;
EXEC dbo.PlatformBilling_Subscription_List @TenantId=NULL;
EXEC dbo.PlatformBilling_Invoice_List @TenantId=NULL,@PageNumber=1,@PageSize=10;
EXEC dbo.PlatformBilling_Invoice_Get @PlatformInvoiceId=0,@TenantId=NULL;
EXEC dbo.PlatformBilling_Payment_List @TenantId=NULL,@PlatformInvoiceId=NULL;
EXEC dbo.TenantPlatformBilling_Summary_Get @TenantId=1;

DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
PRINT 'PHASE9_DATABASE_VERIFICATION_GREEN';
