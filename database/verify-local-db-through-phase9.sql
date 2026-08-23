/*
  CustSearch AI — Local Database Verification Through Phase 9
  ---------------------------------------------------------------------------
  Purpose:
  - Run READ-ONLY acceptance checks against the user's existing CustSearch_AI DB.
  - Intended for SSMS / Azure Data Studio on the private KRUTARTH-BHAVSA server.
  - Does NOT create, alter, drop, update or delete application data/schema.
  - Covers completed planning phases 5–8 plus the Phase 9 platform-billing update.

  If this script reports a Phase 9 object missing, run:
      database/run-phase9.sql
  and execute this verifier again.

  Phase boundaries:
  - Phase 8 Retail Billing = shop-customer purchases.
  - Phase 9 Platform Billing = tenant subscription billing paid to CustSearch.
    These domains must remain structurally separate.
*/
USE [CustSearch_AI];
SET NOCOUNT ON;

IF DB_NAME() <> N'CustSearch_AI'
    THROW 52900, 'Run this verifier against the CustSearch_AI database.', 1;

IF OBJECT_ID(N'dbo.DatabaseVersions', N'U') IS NULL
    THROW 52901, 'dbo.DatabaseVersions is missing.', 1;

PRINT '=== 1. DATABASE VERSION LEDGER ===';
DECLARE @RequiredVersions TABLE(VersionNumber NVARCHAR(50) PRIMARY KEY, PhaseName NVARCHAR(100));
INSERT INTO @RequiredVersions VALUES
(N'V1.4.0', N'Phase 5 — Tenant Users / Stores / Staff'),
(N'V1.5.0', N'Phase 6 — Shopper Customers'),
(N'V1.6.0', N'Phase 7 — Households / Visits'),
(N'V1.7.0', N'Phase 8 — Products / Retail Billing'),
(N'V1.8.0', N'Phase 9 — Platform Billing');

SELECT r.VersionNumber, r.PhaseName,
       COUNT(v.VersionNumber) AS AppliedCount,
       CASE WHEN COUNT(v.VersionNumber)=1 THEN N'OK' ELSE N'MISSING_OR_DUPLICATE' END AS Verification
FROM @RequiredVersions r
LEFT JOIN dbo.DatabaseVersions v ON v.VersionNumber=r.VersionNumber
GROUP BY r.VersionNumber,r.PhaseName
ORDER BY r.VersionNumber;

IF EXISTS(
    SELECT 1
    FROM @RequiredVersions r
    LEFT JOIN dbo.DatabaseVersions v ON v.VersionNumber=r.VersionNumber
    GROUP BY r.VersionNumber
    HAVING COUNT(v.VersionNumber)<>1)
    THROW 52902, 'One or more required V1.4.0-V1.8.0 version rows are missing or duplicated.', 1;

PRINT '=== 2. PHASE 5-9 REQUIRED TABLES ===';
DECLARE @RequiredTables TABLE(PhaseNo INT, ObjectName SYSNAME PRIMARY KEY);
INSERT INTO @RequiredTables VALUES
(5,N'Stores'),(5,N'UserStoreAssignments'),(5,N'StaffProfiles'),(5,N'StaffShifts'),(5,N'StaffPresenceSessions'),(5,N'ProductCategories'),(5,N'StoreVoiceCommandSettings'),
(6,N'Customers'),(6,N'AnonymousVisitors'),(6,N'CustomerStoreAssignments'),
(7,N'Households'),(7,N'HouseholdMembers'),(7,N'VisitParties'),(7,N'VisitPartyMembers'),(7,N'CustomerVisits'),
(8,N'Products'),(8,N'ProductStoreAvailabilities'),(8,N'RetailInvoices'),(8,N'RetailInvoiceItems'),(8,N'RetailInvoicePayments'),(8,N'RetailInvoiceParticipants'),(8,N'RetailInvoiceItemAttributions'),
(9,N'PlatformInvoices'),(9,N'PlatformInvoiceItems'),(9,N'PlatformPayments');

SELECT t.PhaseNo,t.ObjectName,
       CASE WHEN OBJECT_ID(N'dbo.'+t.ObjectName,N'U') IS NOT NULL THEN N'OK' ELSE N'MISSING' END AS Verification
