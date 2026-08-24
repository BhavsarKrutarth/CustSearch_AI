/*
 CustSearch AI — Phase 9 production database upgrade
 Version: V1.8.0
 Rules: SQL Server 2022, idempotent/repeat-safe, no EF migrations.

 Phase 9 is Platform Billing (tenant/shop owner pays CustSearch).
 It is intentionally separate from Phase 8 Retail Billing (shop-customer purchases).
 This upgrade extends the existing plan/subscription foundation and creates only
 PlatformInvoices / PlatformInvoiceItems / PlatformPayments plus read procedures.
*/
USE [CustSearch_AI];
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'CustSearch_AI'
    THROW 51800, 'Run Phase 9 against the CustSearch_AI database.', 1;
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.7.0')
    THROW 51801, 'Phase 9 requires validated V1.7.0 Phase 8 baseline.', 1;
IF OBJECT_ID(N'dbo.SubscriptionPlans',N'U') IS NULL OR OBJECT_ID(N'dbo.Tenants',N'U') IS NULL OR OBJECT_ID(N'dbo.TenantSubscriptions',N'U') IS NULL
    THROW 51802, 'Platform tenancy/subscription baseline is incomplete.', 1;
IF OBJECT_ID(N'dbo.Permissions',N'U') IS NULL OR OBJECT_ID(N'dbo.Roles',N'U') IS NULL OR OBJECT_ID(N'dbo.RolePermissions',N'U') IS NULL
    THROW 51803, 'Authorization baseline is incomplete.', 1;

