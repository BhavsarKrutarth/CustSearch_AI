:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF N'$(ConfirmCleanup)'<>N'DELETE-SMOKE-TENANT-001'
    THROW 56200,'Exact ConfirmCleanup token is required.',1;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @TenantId bigint=(SELECT Id FROM dbo.Tenants WHERE TenantCode=N'SMOKE-TENANT-001');
    IF @TenantId IS NULL BEGIN ROLLBACK TRANSACTION;SELECT N'NOT_FOUND' Result;RETURN;END;
    DECLARE @StoreId bigint=(SELECT Id FROM dbo.Stores WHERE TenantId=@TenantId AND StoreCode=N'SMOKE-STORE-001');
    DECLARE @PlatformUserId bigint=(SELECT Id FROM dbo.Users WHERE NormalizedEmail=N'SMOKE.PLATFORM@CUSTSEARCH.LOCAL' AND TenantId IS NULL);
    DECLARE @TenantUsers TABLE(Id bigint PRIMARY KEY);INSERT @TenantUsers SELECT Id FROM dbo.Users WHERE TenantId=@TenantId AND NormalizedEmail IN(N'SMOKE.TENANTADMIN@CUSTSEARCH.LOCAL',N'SMOKE.STAFF@CUSTSEARCH.LOCAL');
    DECLARE @InvoiceId bigint=(SELECT Id FROM dbo.RetailInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'SMOKE-INVOICE-001');
    DECLARE @PlatformInvoiceId bigint=(SELECT Id FROM dbo.PlatformInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'SMOKE-PLATFORM-INVOICE-001');
    DECLARE @AlertId bigint=(SELECT Id FROM dbo.Alerts WHERE TenantId=@TenantId AND DeduplicationKey=N'SMOKE-ALERT-001');
    DECLARE @IntegrationId bigint=(SELECT Id FROM dbo.IntegrationConfigurations WHERE TenantId=@TenantId AND Provider=N'SMOKE-WEBHOOK');
    DECLARE @CameraId bigint=(SELECT Id FROM dbo.Cameras WHERE TenantId=@TenantId AND CameraCode=N'SMOKE-CAMERA-001');
    DECLARE @TenantBId bigint=(SELECT Id FROM dbo.Tenants WHERE TenantCode=N'SMOKE-TENANT-002');

    IF EXISTS(SELECT 1 FROM dbo.SecurityIncidents WHERE TenantId=@TenantId) THROW 56201,'Cleanup refused: Phase 18 data exists for smoke tenant.',1;
    DELETE dbo.OperationalSettings WHERE TenantId=@TenantId AND [Key]=N'Smoke.DemoMode';
    DELETE dbo.ReportExportEvents WHERE ReportExportJobId IN(SELECT Id FROM dbo.ReportExportJobs WHERE TenantId=@TenantId);
    DELETE dbo.ReportExportJobs WHERE TenantId=@TenantId AND ReportType=N'Tenant.SmokeOperational';
    DELETE dbo.RecognitionCandidates WHERE TenantId=@TenantId;DELETE dbo.BiometricTemplates WHERE TenantId=@TenantId;DELETE dbo.CustomerRecognitionConsents WHERE TenantId=@TenantId AND ConsentVersion=N'SMOKE-CONSENT-V1';
    DELETE dbo.CameraTrackHandoffs WHERE TenantId=@TenantId;DELETE dbo.PersonTrackSessions WHERE TenantId=@TenantId AND PersonTrackId=N'SMOKE-TRACK-001';DELETE dbo.CameraZoneConfigurations WHERE TenantId=@TenantId AND CameraId=@CameraId;DELETE dbo.CameraOperationalEvents WHERE TenantId=@TenantId AND CameraId=@CameraId;DELETE dbo.Cameras WHERE TenantId=@TenantId AND CameraCode=N'SMOKE-CAMERA-001';
    DELETE dbo.IntegrationDeliveryLogs WHERE TenantId=@TenantId AND IntegrationConfigurationId=@IntegrationId;DELETE dbo.IntegrationInboundEvents WHERE TenantId=@TenantId AND IntegrationConfigurationId=@IntegrationId;DELETE dbo.IntegrationOutbox WHERE TenantId=@TenantId AND IntegrationConfigurationId=@IntegrationId;DELETE dbo.IntegrationConfigurations WHERE TenantId=@TenantId AND Provider=N'SMOKE-WEBHOOK';
    DELETE dbo.NotificationOutbox WHERE TenantId=@TenantId AND IdempotencyKey=N'SMOKE-NOTIFY-001';DELETE dbo.RealtimeEvents WHERE TenantId=@TenantId AND DeduplicationKey=N'SMOKE-REALTIME-001';DELETE dbo.Alerts WHERE TenantId=@TenantId AND DeduplicationKey=N'SMOKE-ALERT-001';
    DELETE dbo.HouseholdPreferenceTags WHERE TenantId=@TenantId;DELETE dbo.CustomerPreferenceScores WHERE TenantId=@TenantId;DELETE dbo.CustomerPreferenceSignals WHERE TenantId=@TenantId;DELETE dbo.VoiceCommandSessions WHERE TenantId=@TenantId;
    DELETE dbo.PlatformPayments WHERE TenantId=@TenantId AND PlatformInvoiceId=@PlatformInvoiceId;DELETE dbo.PlatformInvoiceItems WHERE TenantId=@TenantId AND PlatformInvoiceId=@PlatformInvoiceId;DELETE dbo.PlatformInvoices WHERE TenantId=@TenantId AND Id=@PlatformInvoiceId;
    DELETE dbo.RetailInvoiceItemAttributions WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId;DELETE dbo.RetailInvoicePayments WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId;DELETE dbo.RetailInvoiceParticipants WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId;DELETE dbo.RetailInvoiceItems WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId;DELETE dbo.RetailInvoices WHERE TenantId=@TenantId AND Id=@InvoiceId;
    DELETE dbo.ProductStoreAvailabilities WHERE TenantId=@TenantId;DELETE dbo.Products WHERE TenantId=@TenantId AND ProductCode=N'SMOKE-PRODUCT-001';
    DELETE dbo.CustomerVisits WHERE TenantId=@TenantId AND VisitCode=N'SMOKE-VISIT-001';DELETE dbo.VisitPartyMembers WHERE TenantId=@TenantId;DELETE dbo.VisitParties WHERE TenantId=@TenantId AND PartyCode=N'SMOKE-PARTY-001';DELETE dbo.HouseholdMembers WHERE TenantId=@TenantId;DELETE dbo.Households WHERE TenantId=@TenantId AND HouseholdCode=N'SMOKE-HOUSEHOLD-001';
    DELETE dbo.AnonymousVisitors WHERE TenantId=@TenantId AND VisitorCode=N'SMOKE-VISITOR-001';DELETE dbo.CustomerStoreAssignments WHERE TenantId=@TenantId;DELETE dbo.Customers WHERE TenantId=@TenantId AND CustomerCode=N'SMOKE-CUSTOMER-001';
    DELETE dbo.StoreVoiceCommandAliases WHERE TenantId=@TenantId;DELETE dbo.StoreVoiceCommandRuntimeSettings WHERE TenantId=@TenantId;DELETE dbo.StoreVoiceCommandSettings WHERE TenantId=@TenantId;DELETE dbo.ProductCategoryAliases WHERE TenantId=@TenantId;DELETE dbo.ProductCategories WHERE TenantId=@TenantId AND CategoryCode=N'SMOKE-CAT-001';
    DELETE dbo.StaffPresenceSessions WHERE TenantId=@TenantId;DELETE dbo.StaffShifts WHERE TenantId=@TenantId;DELETE dbo.StaffProfiles WHERE TenantId=@TenantId AND EmployeeCode=N'SMOKE-STAFF-001';DELETE dbo.UserStoreAssignments WHERE TenantId=@TenantId;
    DELETE dbo.RolePermissions WHERE RoleId IN(SELECT Id FROM dbo.Roles WHERE TenantId=@TenantId);DELETE dbo.UserRoles WHERE UserId IN(SELECT Id FROM @TenantUsers);DELETE dbo.Users WHERE Id IN(SELECT Id FROM @TenantUsers);DELETE dbo.Roles WHERE TenantId=@TenantId;
    DELETE dbo.TenantSubscriptions WHERE TenantId=@TenantId;DELETE dbo.TenantQuotaOverrides WHERE TenantId=@TenantId;DELETE dbo.TenantUsageSnapshots WHERE TenantId=@TenantId;DELETE dbo.Stores WHERE TenantId=@TenantId AND StoreCode=N'SMOKE-STORE-001';DELETE dbo.Tenants WHERE Id=@TenantId;
    IF @TenantBId IS NOT NULL
    BEGIN
        DELETE dbo.CustomerStoreAssignments WHERE TenantId=@TenantBId;
        DELETE dbo.Customers WHERE TenantId=@TenantBId AND CustomerCode=N'SMOKE-CUSTOMER-002';
        DELETE dbo.UserStoreAssignments WHERE TenantId=@TenantBId;
        DELETE dbo.RolePermissions WHERE RoleId IN(SELECT Id FROM dbo.Roles WHERE TenantId=@TenantBId);
        DELETE dbo.UserRoles WHERE UserId IN(SELECT Id FROM dbo.Users WHERE TenantId=@TenantBId);
        DELETE dbo.Users WHERE TenantId=@TenantBId AND NormalizedEmail=N'SMOKE.TENANTBADMIN@CUSTSEARCH.LOCAL';
        DELETE dbo.Roles WHERE TenantId=@TenantBId;
        DELETE dbo.TenantSubscriptions WHERE TenantId=@TenantBId;
        DELETE dbo.Stores WHERE TenantId=@TenantBId AND StoreCode=N'SMOKE-STORE-002';
        DELETE dbo.Tenants WHERE Id=@TenantBId;
    END;
    IF @PlatformUserId IS NOT NULL BEGIN DELETE dbo.UserRoles WHERE UserId=@PlatformUserId;DELETE dbo.Users WHERE Id=@PlatformUserId;END;
    COMMIT TRANSACTION;
    SELECT N'DELETED' Result,N'SMOKE-TENANT-001' TenantCode;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