FROM @RequiredTables t ORDER BY t.PhaseNo,t.ObjectName;

IF EXISTS(SELECT 1 FROM @RequiredTables WHERE OBJECT_ID(N'dbo.'+ObjectName,N'U') IS NULL)
    THROW 52903, 'One or more required Phase 5-9 tables are missing.', 1;

PRINT '=== 3. PHASE 9 REQUIRED/ALTERED COLUMNS ===';
DECLARE @RequiredColumns TABLE(TableName SYSNAME,ColumnName SYSNAME,PRIMARY KEY(TableName,ColumnName));
INSERT INTO @RequiredColumns VALUES
(N'SubscriptionPlans',N'Description'),(N'SubscriptionPlans',N'Currency'),(N'SubscriptionPlans',N'TrialDays'),(N'SubscriptionPlans',N'MaxStaff'),(N'SubscriptionPlans',N'FeatureLimitsJson'),(N'SubscriptionPlans',N'DisplayOrder'),
(N'Tenants',N'MaxStaff'),
(N'TenantSubscriptions',N'TrialEndUtc'),(N'TenantSubscriptions',N'CurrentPeriodStartUtc'),(N'TenantSubscriptions',N'CurrentPeriodEndUtc'),(N'TenantSubscriptions',N'CancelAtPeriodEnd'),(N'TenantSubscriptions',N'CancelledUtc');

SELECT TableName,ColumnName,
       CASE WHEN COL_LENGTH(N'dbo.'+TableName,ColumnName) IS NOT NULL THEN N'OK' ELSE N'MISSING' END AS Verification
FROM @RequiredColumns ORDER BY TableName,ColumnName;

IF EXISTS(SELECT 1 FROM @RequiredColumns WHERE COL_LENGTH(N'dbo.'+TableName,ColumnName) IS NULL)
    THROW 52904, 'One or more required Phase 9 columns are missing. Run database/run-phase9.sql.', 1;

PRINT '=== 4. PHASE 9 INDEXES ===';
DECLARE @RequiredIndexes TABLE(TableName SYSNAME,IndexName SYSNAME,PRIMARY KEY(TableName,IndexName));
INSERT INTO @RequiredIndexes VALUES
(N'PlatformInvoices',N'UX_PlatformInvoices_Tenant_Number'),
(N'PlatformInvoices',N'IX_PlatformInvoices_Tenant_Status_Date'),
(N'PlatformInvoiceItems',N'IX_PlatformInvoiceItems_Tenant_Invoice'),
(N'PlatformPayments',N'UX_PlatformPayments_Tenant_TransactionReference'),
(N'PlatformPayments',N'IX_PlatformPayments_Tenant_Invoice_Status');

SELECT i.TableName,i.IndexName,
       CASE WHEN EXISTS(SELECT 1 FROM sys.indexes x WHERE x.object_id=OBJECT_ID(N'dbo.'+i.TableName) AND x.name=i.IndexName) THEN N'OK' ELSE N'MISSING' END AS Verification
FROM @RequiredIndexes i ORDER BY i.TableName,i.IndexName;

IF EXISTS(SELECT 1 FROM @RequiredIndexes i WHERE NOT EXISTS(SELECT 1 FROM sys.indexes x WHERE x.object_id=OBJECT_ID(N'dbo.'+i.TableName) AND x.name=i.IndexName))
    THROW 52905, 'One or more required Phase 9 indexes are missing. Run database/run-phase9.sql.', 1;

PRINT '=== 5. PHASE 9 CONSTRAINTS ===';
DECLARE @RequiredConstraints TABLE(ConstraintName SYSNAME PRIMARY KEY);
INSERT INTO @RequiredConstraints VALUES
(N'CK_SubscriptionPlans_Limits'),
(N'CK_Tenants_Quotas'),
(N'CK_TenantSubscriptions_CurrentPeriod'),
(N'CK_PlatformInvoices_Status'),
(N'CK_PlatformInvoices_Amounts'),
(N'CK_PlatformInvoices_Due'),
(N'CK_PlatformInvoiceItems_Amounts'),
(N'CK_PlatformPayments_Status'),
(N'CK_PlatformPayments_Amount');