BEGIN TRANSACTION;
BEGIN TRY
    /* =========================================================
       9A — Subscription plan catalog extension
       ========================================================= */
    IF COL_LENGTH('dbo.SubscriptionPlans','Description') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD Description NVARCHAR(1000) NOT NULL CONSTRAINT DF_SubscriptionPlans_Description DEFAULT(N'');
    IF COL_LENGTH('dbo.SubscriptionPlans','Currency') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD Currency CHAR(3) NOT NULL CONSTRAINT DF_SubscriptionPlans_Currency DEFAULT('USD');
    IF COL_LENGTH('dbo.SubscriptionPlans','TrialDays') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD TrialDays INT NOT NULL CONSTRAINT DF_SubscriptionPlans_TrialDays DEFAULT(0);
    IF COL_LENGTH('dbo.SubscriptionPlans','MaxStaff') IS NULL
    BEGIN
        ALTER TABLE dbo.SubscriptionPlans ADD MaxStaff INT NULL;
        EXEC sys.sp_executesql N'UPDATE dbo.SubscriptionPlans SET MaxStaff=MaxUsers WHERE MaxStaff IS NULL;';
        EXEC sys.sp_executesql N'ALTER TABLE dbo.SubscriptionPlans ALTER COLUMN MaxStaff INT NOT NULL;';
    END;
    IF COL_LENGTH('dbo.SubscriptionPlans','FeatureLimitsJson') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD FeatureLimitsJson NVARCHAR(4000) NULL;
    IF COL_LENGTH('dbo.SubscriptionPlans','DisplayOrder') IS NULL
        ALTER TABLE dbo.SubscriptionPlans ADD DisplayOrder INT NOT NULL CONSTRAINT DF_SubscriptionPlans_DisplayOrder DEFAULT(0);

    IF OBJECT_ID(N'dbo.CK_SubscriptionPlans_Limits',N'C') IS NOT NULL
        ALTER TABLE dbo.SubscriptionPlans DROP CONSTRAINT CK_SubscriptionPlans_Limits;
    EXEC sys.sp_executesql N'ALTER TABLE dbo.SubscriptionPlans WITH CHECK ADD CONSTRAINT CK_SubscriptionPlans_Limits CHECK(MaxStores>0 AND MaxUsers>0 AND MaxStaff>0 AND MaxCameras>0 AND (MaxMonthlyRecognitions IS NULL OR MaxMonthlyRecognitions>0) AND (MaxMonthlyApiCalls IS NULL OR MaxMonthlyApiCalls>0));';
    IF OBJECT_ID(N'dbo.CK_SubscriptionPlans_TrialDisplay',N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.SubscriptionPlans WITH CHECK ADD CONSTRAINT CK_SubscriptionPlans_TrialDisplay CHECK(TrialDays>=0 AND DisplayOrder>=0);';
    IF OBJECT_ID(N'dbo.CK_SubscriptionPlans_FeatureLimitsJson',N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.SubscriptionPlans WITH CHECK ADD CONSTRAINT CK_SubscriptionPlans_FeatureLimitsJson CHECK(FeatureLimitsJson IS NULL OR ISJSON(FeatureLimitsJson)=1);';
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SubscriptionPlans') AND name=N'IX_SubscriptionPlans_Display')
        EXEC sys.sp_executesql N'CREATE INDEX IX_SubscriptionPlans_Display ON dbo.SubscriptionPlans(IsActive,DisplayOrder,PlanName);';

    /* =========================================================
       9B — Effective tenant quotas and subscription lifecycle
       ========================================================= */
    IF COL_LENGTH('dbo.Tenants','MaxStaff') IS NULL
    BEGIN
        ALTER TABLE dbo.Tenants ADD MaxStaff INT NULL;
        EXEC sys.sp_executesql N'UPDATE dbo.Tenants SET MaxStaff=MaxUsers WHERE MaxStaff IS NULL;';
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Tenants ALTER COLUMN MaxStaff INT NOT NULL;';
    END;
    IF OBJECT_ID(N'dbo.CK_Tenants_Quotas',N'C') IS NOT NULL
        ALTER TABLE dbo.Tenants DROP CONSTRAINT CK_Tenants_Quotas;
    EXEC sys.sp_executesql N'ALTER TABLE dbo.Tenants WITH CHECK ADD CONSTRAINT CK_Tenants_Quotas CHECK(MaxStores>0 AND MaxUsers>0 AND MaxStaff>0 AND MaxCameras>0);';

    IF COL_LENGTH('dbo.TenantSubscriptions','TrialEndUtc') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD TrialEndUtc DATETIME2(7) NULL;
    IF COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodStartUtc') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD CurrentPeriodStartUtc DATETIME2(7) NULL;
    IF COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodEndUtc') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD CurrentPeriodEndUtc DATETIME2(7) NULL;
    IF COL_LENGTH('dbo.TenantSubscriptions','CancelAtPeriodEnd') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD CancelAtPeriodEnd BIT NOT NULL CONSTRAINT DF_TenantSubscriptions_CancelAtPeriodEnd DEFAULT(0);
    IF COL_LENGTH('dbo.TenantSubscriptions','CancelledUtc') IS NULL
        ALTER TABLE dbo.TenantSubscriptions ADD CancelledUtc DATETIME2(7) NULL;
    EXEC sys.sp_executesql N'UPDATE dbo.TenantSubscriptions SET CurrentPeriodStartUtc=COALESCE(CurrentPeriodStartUtc,StartsUtc),CurrentPeriodEndUtc=COALESCE(CurrentPeriodEndUtc,EndsUtc),CancelAtPeriodEnd=CASE WHEN AutoRenew=0 THEN 1 ELSE CancelAtPeriodEnd END;';
    IF OBJECT_ID(N'dbo.CK_TenantSubscriptions_CurrentPeriod',N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.TenantSubscriptions WITH CHECK ADD CONSTRAINT CK_TenantSubscriptions_CurrentPeriod CHECK(CurrentPeriodEndUtc IS NULL OR (CurrentPeriodStartUtc IS NOT NULL AND CurrentPeriodEndUtc>CurrentPeriodStartUtc));';

    /* =========================================================
       9C — Platform invoices. Never use RetailInvoices here.
       ========================================================= */
    IF OBJECT_ID(N'dbo.PlatformInvoices',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PlatformInvoices(
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformInvoices PRIMARY KEY,
            TenantId BIGINT NOT NULL,
            TenantSubscriptionId BIGINT NOT NULL,
            InvoiceNumber NVARCHAR(60) NOT NULL,
            Currency CHAR(3) NOT NULL,
            InvoiceUtc DATETIME2(7) NOT NULL,
            DueUtc DATETIME2(7) NOT NULL,
            Status TINYINT NOT NULL,
            Subtotal DECIMAL(19,4) NOT NULL,
            DiscountAmount DECIMAL(19,4) NOT NULL,
            TaxAmount DECIMAL(19,4) NOT NULL,
            Total DECIMAL(19,4) NOT NULL,
            PaidAmount DECIMAL(19,4) NOT NULL,
            CreatedUtc DATETIME2(7) NOT NULL,
            UpdatedUtc DATETIME2(7) NOT NULL,
            RowVersion BINARY(16) NOT NULL,
            CONSTRAINT FK_PlatformInvoices_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
            CONSTRAINT FK_PlatformInvoices_TenantSubscriptions FOREIGN KEY(TenantSubscriptionId) REFERENCES dbo.TenantSubscriptions(Id),
            CONSTRAINT CK_PlatformInvoices_Status CHECK(Status BETWEEN 1 AND 5),
            CONSTRAINT CK_PlatformInvoices_Amounts CHECK(Subtotal>=0 AND DiscountAmount>=0 AND TaxAmount>=0 AND Total>=0 AND PaidAmount>=0 AND PaidAmount<=Total),
            CONSTRAINT CK_PlatformInvoices_Due CHECK(DueUtc>=InvoiceUtc)
        );
    END;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformInvoices') AND name=N'UX_PlatformInvoices_Tenant_Number')
        CREATE UNIQUE INDEX UX_PlatformInvoices_Tenant_Number ON dbo.PlatformInvoices(TenantId,InvoiceNumber);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformInvoices') AND name=N'IX_PlatformInvoices_Tenant_Status_Date')
        CREATE INDEX IX_PlatformInvoices_Tenant_Status_Date ON dbo.PlatformInvoices(TenantId,Status,InvoiceUtc DESC);

    IF OBJECT_ID(N'dbo.PlatformInvoiceItems',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PlatformInvoiceItems(
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformInvoiceItems PRIMARY KEY,
            TenantId BIGINT NOT NULL,
            PlatformInvoiceId BIGINT NOT NULL,
            SubscriptionPlanId BIGINT NULL,
            PlanName NVARCHAR(150) NOT NULL,
            Description NVARCHAR(500) NULL,
            Quantity DECIMAL(19,4) NOT NULL,
            Rate DECIMAL(19,4) NOT NULL,
            DiscountAmount DECIMAL(19,4) NOT NULL,
            TaxAmount DECIMAL(19,4) NOT NULL,
            Subtotal DECIMAL(19,4) NOT NULL,
            Total DECIMAL(19,4) NOT NULL,
            CreatedUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_PlatformInvoiceItems_PlatformInvoices FOREIGN KEY(PlatformInvoiceId) REFERENCES dbo.PlatformInvoices(Id) ON DELETE CASCADE,
            CONSTRAINT CK_PlatformInvoiceItems_Amounts CHECK(Quantity>0 AND Rate>=0 AND DiscountAmount>=0 AND TaxAmount>=0 AND Subtotal>=0 AND Total>=0)
        );
    END;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformInvoiceItems') AND name=N'IX_PlatformInvoiceItems_Tenant_Invoice')
        CREATE INDEX IX_PlatformInvoiceItems_Tenant_Invoice ON dbo.PlatformInvoiceItems(TenantId,PlatformInvoiceId);

    /* =========================================================
       9D — Provider-neutral idempotent platform payments
       ========================================================= */
    IF OBJECT_ID(N'dbo.PlatformPayments',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PlatformPayments(
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformPayments PRIMARY KEY,
            TenantId BIGINT NOT NULL,
            PlatformInvoiceId BIGINT NOT NULL,
            PaymentMethod NVARCHAR(50) NOT NULL,
            Amount DECIMAL(19,4) NOT NULL,
            Currency CHAR(3) NOT NULL,
            GatewayReference NVARCHAR(150) NULL,
            TransactionReference NVARCHAR(150) NOT NULL,
            PaymentUtc DATETIME2(7) NOT NULL,
            Status TINYINT NOT NULL,
            CreatedUtc DATETIME2(7) NOT NULL,
            UpdatedUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_PlatformPayments_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
            CONSTRAINT FK_PlatformPayments_PlatformInvoices FOREIGN KEY(PlatformInvoiceId) REFERENCES dbo.PlatformInvoices(Id),
            CONSTRAINT CK_PlatformPayments_Status CHECK(Status BETWEEN 1 AND 4),
            CONSTRAINT CK_PlatformPayments_Amount CHECK(Amount>0)
        );
    END;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformPayments') AND name=N'UX_PlatformPayments_Tenant_TransactionReference')
        CREATE UNIQUE INDEX UX_PlatformPayments_Tenant_TransactionReference ON dbo.PlatformPayments(TenantId,TransactionReference);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PlatformPayments') AND name=N'IX_PlatformPayments_Tenant_Invoice_Status')
        CREATE INDEX IX_PlatformPayments_Tenant_Invoice_Status ON dbo.PlatformPayments(TenantId,PlatformInvoiceId,Status);

    /* =========================================================
       9F — Platform-admin vs tenant read-only permission split.
       Repair names left by an earlier failed Phase 9 attempt if present.
       ========================================================= */
    IF EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'PlatformBilling.Subscriptions.View')
       AND NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'TenantPlatformBilling.Subscriptions.View')
        UPDATE dbo.Permissions SET Name=N'TenantPlatformBilling.Subscriptions.View',Description=N'View this tenant CustSearch platform subscription.' WHERE Scope=2 AND Name=N'PlatformBilling.Subscriptions.View';
    IF EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'PlatformBilling.Invoices.View')
       AND NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'TenantPlatformBilling.Invoices.View')
        UPDATE dbo.Permissions SET Name=N'TenantPlatformBilling.Invoices.View',Description=N'View this tenant CustSearch platform invoices.' WHERE Scope=2 AND Name=N'PlatformBilling.Invoices.View';
    IF EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'PlatformBilling.Payments.View')
       AND NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'TenantPlatformBilling.Payments.View')
        UPDATE dbo.Permissions SET Name=N'TenantPlatformBilling.Payments.View',Description=N'View this tenant CustSearch platform payments.' WHERE Scope=2 AND Name=N'PlatformBilling.Payments.View';

    DECLARE @PlatformPermissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
    INSERT INTO @PlatformPermissions VALUES
      (N'PlatformBilling.Plans.View',N'View CustSearch subscription plans.'),
      (N'PlatformBilling.Plans.Manage',N'Manage CustSearch subscription plans.'),
      (N'PlatformBilling.Subscriptions.View',N'View tenant platform subscriptions.'),
      (N'PlatformBilling.Subscriptions.Manage',N'Manage tenant platform subscriptions, invoices and payment callbacks.'),
      (N'PlatformBilling.Invoices.View',N'View CustSearch platform invoices.'),
      (N'PlatformBilling.Payments.View',N'View CustSearch platform payments.');
    INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
    SELECT 1,p.Name,p.Description,1,SYSUTCDATETIME() FROM @PlatformPermissions p
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);

    DECLARE @TenantPermissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
    INSERT INTO @TenantPermissions VALUES
      (N'TenantPlatformBilling.Subscriptions.View',N'View this tenant CustSearch platform subscription.'),
      (N'TenantPlatformBilling.Invoices.View',N'View this tenant CustSearch platform invoices.'),
      (N'TenantPlatformBilling.Payments.View',N'View this tenant CustSearch platform payments.');
    INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
    SELECT 2,p.Name,p.Description,1,SYSUTCDATETIME() FROM @TenantPermissions p
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);

    INSERT INTO dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id
    FROM dbo.Roles r
    JOIN dbo.Permissions p ON p.Scope=1 AND p.IsActive=1 AND p.Name LIKE N'PlatformBilling.%'
    WHERE r.Scope=1 AND r.IsActive=1 AND (
      r.NormalizedName IN(N'PLATFORMSUPERADMIN',N'PLATFORMBILLINGADMIN') OR
      (r.NormalizedName IN(N'PLATFORMOPERATIONSADMIN',N'PLATFORMAUDITOR') AND p.Name IN(N'PlatformBilling.Plans.View',N'PlatformBilling.Subscriptions.View',N'PlatformBilling.Invoices.View',N'PlatformBilling.Payments.View')))
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);

    INSERT INTO dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id
    FROM dbo.Roles r
    JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1
      AND p.Name IN(N'TenantPlatformBilling.Subscriptions.View',N'TenantPlatformBilling.Invoices.View',N'TenantPlatformBilling.Payments.View')
    WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER',N'AUDITOR')
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);

    /* =========================================================
       9G — Read-only stored procedures for platform/tenant views.
       Tenant API callers pass TenantId from trusted server context.
       ========================================================= */
    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Plan_List
    @IncludeInactive BIT=0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,PlanCode,PlanName AS [Name],Description,MonthlyPrice,AnnualPrice,Currency,TrialDays,MaxStores,MaxUsers,MaxStaff,MaxCameras,MaxMonthlyRecognitions,MaxMonthlyApiCalls,FeatureLimitsJson,IsActive,DisplayOrder,CreatedUtc,UpdatedUtc
    FROM dbo.SubscriptionPlans
    WHERE @IncludeInactive=1 OR IsActive=1
    ORDER BY DisplayOrder,PlanName,Id;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Subscription_List
    @TenantId BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ts.Id,ts.TenantId,t.TenantCode,t.DisplayName AS TenantName,ts.SubscriptionPlanId AS PlanId,sp.PlanCode,sp.PlanName,ts.BillingCycle,ts.Status,ts.StartsUtc AS StartUtc,ts.TrialEndUtc,ts.CurrentPeriodStartUtc,ts.CurrentPeriodEndUtc,ts.CancelAtPeriodEnd,ts.CancelledUtc,t.MaxStores,t.MaxUsers,t.MaxStaff,t.MaxCameras
    FROM dbo.TenantSubscriptions ts
    INNER JOIN dbo.Tenants t ON t.Id=ts.TenantId
    INNER JOIN dbo.SubscriptionPlans sp ON sp.Id=ts.SubscriptionPlanId
    WHERE @TenantId IS NULL OR ts.TenantId=@TenantId
    ORDER BY ts.StartsUtc DESC,ts.Id DESC;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Invoice_List
    @TenantId BIGINT=NULL,
    @PageNumber INT=1,
    @PageSize INT=50
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber<1 SET @PageNumber=1;
    IF @PageSize<1 SET @PageSize=50;
    IF @PageSize>200 SET @PageSize=200;
    SELECT i.Id,i.TenantId,i.TenantSubscriptionId,i.InvoiceNumber,i.Currency,i.InvoiceUtc,i.DueUtc,i.Status,i.Subtotal,i.DiscountAmount,i.TaxAmount,i.Total,i.PaidAmount,i.CreatedUtc,i.UpdatedUtc,COUNT_BIG(1) OVER() AS TotalCount
    FROM dbo.PlatformInvoices i
    WHERE @TenantId IS NULL OR i.TenantId=@TenantId
    ORDER BY i.InvoiceUtc DESC,i.Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Invoice_Get
    @PlatformInvoiceId BIGINT,
    @TenantId BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.Id,i.TenantId,i.TenantSubscriptionId,i.InvoiceNumber,i.Currency,i.InvoiceUtc,i.DueUtc,i.Status,i.Subtotal,i.DiscountAmount,i.TaxAmount,i.Total,i.PaidAmount,i.CreatedUtc,i.UpdatedUtc
    FROM dbo.PlatformInvoices i
    WHERE i.Id=@PlatformInvoiceId AND (@TenantId IS NULL OR i.TenantId=@TenantId);
    SELECT x.Id,x.TenantId,x.PlatformInvoiceId,x.SubscriptionPlanId,x.PlanName,x.Description,x.Quantity,x.Rate,x.DiscountAmount,x.TaxAmount,x.Subtotal,x.Total,x.CreatedUtc
    FROM dbo.PlatformInvoiceItems x
    INNER JOIN dbo.PlatformInvoices i ON i.Id=x.PlatformInvoiceId
    WHERE x.PlatformInvoiceId=@PlatformInvoiceId AND (@TenantId IS NULL OR i.TenantId=@TenantId)
    ORDER BY x.Id;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.PlatformBilling_Payment_List
    @TenantId BIGINT=NULL,
    @PlatformInvoiceId BIGINT=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id,p.TenantId,p.PlatformInvoiceId,p.PaymentMethod,p.Amount,p.Currency,p.GatewayReference,p.TransactionReference,p.PaymentUtc,p.Status,p.CreatedUtc,p.UpdatedUtc
    FROM dbo.PlatformPayments p
    WHERE (@TenantId IS NULL OR p.TenantId=@TenantId) AND (@PlatformInvoiceId IS NULL OR p.PlatformInvoiceId=@PlatformInvoiceId)
    ORDER BY p.PaymentUtc DESC,p.Id DESC;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.TenantPlatformBilling_Summary_Get
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    IF @TenantId<=0 THROW 51870,''TenantId must be positive.'',1;
    SELECT TOP(1) t.Id AS TenantId,t.TenantCode,t.DisplayName AS TenantName,ts.Id AS TenantSubscriptionId,ts.SubscriptionPlanId AS PlanId,sp.PlanCode,sp.PlanName,ts.BillingCycle,ts.Status AS SubscriptionStatus,ts.StartsUtc AS StartUtc,ts.TrialEndUtc,ts.CurrentPeriodStartUtc,ts.CurrentPeriodEndUtc AS RenewalUtc,ts.CancelAtPeriodEnd,ts.CancelledUtc,t.MaxStores,t.MaxUsers,t.MaxStaff,t.MaxCameras,
      (SELECT COUNT_BIG(1) FROM dbo.PlatformInvoices pi WHERE pi.TenantId=t.Id) AS InvoiceCount,
      (SELECT TOP(1) pp.Status FROM dbo.PlatformPayments pp WHERE pp.TenantId=t.Id ORDER BY pp.PaymentUtc DESC,pp.Id DESC) AS LatestPaymentStatus
    FROM dbo.Tenants t
    LEFT JOIN dbo.TenantSubscriptions ts ON ts.Id=(SELECT TOP(1) ts2.Id FROM dbo.TenantSubscriptions ts2 WHERE ts2.TenantId=t.Id ORDER BY ts2.StartsUtc DESC,ts2.Id DESC)
    LEFT JOIN dbo.SubscriptionPlans sp ON sp.Id=ts.SubscriptionPlanId
    WHERE t.Id=@TenantId;
