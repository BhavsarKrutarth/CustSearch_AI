:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

/*
  Repeat-safe connected UAT data for the existing tenant administrator account.
  This script never creates/changes a user password and never stores an RTSP URL.
  Physical camera credentials stay in CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP on the AI host.
*/
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now datetime2(7)=SYSUTCDATETIME();
    DECLARE @TenantId bigint=(SELECT Id FROM dbo.Tenants WHERE TenantCode=N'TEN-35D77F00D7F0');
    IF @TenantId IS NULL THROW 56200,'Target tenant TEN-35D77F00D7F0 was not found.',1;

    DECLARE @AdminUserId bigint=(
        SELECT Id FROM dbo.Users
        WHERE TenantId=@TenantId AND NormalizedUserName=N'SMOKE.PLATFORM' AND Scope=2 AND IsActive=1);
    IF @AdminUserId IS NULL THROW 56201,'Active tenant user smoke.platform was not found.',1;
    IF NOT EXISTS(
        SELECT 1 FROM dbo.UserRoles ur
        JOIN dbo.Roles r ON r.Id=ur.RoleId
        WHERE ur.UserId=@AdminUserId AND r.TenantId=@TenantId
          AND r.NormalizedName=N'TENANTADMIN' AND r.IsActive=1)
        THROW 56202,'smoke.platform is not an active TenantAdmin.',1;

    EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId=@TenantId;
    DECLARE @TenantAdminRoleId bigint=(
        SELECT Id FROM dbo.Roles WHERE TenantId=@TenantId AND NormalizedName=N'TENANTADMIN' AND IsActive=1);
    INSERT dbo.RolePermissions(RoleId,PermissionId)
    SELECT @TenantAdminRoleId,p.Id
    FROM dbo.Permissions p
    WHERE p.Scope=2 AND p.IsActive=1
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=@TenantAdminRoleId AND rp.PermissionId=p.Id);

    IF NOT EXISTS(SELECT 1 FROM dbo.Stores WHERE TenantId=@TenantId AND StoreCode=N'UAT-STORE-001')
        INSERT dbo.Stores(
            TenantId,StoreCode,StoreName,AddressLine1,City,StateOrProvince,PostalCode,CountryCode,
            Latitude,Longitude,GeoFenceRadiusMeters,LocationSource,IsLocationVerified,
            LocationVerifiedUtc,LocationVerifiedByUserId,TimeZone,ContactEmail,IsActive)
        VALUES(
            @TenantId,N'UAT-STORE-001',N'Krutarth Office Store',N'Krutarth Office',N'Ahmedabad',
            N'Gujarat',N'380001',N'IN',23.0225,72.5714,100,1,1,@Now,@AdminUserId,
            N'Asia/Kolkata',N'office.uat@custsearch.local',1);
    DECLARE @StoreId bigint=(SELECT Id FROM dbo.Stores WHERE TenantId=@TenantId AND StoreCode=N'UAT-STORE-001');

    IF NOT EXISTS(SELECT 1 FROM dbo.UserStoreAssignments WHERE TenantId=@TenantId AND UserId=@AdminUserId AND StoreId=@StoreId)
        INSERT dbo.UserStoreAssignments(TenantId,UserId,StoreId,IsPrimary,AssignedByUserId)
        VALUES(@TenantId,@AdminUserId,@StoreId,1,@AdminUserId);

    IF NOT EXISTS(SELECT 1 FROM dbo.ProductCategories WHERE TenantId=@TenantId AND CategoryCode=N'UAT-CAT-001')
        INSERT dbo.ProductCategories(TenantId,StoreId,CategoryCode,Name,IsActive)
        VALUES(@TenantId,@StoreId,N'UAT-CAT-001',N'UAT Essentials',1);
    DECLARE @CategoryId bigint=(SELECT Id FROM dbo.ProductCategories WHERE TenantId=@TenantId AND CategoryCode=N'UAT-CAT-001');

    IF NOT EXISTS(SELECT 1 FROM dbo.StoreVoiceCommandSettings WHERE TenantId=@TenantId AND StoreId=@StoreId)
        INSERT dbo.StoreVoiceCommandSettings(StoreId,TenantId,TriggerKeyword,ResponseMode,IsEnabled,RequireConfirmationForAmbiguousCategory,CreatedUtc,UpdatedUtc)
        VALUES(@StoreId,@TenantId,N'Aasha Add',4,1,1,@Now,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.StoreVoiceCommandAliases WHERE TenantId=@TenantId AND StoreId=@StoreId AND Alias=N'Add interest')
        INSERT dbo.StoreVoiceCommandAliases(TenantId,StoreId,Alias,CreatedUtc)
        VALUES(@TenantId,@StoreId,N'Add interest',@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.StoreVoiceCommandRuntimeSettings WHERE TenantId=@TenantId AND StoreId=@StoreId)
        INSERT dbo.StoreVoiceCommandRuntimeSettings(StoreId,TenantId,LanguageCode,RequireConfirmation,ListeningTimeoutSeconds,MinimumRecognitionConfidence,CreatedUtc,UpdatedUtc)
        VALUES(@StoreId,@TenantId,N'en-IN',1,30,70,@Now,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.ProductCategoryAliases WHERE TenantId=@TenantId AND StoreId=@StoreId AND NormalizedAliasText=N'UAT ESSENTIALS')
        INSERT dbo.ProductCategoryAliases(TenantId,StoreId,ProductCategoryId,AliasText,NormalizedAliasText,LanguageCode,IsActive,CreatedByUserId,CreatedUtc,UpdatedUtc)
        VALUES(@TenantId,@StoreId,@CategoryId,N'UAT essentials',N'UAT ESSENTIALS',N'en-IN',1,@AdminUserId,@Now,@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.Customers WHERE TenantId=@TenantId AND CustomerCode=N'UAT-CUSTOMER-001')
        INSERT dbo.Customers(TenantId,CustomerCode,FirstName,LastName,Mobile,Email,Notes,IsActive)
        VALUES(@TenantId,N'UAT-CUSTOMER-001',N'Aasha',N'Customer',N'+919999000001',N'uat.customer@custsearch.local',N'Connected tenant-admin UAT customer',1);
    DECLARE @CustomerId bigint=(SELECT Id FROM dbo.Customers WHERE TenantId=@TenantId AND CustomerCode=N'UAT-CUSTOMER-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments WHERE TenantId=@TenantId AND CustomerId=@CustomerId AND StoreId=@StoreId)
        INSERT dbo.CustomerStoreAssignments(TenantId,CustomerId,StoreId,IsPrimary,AssignedByUserId)
        VALUES(@TenantId,@CustomerId,@StoreId,1,@AdminUserId);

    IF NOT EXISTS(SELECT 1 FROM dbo.AnonymousVisitors WHERE TenantId=@TenantId AND StoreId=@StoreId AND VisitorCode=N'UAT-VISITOR-001')
        INSERT dbo.AnonymousVisitors(TenantId,StoreId,VisitorCode,FirstSeenUtc,LastSeenUtc,IsActive,ConvertedCustomerId,ConvertedUtc)
        VALUES(@TenantId,@StoreId,N'UAT-VISITOR-001',DATEADD(minute,-90,@Now),DATEADD(minute,-60,@Now),0,@CustomerId,DATEADD(minute,-60,@Now));
    DECLARE @VisitorId bigint=(SELECT Id FROM dbo.AnonymousVisitors WHERE TenantId=@TenantId AND StoreId=@StoreId AND VisitorCode=N'UAT-VISITOR-001');

    IF NOT EXISTS(SELECT 1 FROM dbo.Households WHERE TenantId=@TenantId AND HouseholdCode=N'UAT-HOUSEHOLD-001')
        INSERT dbo.Households(TenantId,HouseholdCode,Name,Notes,IsActive)
        VALUES(@TenantId,N'UAT-HOUSEHOLD-001',N'Aasha UAT Household',N'Explicitly verified UAT relationship',1);
    DECLARE @HouseholdId bigint=(SELECT Id FROM dbo.Households WHERE TenantId=@TenantId AND HouseholdCode=N'UAT-HOUSEHOLD-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.HouseholdMembers WHERE TenantId=@TenantId AND HouseholdId=@HouseholdId AND CustomerId=@CustomerId)
        INSERT dbo.HouseholdMembers(TenantId,HouseholdId,CustomerId,RelationshipType,RelationshipSource,IsVerified,VerifiedByUserId,VerifiedUtc,IsActive)
        VALUES(@TenantId,@HouseholdId,@CustomerId,N'Primary',1,1,@AdminUserId,@Now,1);

    IF NOT EXISTS(SELECT 1 FROM dbo.VisitParties WHERE TenantId=@TenantId AND StoreId=@StoreId AND PartyCode=N'UAT-PARTY-001')
        INSERT dbo.VisitParties(TenantId,StoreId,PartyCode,StartedUtc,EndedUtc,Source,Status)
        VALUES(@TenantId,@StoreId,N'UAT-PARTY-001',DATEADD(minute,-55,@Now),DATEADD(minute,-20,@Now),1,2);
    DECLARE @PartyId bigint=(SELECT Id FROM dbo.VisitParties WHERE TenantId=@TenantId AND StoreId=@StoreId AND PartyCode=N'UAT-PARTY-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.VisitPartyMembers WHERE TenantId=@TenantId AND VisitPartyId=@PartyId AND CustomerId=@CustomerId)
        INSERT dbo.VisitPartyMembers(TenantId,StoreId,VisitPartyId,IdentityType,CustomerId,AnonymousVisitorId,JoinedUtc)
        VALUES(@TenantId,@StoreId,@PartyId,1,@CustomerId,NULL,DATEADD(minute,-55,@Now));
    IF NOT EXISTS(SELECT 1 FROM dbo.VisitPartyMembers WHERE TenantId=@TenantId AND VisitPartyId=@PartyId AND AnonymousVisitorId=@VisitorId)
        INSERT dbo.VisitPartyMembers(TenantId,StoreId,VisitPartyId,IdentityType,CustomerId,AnonymousVisitorId,JoinedUtc)
        VALUES(@TenantId,@StoreId,@PartyId,2,NULL,@VisitorId,DATEADD(minute,-54,@Now));

    IF NOT EXISTS(SELECT 1 FROM dbo.CustomerVisits WHERE TenantId=@TenantId AND VisitCode=N'UAT-VISIT-001')
        INSERT dbo.CustomerVisits(TenantId,StoreId,CustomerId,VisitPartyId,VisitCode,EnteredUtc,ExitedUtc,Source,Status)
        VALUES(@TenantId,@StoreId,@CustomerId,@PartyId,N'UAT-VISIT-001',DATEADD(minute,-55,@Now),DATEADD(minute,-20,@Now),1,2);
    DECLARE @VisitId bigint=(SELECT Id FROM dbo.CustomerVisits WHERE TenantId=@TenantId AND VisitCode=N'UAT-VISIT-001');

    IF NOT EXISTS(SELECT 1 FROM dbo.Products WHERE TenantId=@TenantId AND ProductCode=N'UAT-PRODUCT-001')
        INSERT dbo.Products(TenantId,ProductCode,Barcode,Name,CategoryId,Brand,UnitName,SalePrice,CostPrice,TaxPercent,IsActive)
        VALUES(@TenantId,N'UAT-PRODUCT-001',N'UAT-BARCODE-001',N'UAT Demo Product',@CategoryId,N'CustSearch',N'each',125,75,0,1);
    DECLARE @ProductId bigint=(SELECT Id FROM dbo.Products WHERE TenantId=@TenantId AND ProductCode=N'UAT-PRODUCT-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities WHERE TenantId=@TenantId AND ProductId=@ProductId AND StoreId=@StoreId)
        INSERT dbo.ProductStoreAvailabilities(TenantId,ProductId,StoreId,IsActive)
        VALUES(@TenantId,@ProductId,@StoreId,1);

    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'UAT-INVOICE-001')
        INSERT dbo.RetailInvoices(TenantId,StoreId,InvoiceNumber,CustomerId,HouseholdId,CustomerVisitId,VisitPartyId,InvoiceUtc,Subtotal,DiscountAmount,TaxAmount,GrandTotal,PaidAmount,BalanceAmount,Status,Notes,CreatedByUserId)
        VALUES(@TenantId,@StoreId,N'UAT-INVOICE-001',@CustomerId,@HouseholdId,@VisitId,@PartyId,@Now,125,0,0,125,125,0,4,N'Connected tenant-admin UAT invoice',@AdminUserId);
    DECLARE @InvoiceId bigint=(SELECT Id FROM dbo.RetailInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'UAT-INVOICE-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoiceItems WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId AND ProductCodeSnapshot=N'UAT-PRODUCT-001')
        INSERT dbo.RetailInvoiceItems(TenantId,InvoiceId,ProductId,CategoryId,ProductCodeSnapshot,ProductNameSnapshot,CategoryNameSnapshot,Quantity,UnitPrice,DiscountAmount,TaxPercent,TaxAmount,LineSubtotal,LineTotal)
        VALUES(@TenantId,@InvoiceId,@ProductId,@CategoryId,N'UAT-PRODUCT-001',N'UAT Demo Product',N'UAT Essentials',1,125,0,0,0,125,125);
    DECLARE @InvoiceItemId bigint=(SELECT Id FROM dbo.RetailInvoiceItems WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId AND ProductCodeSnapshot=N'UAT-PRODUCT-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoicePayments WHERE TenantId=@TenantId AND PaymentReference=N'UAT-PAYMENT-001')
        INSERT dbo.RetailInvoicePayments(TenantId,StoreId,InvoiceId,PaymentReference,PaymentMethod,Amount,PaymentUtc,Status,ReceivedByUserId)
        VALUES(@TenantId,@StoreId,@InvoiceId,N'UAT-PAYMENT-001',2,125,@Now,2,@AdminUserId);
    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoiceParticipants WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId AND CustomerId=@CustomerId)
        INSERT dbo.RetailInvoiceParticipants(TenantId,InvoiceId,CustomerId,ParticipationType,IsPayer)
        VALUES(@TenantId,@InvoiceId,@CustomerId,1,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoiceItemAttributions WHERE TenantId=@TenantId AND InvoiceItemId=@InvoiceItemId AND CustomerId=@CustomerId)
        INSERT dbo.RetailInvoiceItemAttributions(TenantId,InvoiceId,InvoiceItemId,CustomerId,AttributionType,QuantityAttributed,AmountAttributed,Source,CreatedByUserId)
        VALUES(@TenantId,@InvoiceId,@InvoiceItemId,@CustomerId,1,1,125,1,@AdminUserId);

    IF NOT EXISTS(SELECT 1 FROM dbo.CustomerPreferenceSignals WHERE TenantId=@TenantId AND CustomerId=@CustomerId AND Reason=N'UAT-SEED-PREFERENCE-001')
        INSERT dbo.CustomerPreferenceSignals(TenantId,StoreId,CustomerId,PreferenceType,ReferenceId,Value,SignalScore,Source,Confidence,FirstObservedUtc,LastObservedUtc,IsActive,CreatedByUserId,Reason,CreatedUtc,UpdatedUtc)
        VALUES(@TenantId,@StoreId,@CustomerId,1,@CategoryId,N'UAT Essentials',100,4,95,@Now,@Now,1,@AdminUserId,N'UAT-SEED-PREFERENCE-001',@Now,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.HouseholdPreferenceTags WHERE TenantId=@TenantId AND HouseholdId=@HouseholdId AND Reason=N'UAT-SEED-HOUSEHOLD-PREFERENCE-001')
        INSERT dbo.HouseholdPreferenceTags(TenantId,HouseholdId,PreferenceType,ReferenceId,Value,Source,CreatedByUserId,Reason,IsActive,CreatedUtc,UpdatedUtc)
        VALUES(@TenantId,@HouseholdId,1,@CategoryId,N'UAT Essentials',1,@AdminUserId,N'UAT-SEED-HOUSEHOLD-PREFERENCE-001',1,@Now,@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.VoiceCommandSessions WHERE TenantId=@TenantId AND RecognizedText=N'UAT-VOICE-001')
        INSERT dbo.VoiceCommandSessions(TenantId,StoreId,StaffUserId,CustomerId,MatchedTrigger,RecognizedText,RecognitionConfidence,ProposedPreferenceType,ProposedReferenceId,ProposedValue,ConfirmationRequired,Status,ExpiresUtc,ResolvedUtc,CreatedUtc,UpdatedUtc)
        VALUES(@TenantId,@StoreId,@AdminUserId,@CustomerId,N'Aasha Add',N'UAT-VOICE-001',95,1,@CategoryId,N'UAT Essentials',1,3,DATEADD(minute,5,@Now),@Now,DATEADD(minute,-5,@Now),@Now);
    DECLARE @VoiceSessionId bigint=(SELECT Id FROM dbo.VoiceCommandSessions WHERE TenantId=@TenantId AND RecognizedText=N'UAT-VOICE-001');

    IF NOT EXISTS(SELECT 1 FROM dbo.Alerts WHERE TenantId=@TenantId AND DeduplicationKey=N'UAT-ALERT-001')
        INSERT dbo.Alerts(AlertType,TenantId,StoreId,Severity,Title,Message,EntityType,EntityId,CreatedUtc,Status,CorrelationId,DeduplicationKey)
        VALUES(N'UatOperational',@TenantId,@StoreId,2,N'UAT camera connectivity check',N'Validate the authorized office camera preview.',N'Camera',NULL,@Now,1,N'UAT-TEN35D77F',N'UAT-ALERT-001');

    IF NOT EXISTS(SELECT 1 FROM dbo.IntegrationConfigurations WHERE TenantId=@TenantId AND Provider=N'UAT-WEBHOOK')
        INSERT dbo.IntegrationConfigurations(TenantId,Provider,IntegrationType,Enabled,EndpointBaseUrl,CredentialReference,WebhookSigningSecretReference,TimeoutSeconds,RetryMaxAttempts,RetryBaseDelaySeconds,CreatedUtc,UpdatedUtc)
        VALUES(@TenantId,N'UAT-WEBHOOK',2,0,N'https://example.invalid/custsearch-uat',N'env:UAT_WEBHOOK_CREDENTIAL',N'env:UAT_WEBHOOK_SIGNING',10,3,5,@Now,@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.Cameras WHERE TenantId=@TenantId AND CameraCode=N'OFFICE-ENTRY-01')
        INSERT dbo.Cameras(TenantId,StoreId,CameraCode,Name,RtspConfigurationReference,Status,Location,Direction,IsActive,CreatedUtc,UpdatedUtc)
        VALUES(@TenantId,@StoreId,N'OFFICE-ENTRY-01',N'Office Entry Camera',N'env:CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP',1,N'Office entry; RTSP secret is configured only on the AI host',1,1,@Now,@Now);
    DECLARE @CameraId bigint=(SELECT Id FROM dbo.Cameras WHERE TenantId=@TenantId AND CameraCode=N'OFFICE-ENTRY-01');
    IF NOT EXISTS(SELECT 1 FROM dbo.CameraZoneConfigurations WHERE TenantId=@TenantId AND CameraId=@CameraId AND ZoneCode=N'OFFICE-ENTRY')
        INSERT dbo.CameraZoneConfigurations(TenantId,StoreId,CameraId,ZoneCode,Name,ZoneType,GeometryJson,Version,EffectiveUtc,IsActive,CreatedUtc)
        VALUES(@TenantId,@StoreId,@CameraId,N'OFFICE-ENTRY',N'Office entrance',1,N'{"points":[[0.05,0.05],[0.95,0.05],[0.95,0.95],[0.05,0.95]]}',1,@Now,1,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.PersonTrackSessions WHERE TenantId=@TenantId AND PersonTrackId=N'UAT-TRACK-001')
        INSERT dbo.PersonTrackSessions(TenantId,StoreId,CameraId,PersonTrackId,StartUtc,EndUtc,Confidence,SubjectKind,TrackingState,UpdatedUtc)
        VALUES(@TenantId,@StoreId,@CameraId,N'UAT-TRACK-001',DATEADD(minute,-20,@Now),DATEADD(minute,-10,@Now),0.90,1,3,@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.CameraUserPreviewGrants WHERE TenantId=@TenantId AND CameraId=@CameraId AND UserId=@AdminUserId)
        INSERT dbo.CameraUserPreviewGrants(TenantId,StoreId,CameraId,UserId,CanViewLive,CanViewTracking,CanControl,ValidUntilUtc,IsActive,AssignedByUserId,CreatedUtc,UpdatedUtc)
        VALUES(@TenantId,@StoreId,@CameraId,@AdminUserId,1,1,0,NULL,1,@AdminUserId,@Now,@Now);
    ELSE
        UPDATE dbo.CameraUserPreviewGrants
        SET StoreId=@StoreId,CanViewLive=1,CanViewTracking=1,CanControl=0,ValidUntilUtc=NULL,
            IsActive=1,AssignedByUserId=@AdminUserId,UpdatedUtc=@Now
        WHERE TenantId=@TenantId AND CameraId=@CameraId AND UserId=@AdminUserId;

    IF NOT EXISTS(SELECT 1 FROM dbo.CustomerRecognitionConsents WHERE TenantId=@TenantId AND CustomerId=@CustomerId AND ConsentVersion=N'UAT-CONSENT-V1')
        INSERT dbo.CustomerRecognitionConsents(TenantId,CustomerId,ConsentType,Purpose,GrantedUtc,ConsentVersion,CapturedByUserId,EvidenceReference,CreatedUtc)
        VALUES(@TenantId,@CustomerId,1,N'Office welcome UAT; no biometric template stored',@Now,N'UAT-CONSENT-V1',@AdminUserId,N'uat://explicit-consent-record',@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.ExportJobs WHERE TenantId=@TenantId AND RequestedByUserId=@AdminUserId AND FilterJson=N'{"seed":"UAT-TEN35D77F"}')
        INSERT dbo.ExportJobs(RequestedByUserId,TenantId,ReportType,Format,FilterJson,AuthorizedStoreIdsJson,Status,Progress,CreatedUtc,ExpiresUtc,AttemptCount)
        VALUES(@AdminUserId,@TenantId,1,1,N'{"seed":"UAT-TEN35D77F"}',N'['+CONVERT(nvarchar(20),@StoreId)+N']',1,0,@Now,DATEADD(day,7,@Now),0);

    IF NOT EXISTS(SELECT 1 FROM dbo.AuditLogs WHERE TenantId=@TenantId AND CorrelationId='UAT-SEED-TEN35D77F')
        INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc)
        VALUES(@TenantId,@StoreId,@AdminUserId,N'User',N'VoiceCommandConfirmed',N'VoiceCommandSession',CONVERT(nvarchar(100),@VoiceSessionId),N'{"source":"repeat-safe UAT seed"}','192.168.1.30',N'sqlcmd UAT setup','UAT-SEED-TEN35D77F',@Now);

    COMMIT TRANSACTION;
    SELECT N'PASS' Result,@TenantId TenantId,@AdminUserId AdminUserId,@StoreId StoreId,
           @CustomerId CustomerId,@InvoiceId RetailInvoiceId,@CameraId CameraId,@VoiceSessionId VoiceSessionId;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