SELECT c.ConstraintName,
       CASE WHEN OBJECT_ID(N'dbo.'+c.ConstraintName,N'C') IS NOT NULL THEN N'OK' ELSE N'MISSING' END AS Verification
FROM @RequiredConstraints c ORDER BY c.ConstraintName;

IF EXISTS(SELECT 1 FROM @RequiredConstraints WHERE OBJECT_ID(N'dbo.'+ConstraintName,N'C') IS NULL)
    THROW 52906, 'One or more required Phase 9 check constraints are missing. Run database/run-phase9.sql.', 1;

PRINT '=== 6. PHASE 6-9 STORED PROCEDURES ===';
DECLARE @RequiredProcedures TABLE(PhaseNo INT,ProcedureName SYSNAME PRIMARY KEY);
INSERT INTO @RequiredProcedures VALUES
(6,N'Customer_Search'),
(7,N'Household_Search'),(7,N'Household_GetDetail'),(7,N'CustomerVisit_Search'),(7,N'VisitParty_Search'),(7,N'VisitParty_GetDetail'),
(8,N'Product_Search'),(8,N'RetailInvoice_Search'),(8,N'RetailInvoice_GetDetail'),(8,N'CustomerPurchaseHistory_Get'),(8,N'HouseholdPurchaseSummary_Get'),(8,N'RetailSalesSummary_Get'),(8,N'RetailSalesByProduct_Get'),(8,N'RetailSalesByCategory_Get'),(8,N'RetailPaymentSummary_Get'),
(9,N'PlatformBilling_Plan_List'),(9,N'PlatformBilling_Subscription_List'),(9,N'PlatformBilling_Invoice_List'),(9,N'PlatformBilling_Invoice_Get'),(9,N'PlatformBilling_Payment_List'),(9,N'TenantPlatformBilling_Summary_Get');

SELECT p.PhaseNo,p.ProcedureName,
       CASE WHEN OBJECT_ID(N'dbo.'+p.ProcedureName,N'P') IS NOT NULL THEN N'OK' ELSE N'MISSING' END AS Verification
FROM @RequiredProcedures p ORDER BY p.PhaseNo,p.ProcedureName;

IF EXISTS(SELECT 1 FROM @RequiredProcedures WHERE OBJECT_ID(N'dbo.'+ProcedureName,N'P') IS NULL)
    THROW 52907, 'One or more required Phase 6-9 stored procedures are missing.', 1;

PRINT '=== 7. PHASE 9 AUTHORIZATION SEPARATION ===';
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=1 AND Name=N'PlatformBilling.Subscriptions.View')
    THROW 52908, 'Platform-scope PlatformBilling permissions are missing.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'TenantPlatformBilling.Subscriptions.View')
    THROW 52909, 'Tenant-scope TenantPlatformBilling permissions are missing.', 1;
IF EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name IN(N'PlatformBilling.Subscriptions.View',N'PlatformBilling.Invoices.View',N'PlatformBilling.Payments.View'))
    THROW 52910, 'Old duplicate Phase 9 tenant permission names remain; run database/run-phase9.sql to repair them.', 1;

SELECT Scope,Name,IsActive FROM dbo.Permissions
WHERE Name LIKE N'PlatformBilling.%' OR Name LIKE N'TenantPlatformBilling.%'
ORDER BY Scope,Name;

PRINT '=== 8. RETAIL / PLATFORM BILLING DOMAIN SEPARATION ===';
IF EXISTS(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id=OBJECT_ID(N'dbo.PlatformInvoices')
      AND referenced_object_id=OBJECT_ID(N'dbo.RetailInvoices'))
    THROW 52911, 'Invalid domain coupling: PlatformInvoices references RetailInvoices.', 1;

IF EXISTS(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id=OBJECT_ID(N'dbo.RetailInvoices')
      AND referenced_object_id=OBJECT_ID(N'dbo.PlatformInvoices'))
    THROW 52912, 'Invalid domain coupling: RetailInvoices references PlatformInvoices.', 1;

PRINT '=== 9. DATABASE CONSTRAINT HEALTH ===';
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;

PRINT 'LOCAL_DB_PHASE9_VERIFICATION_GREEN';
PRINT 'All checked Phase 5-9 versions/objects and Phase 9 separation rules are present.';
