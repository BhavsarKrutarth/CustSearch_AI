/*
  Phase 9 — Platform Billing standalone SQL Server runner.
  Run after the validated V1.7.0 Phase 8 baseline. Safe to execute repeatedly.
  Platform Billing is intentionally separate from Phase 8 Retail Billing.
*/
USE [CustSearch_AI];
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.7.0')
    THROW 51901,'Phase 9 requires validated V1.7.0.',1;

BEGIN TRANSACTION;
BEGIN TRY
    IF COL_LENGTH('dbo.SubscriptionPlans','Description') IS NULL ALTER TABLE dbo.SubscriptionPlans ADD Description NVARCHAR(1000) NOT NULL CONSTRAINT DF_SubscriptionPlans_Description DEFAULT(N'');
    IF COL_LENGTH('dbo.SubscriptionPlans','Currency') IS NULL ALTER TABLE dbo.SubscriptionPlans ADD Currency CHAR(3) NOT NULL CONSTRAINT DF_SubscriptionPlans_Currency DEFAULT('USD');
    IF COL_LENGTH('dbo.SubscriptionPlans','TrialDays') IS NULL ALTER TABLE dbo.SubscriptionPlans ADD TrialDays INT NOT NULL CONSTRAINT DF_SubscriptionPlans_TrialDays DEFAULT(0);
    IF COL_LENGTH('dbo.SubscriptionPlans','MaxStaff') IS NULL BEGIN ALTER TABLE dbo.SubscriptionPlans ADD MaxStaff INT NULL;UPDATE dbo.SubscriptionPlans SET MaxStaff=MaxUsers WHERE MaxStaff IS NULL;ALTER TABLE dbo.SubscriptionPlans ALTER COLUMN MaxStaff INT NOT NULL;END;
    IF COL_LENGTH('dbo.SubscriptionPlans','FeatureLimitsJson') IS NULL ALTER TABLE dbo.SubscriptionPlans ADD FeatureLimitsJson NVARCHAR(4000) NULL;
    IF COL_LENGTH('dbo.SubscriptionPlans','DisplayOrder') IS NULL ALTER TABLE dbo.SubscriptionPlans ADD DisplayOrder INT NOT NULL CONSTRAINT DF_SubscriptionPlans_DisplayOrder DEFAULT(0);
    IF OBJECT_ID(N'dbo.CK_SubscriptionPlans_Limits',N'C') IS NOT NULL ALTER TABLE dbo.SubscriptionPlans DROP CONSTRAINT CK_SubscriptionPlans_Limits;
    ALTER TABLE dbo.SubscriptionPlans WITH CHECK ADD CONSTRAINT CK_SubscriptionPlans_Limits CHECK(MaxStores>0 AND MaxUsers>0 AND MaxStaff>0 AND MaxCameras>0 AND (MaxMonthlyRecognitions IS NULL OR MaxMonthlyRecognitions>0) AND (MaxMonthlyApiCalls IS NULL OR MaxMonthlyApiCalls>0));
    IF OBJECT_ID(N'dbo.CK_SubscriptionPlans_TrialDisplay',N'C') IS NULL ALTER TABLE dbo.SubscriptionPlans WITH CHECK ADD CONSTRAINT CK_SubscriptionPlans_TrialDisplay CHECK(TrialDays>=0 AND DisplayOrder>=0);
    IF OBJECT_ID(N'dbo.CK_SubscriptionPlans_FeatureLimitsJson',N'C') IS NULL ALTER TABLE dbo.SubscriptionPlans WITH CHECK ADD CONSTRAINT CK_SubscriptionPlans_FeatureLimitsJson CHECK(FeatureLimitsJson IS NULL OR ISJSON(FeatureLimitsJson)=1);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SubscriptionPlans') AND name=N'IX_SubscriptionPlans_Display') CREATE INDEX IX_SubscriptionPlans_Display ON dbo.SubscriptionPlans(IsActive,DisplayOrder,PlanName);

    IF COL_LENGTH('dbo.Tenants','MaxStaff') IS NULL BEGIN ALTER TABLE dbo.Tenants ADD MaxStaff INT NULL;UPDATE dbo.Tenants SET MaxStaff=MaxUsers WHERE MaxStaff IS NULL;ALTER TABLE dbo.Tenants ALTER COLUMN MaxStaff INT NOT NULL;END;
    IF OBJECT_ID(N'dbo.CK_Tenants_Quotas',N'C') IS NOT NULL ALTER TABLE dbo.Tenants DROP CONSTRAINT CK_Tenants_Quotas;
    ALTER TABLE dbo.Tenants WITH CHECK ADD CONSTRAINT CK_Tenants_Quotas CHECK(MaxStores>0 AND MaxUsers>0 AND MaxStaff>0 AND MaxCameras>0);

    IF COL_LENGTH('dbo.TenantSubscriptions','TrialEndUtc') IS NULL ALTER TABLE dbo.TenantSubscriptions ADD TrialEndUtc DATETIME2(7) NULL;
    IF COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodStartUtc') IS NULL ALTER TABLE dbo.TenantSubscriptions ADD CurrentPeriodStartUtc DATETIME2(7) NULL;
    IF COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodEndUtc') IS NULL ALTER TABLE dbo.TenantSubscriptions ADD CurrentPeriodEndUtc DATETIME2(7) NULL;
    IF COL_LENGTH('dbo.TenantSubscriptions','CancelAtPeriodEnd') IS NULL ALTER TABLE dbo.TenantSubscriptions ADD CancelAtPeriodEnd BIT NOT NULL CONSTRAINT DF_TenantSubscriptions_CancelAtPeriodEnd DEFAULT(0);
    IF COL_LENGTH('dbo.TenantSubscriptions','CancelledUtc') IS NULL ALTER TABLE dbo.TenantSubscriptions ADD CancelledUtc DATETIME2(7) NULL;
    UPDATE dbo.TenantSubscriptions SET CurrentPeriodStartUtc=COALESCE(CurrentPeriodStartUtc,StartsUtc),CurrentPeriodEndUtc=COALESCE(CurrentPeriodEndUtc,EndsUtc),CancelAtPeriodEnd=CASE WHEN AutoRenew=0 THEN 1 ELSE CancelAtPeriodEnd END;
    IF OBJECT_ID(N'dbo.CK_TenantSubscriptions_CurrentPeriod',N'C') IS NULL ALTER TABLE dbo.TenantSubscriptions WITH CHECK ADD CONSTRAINT CK_TenantSubscriptions_CurrentPeriod CHECK(CurrentPeriodEndUtc IS NULL OR (CurrentPeriodStartUtc IS NOT NULL AND CurrentPeriodEndUtc>CurrentPeriodStartUtc));

    IF OBJECT_ID(N'dbo.PlatformInvoices',N'U') IS NULL BEGIN
      CREATE TABLE dbo.PlatformInvoices(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformInvoices PRIMARY KEY,TenantId BIGINT NOT NULL,TenantSubscriptionId BIGINT NOT NULL,InvoiceNumber NVARCHAR(60) NOT NULL,Currency CHAR(3) NOT NULL,InvoiceUtc DATETIME2(7) NOT NULL,DueUtc DATETIME2(7) NOT NULL,Status TINYINT NOT NULL,Subtotal DECIMAL(19,4) NOT NULL,DiscountAmount DECIMAL(19,4) NOT NULL,TaxAmount DECIMAL(19,4) NOT NULL,Total DECIMAL(19,4) NOT NULL,PaidAmount DECIMAL(19,4) NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion BINARY(16) NOT NULL,
      CONSTRAINT FK_PlatformInvoices_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_PlatformInvoices_TenantSubscriptions FOREIGN KEY(TenantSubscriptionId) REFERENCES dbo.TenantSubscriptions(Id),CONSTRAINT CK_PlatformInvoices_Status CHECK(Status BETWEEN 1 AND 5),CONSTRAINT CK_PlatformInvoices_Amounts CHECK(Subtotal>=0 AND DiscountAmount>=0 AND TaxAmount>=0 AND Total>=0 AND PaidAmount>=0 AND PaidAmount<=Total),CONSTRAINT CK_PlatformInvoices_Due CHECK(DueUtc>=InvoiceUtc));
      CREATE UNIQUE INDEX UX_PlatformInvoices_Tenant_Number ON dbo.PlatformInvoices(TenantId,InvoiceNumber);CREATE INDEX IX_PlatformInvoices_Tenant_Status_Date ON dbo.PlatformInvoices(TenantId,Status,InvoiceUtc DESC);
    END;

    IF OBJECT_ID(N'dbo.PlatformInvoiceItems',N'U') IS NULL BEGIN
      CREATE TABLE dbo.PlatformInvoiceItems(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformInvoiceItems PRIMARY KEY,TenantId BIGINT NOT NULL,PlatformInvoiceId BIGINT NOT NULL,SubscriptionPlanId BIGINT NULL,PlanName NVARCHAR(150) NOT NULL,Description NVARCHAR(500) NULL,Quantity DECIMAL(19,4) NOT NULL,Rate DECIMAL(19,4) NOT NULL,DiscountAmount DECIMAL(19,4) NOT NULL,TaxAmount DECIMAL(19,4) NOT NULL,Subtotal DECIMAL(19,4) NOT NULL,Total DECIMAL(19,4) NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,
      CONSTRAINT FK_PlatformInvoiceItems_PlatformInvoices FOREIGN KEY(PlatformInvoiceId) REFERENCES dbo.PlatformInvoices(Id) ON DELETE CASCADE,CONSTRAINT CK_PlatformInvoiceItems_Amounts CHECK(Quantity>0 AND Rate>=0 AND DiscountAmount>=0 AND TaxAmount>=0 AND Subtotal>=0 AND Total>=0));
      CREATE INDEX IX_PlatformInvoiceItems_Tenant_Invoice ON dbo.PlatformInvoiceItems(TenantId,PlatformInvoiceId);
    END;

    IF OBJECT_ID(N'dbo.PlatformPayments',N'U') IS NULL BEGIN
      CREATE TABLE dbo.PlatformPayments(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformPayments PRIMARY KEY,TenantId BIGINT NOT NULL,PlatformInvoiceId BIGINT NOT NULL,PaymentMethod NVARCHAR(50) NOT NULL,Amount DECIMAL(19,4) NOT NULL,Currency CHAR(3) NOT NULL,GatewayReference NVARCHAR(150) NULL,TransactionReference NVARCHAR(150) NOT NULL,PaymentUtc DATETIME2(7) NOT NULL,Status TINYINT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,
      CONSTRAINT FK_PlatformPayments_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_PlatformPayments_PlatformInvoices FOREIGN KEY(PlatformInvoiceId) REFERENCES dbo.PlatformInvoices(Id),CONSTRAINT CK_PlatformPayments_Status CHECK(Status BETWEEN 1 AND 4),CONSTRAINT CK_PlatformPayments_Amount CHECK(Amount>0));
      CREATE UNIQUE INDEX UX_PlatformPayments_Tenant_TransactionReference ON dbo.PlatformPayments(TenantId,TransactionReference);CREATE INDEX IX_PlatformPayments_Tenant_Invoice_Status ON dbo.PlatformPayments(TenantId,PlatformInvoiceId,Status);
    END;

    DECLARE @PP TABLE(Name NVARCHAR(150),Description NVARCHAR(300));INSERT INTO @PP VALUES(N'PlatformBilling.Plans.View',N'View CustSearch subscription plans.'),(N'PlatformBilling.Plans.Manage',N'Manage CustSearch subscription plans.'),(N'PlatformBilling.Subscriptions.View',N'View tenant platform subscriptions.'),(N'PlatformBilling.Subscriptions.Manage',N'Manage tenant platform subscriptions, invoices and payment callbacks.'),(N'PlatformBilling.Invoices.View',N'View CustSearch platform invoices.'),(N'PlatformBilling.Payments.View',N'View CustSearch platform payments.');
    INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) SELECT 1,p.Name,p.Description,1,SYSUTCDATETIME() FROM @PP p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=1 AND x.Name=p.Name);
    DECLARE @TP TABLE(Name NVARCHAR(150),Description NVARCHAR(300));INSERT INTO @TP VALUES(N'PlatformBilling.Subscriptions.View',N'View this tenant platform subscription.'),(N'PlatformBilling.Invoices.View',N'View this tenant platform invoices.'),(N'PlatformBilling.Payments.View',N'View this tenant platform payments.');
    INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) SELECT 2,p.Name,p.Description,1,SYSUTCDATETIME() FROM @TP p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
    INSERT INTO dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=1 AND p.IsActive=1 AND p.Name LIKE N'PlatformBilling.%' WHERE r.Scope=1 AND r.IsActive=1 AND (r.NormalizedName IN(N'PLATFORMSUPERADMIN',N'PLATFORMBILLINGADMIN') OR (r.NormalizedName IN(N'PLATFORMOPERATIONSADMIN',N'PLATFORMAUDITOR') AND p.Name IN(N'PlatformBilling.Plans.View',N'PlatformBilling.Subscriptions.View',N'PlatformBilling.Invoices.View',N'PlatformBilling.Payments.View'))) AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
    INSERT INTO dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 AND p.Name IN(N'PlatformBilling.Subscriptions.View',N'PlatformBilling.Invoices.View',N'PlatformBilling.Payments.View') WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER',N'AUDITOR') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);

    IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0') INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.8.0',N'Phase 9 platform plans, authoritative tenant subscriptions, separate platform invoices and idempotent platform payments',SYSUTCDATETIME(),SUSER_SNAME());
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0')<>1 THROW 51990,'V1.8.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.PlatformInvoices',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformInvoiceItems',N'U') IS NULL OR OBJECT_ID(N'dbo.PlatformPayments',N'U') IS NULL THROW 51991,'Phase 9 platform tables missing.',1;
IF COL_LENGTH('dbo.SubscriptionPlans','MaxStaff') IS NULL OR COL_LENGTH('dbo.Tenants','MaxStaff') IS NULL OR COL_LENGTH('dbo.TenantSubscriptions','CurrentPeriodEndUtc') IS NULL THROW 51992,'Phase 9 subscription columns missing.',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.PlatformInvoices') AND referenced_object_id=OBJECT_ID(N'dbo.RetailInvoices')) THROW 51993,'Platform invoices must not reference RetailInvoices.',1;
PRINT 'Phase 9 Platform Billing SQL Server script completed and validated successfully.';
SELECT VersionNumber,Description,AppliedUtc,AppliedBy FROM dbo.DatabaseVersions WHERE VersionNumber IN(N'V1.7.0',N'V1.8.0') ORDER BY VersionNumber;
SELECT name AS Phase9Table FROM sys.tables WHERE name IN(N'PlatformInvoices',N'PlatformInvoiceItems',N'PlatformPayments') ORDER BY name;
