:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON;

DECLARE @TenantId bigint=(SELECT Id FROM dbo.Tenants WHERE TenantCode=N'TEN-35D77F00D7F0');
DECLARE @AdminUserId bigint=(SELECT Id FROM dbo.Users WHERE TenantId=@TenantId AND NormalizedUserName=N'SMOKE.PLATFORM' AND Scope=2 AND IsActive=1);
DECLARE @StoreId bigint=(SELECT Id FROM dbo.Stores WHERE TenantId=@TenantId AND StoreCode=N'UAT-STORE-001');
DECLARE @CameraId bigint=(SELECT Id FROM dbo.Cameras WHERE TenantId=@TenantId AND CameraCode=N'OFFICE-ENTRY-01');
IF @TenantId IS NULL OR @AdminUserId IS NULL OR @StoreId IS NULL OR @CameraId IS NULL THROW 56220,'Tenant UAT identity/store/camera is incomplete.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.UserStoreAssignments WHERE TenantId=@TenantId AND UserId=@AdminUserId AND StoreId=@StoreId) THROW 56221,'Tenant admin store assignment is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.CameraUserPreviewGrants WHERE TenantId=@TenantId AND CameraId=@CameraId AND UserId=@AdminUserId AND CanViewLive=1 AND IsActive=1) THROW 56222,'Tenant admin live-preview grant is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Customers WHERE TenantId=@TenantId AND CustomerCode=N'UAT-CUSTOMER-001') THROW 56223,'Customer page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Households WHERE TenantId=@TenantId AND HouseholdCode=N'UAT-HOUSEHOLD-001') THROW 56224,'Household page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.CustomerVisits WHERE TenantId=@TenantId AND VisitCode=N'UAT-VISIT-001') THROW 56225,'Visit page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.VisitParties WHERE TenantId=@TenantId AND PartyCode=N'UAT-PARTY-001') THROW 56226,'Visit-party page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.ProductCategories WHERE TenantId=@TenantId AND CategoryCode=N'UAT-CAT-001') THROW 56227,'Category page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Products WHERE TenantId=@TenantId AND ProductCode=N'UAT-PRODUCT-001') THROW 56228,'Product page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.VoiceCommandSessions WHERE TenantId=@TenantId AND RecognizedText=N'UAT-VOICE-001') THROW 56229,'Voice-audit page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'UAT-INVOICE-001' AND GrandTotal=125 AND PaidAmount=125) THROW 56230,'Retail page data is missing or invalid.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Alerts WHERE TenantId=@TenantId AND DeduplicationKey=N'UAT-ALERT-001') THROW 56231,'Alerts page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.IntegrationConfigurations WHERE TenantId=@TenantId AND Provider=N'UAT-WEBHOOK' AND Enabled=0) THROW 56232,'Integration page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.CustomerRecognitionConsents WHERE TenantId=@TenantId AND ConsentVersion=N'UAT-CONSENT-V1') THROW 56233,'Recognition-consent data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.ExportJobs WHERE TenantId=@TenantId AND RequestedByUserId=@AdminUserId AND FilterJson=N'{"seed":"UAT-TEN35D77F"}') THROW 56234,'Report/export page data is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AuditLogs WHERE TenantId=@TenantId AND UserId=@AdminUserId AND CorrelationId='UAT-SEED-TEN35D77F') THROW 56235,'User-wise audit row is missing.',1;
IF EXISTS(SELECT 1 FROM dbo.BiometricTemplates WHERE TenantId=@TenantId) THROW 56236,'UAT setup must not create biometric templates.',1;
IF EXISTS(SELECT 1 FROM dbo.Cameras WHERE TenantId=@TenantId AND RtspConfigurationReference LIKE N'rtsp%') THROW 56237,'An RTSP secret was stored in SQL.',1;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;

SELECT N'PASS' Result,@TenantId TenantId,@AdminUserId AdminUserId,@StoreId StoreId,@CameraId CameraId,
       (SELECT COUNT(*) FROM dbo.AuditLogs WHERE TenantId=@TenantId AND UserId=@AdminUserId) UserAuditCount;
GO