END;';

    /* Version row is written only after all Phase 9 objects compile. */
    IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0')
        INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
        VALUES(N'V1.8.0',N'Phase 9 platform plans, authoritative tenant subscriptions, separate platform invoices/payments and billing read procedures',SYSUTCDATETIME(),SUSER_SNAME());

    IF OBJECT_ID(N'dbo.PlatformInvoices',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformInvoiceItems',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformPayments',N'U') IS NULL
        THROW 51880,'Phase 9 platform billing tables are missing.',1;
    IF OBJECT_ID(N'dbo.PlatformBilling_Plan_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Subscription_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Payment_List',N'P') IS NULL OR OBJECT_ID(N'dbo.TenantPlatformBilling_Summary_Get',N'P') IS NULL
        THROW 51881,'Phase 9 stored procedures are missing.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=1 AND Name=N'PlatformBilling.Subscriptions.View') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'TenantPlatformBilling.Subscriptions.View')
        THROW 51882,'Phase 9 platform/tenant permission separation is missing.',1;
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.PlatformInvoices') AND referenced_object_id=OBJECT_ID(N'dbo.RetailInvoices'))
        THROW 51883,'PlatformInvoices must never reference RetailInvoices.',1;
    IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0')<>1
        THROW 51884,'V1.8.0 must exist exactly once.',1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0')<>1 THROW 51890,'V1.8.0 version row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.PlatformInvoices',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformInvoiceItems',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformPayments',N'U') IS NULL THROW 51891,'Phase 9 platform billing tables are missing.',1;
IF COL_LENGTH('dbo.Tenants','MaxStaff') IS NULL OR COL_LENGTH('dbo.SubscriptionPlans','MaxStaff') IS NULL OR COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodEndUtc') IS NULL THROW 51892,'Phase 9 subscription/quota columns are missing.',1;
IF OBJECT_ID(N'dbo.PlatformBilling_Plan_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Subscription_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_List',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Invoice_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformBilling_Payment_List',N'P') IS NULL OR OBJECT_ID(N'dbo.TenantPlatformBilling_Summary_Get',N'P') IS NULL THROW 51893,'Phase 9 platform billing procedures are missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=1 AND Name=N'PlatformBilling.Subscriptions.View') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'TenantPlatformBilling.Subscriptions.View') THROW 51894,'Phase 9 platform/tenant billing permission separation is missing.',1;
