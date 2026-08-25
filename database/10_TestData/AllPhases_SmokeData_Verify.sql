:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
DECLARE @TenantId bigint=(SELECT Id FROM dbo.Tenants WHERE TenantCode=N'SMOKE-TENANT-001');
DECLARE @StoreId bigint=(SELECT Id FROM dbo.Stores WHERE TenantId=@TenantId AND StoreCode=N'SMOKE-STORE-001');
IF @TenantId IS NULL OR @StoreId IS NULL THROW 56100,'Smoke tenant/store missing.',1;
DECLARE @TenantBId bigint=(SELECT Id FROM dbo.Tenants WHERE TenantCode=N'SMOKE-TENANT-002');
DECLARE @StoreBId bigint=(SELECT Id FROM dbo.Stores WHERE TenantId=@TenantBId AND StoreCode=N'SMOKE-STORE-002');
IF @TenantBId IS NULL OR @StoreBId IS NULL THROW 56112,'Isolation tenant/store missing.',1;
IF (SELECT COUNT(*) FROM dbo.Users WHERE NormalizedEmail IN(N'SMOKE.PLATFORM@CUSTSEARCH.LOCAL',N'SMOKE.TENANTADMIN@CUSTSEARCH.LOCAL',N'SMOKE.STAFF@CUSTSEARCH.LOCAL'))<>3 THROW 56101,'Smoke users incomplete.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Customers WHERE TenantId=@TenantId AND CustomerCode=N'SMOKE-CUSTOMER-001') THROW 56102,'Smoke customer missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Households WHERE TenantId=@TenantId AND HouseholdCode=N'SMOKE-HOUSEHOLD-001') THROW 56103,'Smoke household missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.CustomerVisits WHERE TenantId=@TenantId AND VisitCode=N'SMOKE-VISIT-001') THROW 56104,'Smoke visit missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'SMOKE-INVOICE-001' AND GrandTotal=100 AND PaidAmount=100) THROW 56105,'Smoke retail invoice missing/invalid.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.PlatformInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'SMOKE-PLATFORM-INVOICE-001') THROW 56106,'Smoke platform invoice missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.NotificationOutbox WHERE TenantId=@TenantId AND IdempotencyKey=N'SMOKE-NOTIFY-001') THROW 56107,'Smoke notification missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.IntegrationOutbox WHERE TenantId=@TenantId AND IdempotencyKey=N'SMOKE-INTEGRATION-001') THROW 56108,'Smoke integration event missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Cameras WHERE TenantId=@TenantId AND CameraCode=N'SMOKE-CAMERA-001') THROW 56109,'Smoke camera missing.',1;
IF EXISTS(SELECT 1 FROM dbo.BiometricTemplates WHERE TenantId=@TenantId) THROW 56110,'Smoke seed must not contain biometric templates.',1;
IF EXISTS(SELECT 1 FROM dbo.SecurityIncidents WHERE TenantId=@TenantId) THROW 56111,'Phase 18 live-only data must not be seeded before source reconciliation.',1;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
IF NOT EXISTS(SELECT 1 FROM dbo.Customers WHERE TenantId=@TenantBId AND CustomerCode=N'SMOKE-CUSTOMER-002') THROW 56113,'Isolation customer missing.',1;
SELECT @TenantId TenantId,@StoreId StoreId,@TenantBId IsolationTenantId,@StoreBId IsolationStoreId,N'PASS' Result;
GO
