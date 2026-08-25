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
  Deterministic connected UAT data for implemented Phases 1-16.
  Tenant/store ownership is explicit and every insert is guarded by a stable smoke key.
  The password hash is supplied by run-smoke-data.ps1; no password or reusable hash is committed.
  Phase 18 data is intentionally excluded until its source/schema drift is reconciled.
*/
IF N'$(SmokePasswordHash)' = N''
    THROW 56000, 'SmokePasswordHash sqlcmd variable is required.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Now datetime2(7)=SYSUTCDATETIME();
    DECLARE @PasswordHash nvarchar(500)=N'$(SmokePasswordHash)';
    DECLARE @PlanId bigint=(SELECT TOP(1) Id FROM dbo.SubscriptionPlans WHERE IsActive=1 ORDER BY DisplayOrder,Id);
    IF @PlanId IS NULL THROW 56001,'An active subscription plan is required.',1;

    IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE NormalizedEmail=N'SMOKE.PLATFORM@CUSTSEARCH.LOCAL')
        INSERT dbo.Users(TenantId,Scope,UserName,NormalizedUserName,Email,NormalizedEmail,DisplayName,PasswordHash,SecurityStamp,IsActive,CreatedUtc)
        VALUES(NULL,1,N'smoke.platform',N'SMOKE.PLATFORM',N'smoke.platform@custsearch.local',N'SMOKE.PLATFORM@CUSTSEARCH.LOCAL',N'Smoke Platform Admin',@PasswordHash,REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),1,@Now);
    DECLARE @PlatformUserId bigint=(SELECT Id FROM dbo.Users WHERE NormalizedEmail=N'SMOKE.PLATFORM@CUSTSEARCH.LOCAL');
    DECLARE @PlatformRoleId bigint=(SELECT TOP(1) Id FROM dbo.Roles WHERE Scope=1 AND NormalizedName=N'PLATFORMSUPERADMIN' AND IsActive=1);
    IF @PlatformRoleId IS NULL THROW 56002,'PlatformSuperAdmin role is required.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.UserRoles WHERE UserId=@PlatformUserId AND RoleId=@PlatformRoleId)
        INSERT dbo.UserRoles(UserId,RoleId,AssignedByUserId) VALUES(@PlatformUserId,@PlatformRoleId,@PlatformUserId);

    IF NOT EXISTS(SELECT 1 FROM dbo.Tenants WHERE TenantCode=N'SMOKE-TENANT-001')
        INSERT dbo.Tenants(TenantCode,LegalName,DisplayName,TimeZone,PrimaryContactName,PrimaryEmail,PrimaryMobile,CountryCode,CurrencyCode,SubscriptionPlanId,SubscriptionStatus,SubscriptionStartsUtc,MaxStores,MaxUsers,MaxCameras,MaxStaff)
        VALUES(N'SMOKE-TENANT-001',N'CustSearch Smoke Retail Private Limited',N'CustSearch Smoke Retail',N'Asia/Kolkata',N'Smoke Owner',N'smoke.owner@custsearch.local',N'+910000000001',N'IN',N'INR',@PlanId,2,@Now,5,20,10,20);
    DECLARE @TenantId bigint=(SELECT Id FROM dbo.Tenants WHERE TenantCode=N'SMOKE-TENANT-001');

    IF NOT EXISTS(SELECT 1 FROM dbo.TenantSubscriptions WHERE TenantId=@TenantId AND Status IN(1,2,3))
        INSERT dbo.TenantSubscriptions(TenantId,SubscriptionPlanId,BillingCycle,Status,StartsUtc,AutoRenew)
        VALUES(@TenantId,@PlanId,1,2,@Now,1);
    DECLARE @SubscriptionId bigint=(SELECT TOP(1) Id FROM dbo.TenantSubscriptions WHERE TenantId=@TenantId ORDER BY Id DESC);

    IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE TenantId=@TenantId AND NormalizedName=N'TENANTADMIN')
        INSERT dbo.Roles(TenantId,Scope,Name,NormalizedName,Description,IsSystem,IsActive,CreatedUtc)
        VALUES(@TenantId,2,N'TenantAdmin',N'TENANTADMIN',N'Smoke tenant administrator',1,1,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE TenantId=@TenantId AND NormalizedName=N'STAFF')
        INSERT dbo.Roles(TenantId,Scope,Name,NormalizedName,Description,IsSystem,IsActive,CreatedUtc)
        VALUES(@TenantId,2,N'Staff',N'STAFF',N'Smoke store staff',1,1,@Now);
    DECLARE @TenantAdminRoleId bigint=(SELECT Id FROM dbo.Roles WHERE TenantId=@TenantId AND NormalizedName=N'TENANTADMIN');
    DECLARE @StaffRoleId bigint=(SELECT Id FROM dbo.Roles WHERE TenantId=@TenantId AND NormalizedName=N'STAFF');
    INSERT dbo.RolePermissions(RoleId,PermissionId)
    SELECT @TenantAdminRoleId,p.Id FROM dbo.Permissions p WHERE p.Scope=2
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=@TenantAdminRoleId AND rp.PermissionId=p.Id);
    INSERT dbo.RolePermissions(RoleId,PermissionId)
    SELECT @StaffRoleId,p.Id FROM dbo.Permissions p WHERE p.Scope=2 AND p.Name IN
      (N'TenantDashboard.View',N'Customers.View',N'Customers.Create',N'Customers.Edit',N'Visitors.View',N'Visitors.Convert',N'Visits.View',N'Visits.Edit',N'Products.View',N'RetailInvoices.View',N'RetailInvoices.Create',N'RetailPayments.View',N'RetailPayments.Create',N'Preferences.View',N'Preferences.Manage',N'VoiceCommands.Use',N'Alerts.View')
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=@StaffRoleId AND rp.PermissionId=p.Id);

    IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE TenantId=@TenantId AND NormalizedEmail=N'SMOKE.TENANTADMIN@CUSTSEARCH.LOCAL')
        INSERT dbo.Users(TenantId,Scope,UserName,NormalizedUserName,Email,NormalizedEmail,DisplayName,PasswordHash,SecurityStamp,IsActive,CreatedUtc)
        VALUES(@TenantId,2,N'smoke.tenantadmin',N'SMOKE.TENANTADMIN',N'smoke.tenantadmin@custsearch.local',N'SMOKE.TENANTADMIN@CUSTSEARCH.LOCAL',N'Smoke Tenant Admin',@PasswordHash,REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),1,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE TenantId=@TenantId AND NormalizedEmail=N'SMOKE.STAFF@CUSTSEARCH.LOCAL')
        INSERT dbo.Users(TenantId,Scope,UserName,NormalizedUserName,Email,NormalizedEmail,DisplayName,PasswordHash,SecurityStamp,IsActive,CreatedUtc)
        VALUES(@TenantId,2,N'smoke.staff',N'SMOKE.STAFF',N'smoke.staff@custsearch.local',N'SMOKE.STAFF@CUSTSEARCH.LOCAL',N'Smoke Staff',@PasswordHash,REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),1,@Now);
    DECLARE @TenantAdminId bigint=(SELECT Id FROM dbo.Users WHERE TenantId=@TenantId AND NormalizedEmail=N'SMOKE.TENANTADMIN@CUSTSEARCH.LOCAL');
    DECLARE @StaffUserId bigint=(SELECT Id FROM dbo.Users WHERE TenantId=@TenantId AND NormalizedEmail=N'SMOKE.STAFF@CUSTSEARCH.LOCAL');
    -- A deliberate rerun rotates only deterministic smoke credentials, allowing a developer to
    -- choose a new local password without storing it in source or touching non-smoke accounts.
    UPDATE dbo.Users SET PasswordHash=@PasswordHash,SecurityStamp=REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')
      WHERE Id IN(@PlatformUserId,@TenantAdminId,@StaffUserId);
    IF NOT EXISTS(SELECT 1 FROM dbo.UserRoles WHERE UserId=@TenantAdminId AND RoleId=@TenantAdminRoleId) INSERT dbo.UserRoles(UserId,RoleId,AssignedByUserId)VALUES(@TenantAdminId,@TenantAdminRoleId,@PlatformUserId);
    IF NOT EXISTS(SELECT 1 FROM dbo.UserRoles WHERE UserId=@StaffUserId AND RoleId=@StaffRoleId) INSERT dbo.UserRoles(UserId,RoleId,AssignedByUserId)VALUES(@StaffUserId,@StaffRoleId,@TenantAdminId);

    IF NOT EXISTS(SELECT 1 FROM dbo.Stores WHERE TenantId=@TenantId AND StoreCode=N'SMOKE-STORE-001')
        INSERT dbo.Stores(TenantId,StoreCode,StoreName,AddressLine1,City,StateOrProvince,PostalCode,CountryCode,Latitude,Longitude,GeoFenceRadiusMeters,LocationSource,IsLocationVerified,LocationVerifiedUtc,LocationVerifiedByUserId,TimeZone,ContactEmail,IsActive)
        VALUES(@TenantId,N'SMOKE-STORE-001',N'Smoke Main Store',N'1 Smoke Test Road',N'Ahmedabad',N'Gujarat',N'380001',N'IN',23.0225,72.5714,100,1,1,@Now,@TenantAdminId,N'Asia/Kolkata',N'smoke.store@custsearch.local',1);
    DECLARE @StoreId bigint=(SELECT Id FROM dbo.Stores WHERE TenantId=@TenantId AND StoreCode=N'SMOKE-STORE-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.UserStoreAssignments WHERE TenantId=@TenantId AND UserId=@TenantAdminId AND StoreId=@StoreId) INSERT dbo.UserStoreAssignments(TenantId,UserId,StoreId,IsPrimary,AssignedByUserId)VALUES(@TenantId,@TenantAdminId,@StoreId,1,@TenantAdminId);
    IF NOT EXISTS(SELECT 1 FROM dbo.UserStoreAssignments WHERE TenantId=@TenantId AND UserId=@StaffUserId AND StoreId=@StoreId) INSERT dbo.UserStoreAssignments(TenantId,UserId,StoreId,IsPrimary,AssignedByUserId)VALUES(@TenantId,@StaffUserId,@StoreId,1,@TenantAdminId);
    IF NOT EXISTS(SELECT 1 FROM dbo.StaffProfiles WHERE TenantId=@TenantId AND EmployeeCode=N'SMOKE-STAFF-001') INSERT dbo.StaffProfiles(TenantId,UserId,EmployeeCode,FirstName,LastName,Mobile,IsActive)VALUES(@TenantId,@StaffUserId,N'SMOKE-STAFF-001',N'Smoke',N'Staff',N'+910000000002',1);

    IF NOT EXISTS(SELECT 1 FROM dbo.ProductCategories WHERE TenantId=@TenantId AND CategoryCode=N'SMOKE-CAT-001') INSERT dbo.ProductCategories(TenantId,StoreId,CategoryCode,Name,IsActive)VALUES(@TenantId,@StoreId,N'SMOKE-CAT-001',N'Smoke Essentials',1);
    DECLARE @CategoryId bigint=(SELECT Id FROM dbo.ProductCategories WHERE TenantId=@TenantId AND CategoryCode=N'SMOKE-CAT-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.StoreVoiceCommandSettings WHERE TenantId=@TenantId AND StoreId=@StoreId) INSERT dbo.StoreVoiceCommandSettings(StoreId,TenantId,TriggerKeyword,ResponseMode,IsEnabled,RequireConfirmationForAmbiguousCategory,CreatedUtc,UpdatedUtc)VALUES(@StoreId,@TenantId,N'Aasha Add',1,1,1,@Now,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.StoreVoiceCommandAliases WHERE TenantId=@TenantId AND StoreId=@StoreId AND Alias=N'Add interest') INSERT dbo.StoreVoiceCommandAliases(TenantId,StoreId,Alias,CreatedUtc)VALUES(@TenantId,@StoreId,N'Add interest',@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.StoreVoiceCommandRuntimeSettings WHERE TenantId=@TenantId AND StoreId=@StoreId) INSERT dbo.StoreVoiceCommandRuntimeSettings(StoreId,TenantId,LanguageCode,RequireConfirmation,ListeningTimeoutSeconds,MinimumRecognitionConfidence,CreatedUtc,UpdatedUtc)VALUES(@StoreId,@TenantId,N'en-IN',1,20,80,@Now,@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.Customers WHERE TenantId=@TenantId AND CustomerCode=N'SMOKE-CUSTOMER-001') INSERT dbo.Customers(TenantId,CustomerCode,FirstName,LastName,Mobile,Email,Notes,IsActive)VALUES(@TenantId,N'SMOKE-CUSTOMER-001',N'Smoke',N'Customer',N'+910000000003',N'smoke.customer@custsearch.local',N'Deterministic UAT customer',1);
    DECLARE @CustomerId bigint=(SELECT Id FROM dbo.Customers WHERE TenantId=@TenantId AND CustomerCode=N'SMOKE-CUSTOMER-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments WHERE TenantId=@TenantId AND CustomerId=@CustomerId AND StoreId=@StoreId) INSERT dbo.CustomerStoreAssignments(TenantId,CustomerId,StoreId,IsPrimary,AssignedByUserId)VALUES(@TenantId,@CustomerId,@StoreId,1,@TenantAdminId);
    IF NOT EXISTS(SELECT 1 FROM dbo.AnonymousVisitors WHERE TenantId=@TenantId AND StoreId=@StoreId AND VisitorCode=N'SMOKE-VISITOR-001') INSERT dbo.AnonymousVisitors(TenantId,StoreId,VisitorCode,FirstSeenUtc,LastSeenUtc,IsActive,ConvertedCustomerId,ConvertedUtc)VALUES(@TenantId,@StoreId,N'SMOKE-VISITOR-001',DATEADD(minute,-60,@Now),DATEADD(minute,-5,@Now),0,@CustomerId,@Now);
    DECLARE @VisitorId bigint=(SELECT Id FROM dbo.AnonymousVisitors WHERE TenantId=@TenantId AND StoreId=@StoreId AND VisitorCode=N'SMOKE-VISITOR-001');

    IF NOT EXISTS(SELECT 1 FROM dbo.Households WHERE TenantId=@TenantId AND HouseholdCode=N'SMOKE-HOUSEHOLD-001') INSERT dbo.Households(TenantId,HouseholdCode,Name,Notes,IsActive)VALUES(@TenantId,N'SMOKE-HOUSEHOLD-001',N'Smoke Verified Household',N'Explicitly verified UAT relationship',1);
    DECLARE @HouseholdId bigint=(SELECT Id FROM dbo.Households WHERE TenantId=@TenantId AND HouseholdCode=N'SMOKE-HOUSEHOLD-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.HouseholdMembers WHERE TenantId=@TenantId AND HouseholdId=@HouseholdId AND CustomerId=@CustomerId) INSERT dbo.HouseholdMembers(TenantId,HouseholdId,CustomerId,RelationshipType,RelationshipSource,IsVerified,VerifiedByUserId,VerifiedUtc,IsActive)VALUES(@TenantId,@HouseholdId,@CustomerId,N'Primary',1,1,@TenantAdminId,@Now,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.VisitParties WHERE TenantId=@TenantId AND StoreId=@StoreId AND PartyCode=N'SMOKE-PARTY-001') INSERT dbo.VisitParties(TenantId,StoreId,PartyCode,StartedUtc,EndedUtc,Source,Status)VALUES(@TenantId,@StoreId,N'SMOKE-PARTY-001',DATEADD(minute,-45,@Now),DATEADD(minute,-10,@Now),1,2);
    DECLARE @PartyId bigint=(SELECT Id FROM dbo.VisitParties WHERE TenantId=@TenantId AND StoreId=@StoreId AND PartyCode=N'SMOKE-PARTY-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.VisitPartyMembers WHERE TenantId=@TenantId AND VisitPartyId=@PartyId AND CustomerId=@CustomerId) INSERT dbo.VisitPartyMembers(TenantId,StoreId,VisitPartyId,IdentityType,CustomerId,AnonymousVisitorId,JoinedUtc)VALUES(@TenantId,@StoreId,@PartyId,1,@CustomerId,NULL,DATEADD(minute,-45,@Now));
    IF NOT EXISTS(SELECT 1 FROM dbo.VisitPartyMembers WHERE TenantId=@TenantId AND VisitPartyId=@PartyId AND AnonymousVisitorId=@VisitorId) INSERT dbo.VisitPartyMembers(TenantId,StoreId,VisitPartyId,IdentityType,CustomerId,AnonymousVisitorId,JoinedUtc)VALUES(@TenantId,@StoreId,@PartyId,2,NULL,@VisitorId,DATEADD(minute,-44,@Now));
    IF NOT EXISTS(SELECT 1 FROM dbo.CustomerVisits WHERE TenantId=@TenantId AND VisitCode=N'SMOKE-VISIT-001') INSERT dbo.CustomerVisits(TenantId,StoreId,CustomerId,VisitPartyId,VisitCode,EnteredUtc,ExitedUtc,Source,Status)VALUES(@TenantId,@StoreId,@CustomerId,@PartyId,N'SMOKE-VISIT-001',DATEADD(minute,-45,@Now),DATEADD(minute,-10,@Now),1,2);
    DECLARE @VisitId bigint=(SELECT Id FROM dbo.CustomerVisits WHERE TenantId=@TenantId AND VisitCode=N'SMOKE-VISIT-001');

    IF NOT EXISTS(SELECT 1 FROM dbo.Products WHERE TenantId=@TenantId AND ProductCode=N'SMOKE-PRODUCT-001') INSERT dbo.Products(TenantId,ProductCode,Barcode,Name,CategoryId,Brand,UnitName,SalePrice,CostPrice,TaxPercent,IsActive)VALUES(@TenantId,N'SMOKE-PRODUCT-001',N'SMOKE-BARCODE-001',N'Smoke Product',@CategoryId,N'CustSearch',N'each',100,60,0,1);
    DECLARE @ProductId bigint=(SELECT Id FROM dbo.Products WHERE TenantId=@TenantId AND ProductCode=N'SMOKE-PRODUCT-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities WHERE TenantId=@TenantId AND ProductId=@ProductId AND StoreId=@StoreId) INSERT dbo.ProductStoreAvailabilities(TenantId,ProductId,StoreId,IsActive)VALUES(@TenantId,@ProductId,@StoreId,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'SMOKE-INVOICE-001') INSERT dbo.RetailInvoices(TenantId,StoreId,InvoiceNumber,CustomerId,HouseholdId,CustomerVisitId,VisitPartyId,InvoiceUtc,Subtotal,DiscountAmount,TaxAmount,GrandTotal,PaidAmount,BalanceAmount,Status,Notes,CreatedByUserId)VALUES(@TenantId,@StoreId,N'SMOKE-INVOICE-001',@CustomerId,@HouseholdId,@VisitId,@PartyId,@Now,100,0,0,100,100,0,4,N'Connected smoke invoice',@StaffUserId);
    DECLARE @InvoiceId bigint=(SELECT Id FROM dbo.RetailInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'SMOKE-INVOICE-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoiceItems WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId AND ProductCodeSnapshot=N'SMOKE-PRODUCT-001') INSERT dbo.RetailInvoiceItems(TenantId,InvoiceId,ProductId,CategoryId,ProductCodeSnapshot,ProductNameSnapshot,CategoryNameSnapshot,Quantity,UnitPrice,DiscountAmount,TaxPercent,TaxAmount,LineSubtotal,LineTotal)VALUES(@TenantId,@InvoiceId,@ProductId,@CategoryId,N'SMOKE-PRODUCT-001',N'Smoke Product',N'Smoke Essentials',1,100,0,0,0,100,100);
    DECLARE @InvoiceItemId bigint=(SELECT Id FROM dbo.RetailInvoiceItems WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId AND ProductCodeSnapshot=N'SMOKE-PRODUCT-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoicePayments WHERE TenantId=@TenantId AND PaymentReference=N'SMOKE-PAYMENT-001') INSERT dbo.RetailInvoicePayments(TenantId,StoreId,InvoiceId,PaymentReference,PaymentMethod,Amount,PaymentUtc,Status,ReceivedByUserId)VALUES(@TenantId,@StoreId,@InvoiceId,N'SMOKE-PAYMENT-001',2,100,@Now,2,@StaffUserId);
    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoiceParticipants WHERE TenantId=@TenantId AND InvoiceId=@InvoiceId AND CustomerId=@CustomerId) INSERT dbo.RetailInvoiceParticipants(TenantId,InvoiceId,CustomerId,ParticipationType,IsPayer)VALUES(@TenantId,@InvoiceId,@CustomerId,1,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.RetailInvoiceItemAttributions WHERE TenantId=@TenantId AND InvoiceItemId=@InvoiceItemId AND CustomerId=@CustomerId) INSERT dbo.RetailInvoiceItemAttributions(TenantId,InvoiceId,InvoiceItemId,CustomerId,AttributionType,QuantityAttributed,AmountAttributed,Source,CreatedByUserId)VALUES(@TenantId,@InvoiceId,@InvoiceItemId,@CustomerId,1,1,100,1,@StaffUserId);

    IF NOT EXISTS(SELECT 1 FROM dbo.PlatformInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'SMOKE-PLATFORM-INVOICE-001') INSERT dbo.PlatformInvoices(TenantId,TenantSubscriptionId,InvoiceNumber,Currency,InvoiceUtc,DueUtc,Status,Subtotal,DiscountAmount,TaxAmount,Total,PaidAmount,CreatedUtc,UpdatedUtc,RowVersion)VALUES(@TenantId,@SubscriptionId,N'SMOKE-PLATFORM-INVOICE-001',N'INR',@Now,DATEADD(day,7,@Now),3,1000,0,0,1000,1000,@Now,@Now,CONVERT(binary(16),NEWID()));
    DECLARE @PlatformInvoiceId bigint=(SELECT Id FROM dbo.PlatformInvoices WHERE TenantId=@TenantId AND InvoiceNumber=N'SMOKE-PLATFORM-INVOICE-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.PlatformInvoiceItems WHERE PlatformInvoiceId=@PlatformInvoiceId) INSERT dbo.PlatformInvoiceItems(TenantId,PlatformInvoiceId,PlanName,Quantity,Rate,DiscountAmount,TaxAmount,Subtotal,Total,CreatedUtc)VALUES(@TenantId,@PlatformInvoiceId,N'Smoke Plan Snapshot',1,1000,0,0,1000,1000,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.PlatformPayments WHERE TenantId=@TenantId AND TransactionReference=N'SMOKE-PLATFORM-PAYMENT-001') INSERT dbo.PlatformPayments(TenantId,PlatformInvoiceId,PaymentMethod,Amount,Currency,TransactionReference,PaymentUtc,Status,CreatedUtc,UpdatedUtc)VALUES(@TenantId,@PlatformInvoiceId,N'UPI',1000,N'INR',N'SMOKE-PLATFORM-PAYMENT-001',@Now,2,@Now,@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.CustomerPreferenceSignals WHERE TenantId=@TenantId AND CustomerId=@CustomerId AND ReferenceId=CONVERT(nvarchar(100),@CategoryId)) INSERT dbo.CustomerPreferenceSignals(TenantId,StoreId,CustomerId,PreferenceType,ReferenceId,Value,Source,Confidence,SignalScore,FirstObservedUtc,LastObservedUtc,CreatedUtc,UpdatedUtc)VALUES(@TenantId,@StoreId,@CustomerId,1,CONVERT(nvarchar(100),@CategoryId),N'Smoke Essentials',2,100,100,@Now,@Now,@Now,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.HouseholdPreferenceTags WHERE TenantId=@TenantId AND HouseholdId=@HouseholdId AND Value=N'Smoke Essentials') INSERT dbo.HouseholdPreferenceTags(TenantId,HouseholdId,PreferenceType,Value,Source,CreatedByUserId,CreatedUtc,UpdatedUtc)VALUES(@TenantId,@HouseholdId,1,N'Smoke Essentials',1,@TenantAdminId,@Now,@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.Alerts WHERE TenantId=@TenantId AND DeduplicationKey=N'SMOKE-ALERT-001') INSERT dbo.Alerts(AlertType,TenantId,StoreId,Severity,Title,Message,EntityType,EntityId,CreatedUtc,Status,CorrelationId,DeduplicationKey)VALUES(N'SmokeOperational',@TenantId,@StoreId,1,N'Smoke alert',N'Deterministic UAT notification',N'Store',CONVERT(nvarchar(100),@StoreId),@Now,1,N'SMOKE-RUN-CUSTSEARCH-001',N'SMOKE-ALERT-001');
    DECLARE @AlertId bigint=(SELECT Id FROM dbo.Alerts WHERE TenantId=@TenantId AND DeduplicationKey=N'SMOKE-ALERT-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.RealtimeEvents WHERE TenantId=@TenantId AND DeduplicationKey=N'SMOKE-REALTIME-001') INSERT dbo.RealtimeEvents(TenantId,StoreId,AlertId,EventName,ContractVersion,PayloadJson,OccurredUtc,CorrelationId,DeduplicationKey)VALUES(@TenantId,@StoreId,@AlertId,N'alert.created',1,N'{"source":"smoke"}',@Now,N'SMOKE-RUN-CUSTSEARCH-001',N'SMOKE-REALTIME-001');
    DECLARE @RealtimeId bigint=(SELECT Id FROM dbo.RealtimeEvents WHERE TenantId=@TenantId AND DeduplicationKey=N'SMOKE-REALTIME-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.NotificationOutbox WHERE TenantId=@TenantId AND IdempotencyKey=N'SMOKE-NOTIFY-001') INSERT dbo.NotificationOutbox(TenantId,StoreId,AlertId,RealtimeEventId,Channel,EventType,ContractVersion,PayloadJson,Status,NextAttemptUtc,CorrelationId,IdempotencyKey,CreatedUtc)VALUES(@TenantId,@StoreId,@AlertId,@RealtimeId,N'SignalR',N'alert.created',1,N'{"source":"smoke"}',1,@Now,N'SMOKE-RUN-CUSTSEARCH-001',N'SMOKE-NOTIFY-001',@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.IntegrationConfigurations WHERE TenantId=@TenantId AND Provider=N'SMOKE-WEBHOOK') INSERT dbo.IntegrationConfigurations(TenantId,Provider,IntegrationType,Enabled,EndpointBaseUrl,CredentialReference,WebhookSigningSecretReference,TimeoutSeconds,RetryMaxAttempts,RetryBaseDelaySeconds,CreatedUtc,UpdatedUtc)VALUES(@TenantId,N'SMOKE-WEBHOOK',2,0,N'https://example.invalid/custsearch-smoke',N'env://SMOKE_WEBHOOK_CREDENTIAL',N'env://SMOKE_WEBHOOK_SIGNING',10,3,5,@Now,@Now);
    DECLARE @IntegrationId bigint=(SELECT Id FROM dbo.IntegrationConfigurations WHERE TenantId=@TenantId AND Provider=N'SMOKE-WEBHOOK');
    IF NOT EXISTS(SELECT 1 FROM dbo.IntegrationOutbox WHERE TenantId=@TenantId AND IdempotencyKey=N'SMOKE-INTEGRATION-001') INSERT dbo.IntegrationOutbox(TenantId,IntegrationConfigurationId,Provider,Destination,EventType,ContractVersion,PayloadJson,PayloadHash,Status,MaxAttempts,RetryBaseDelaySeconds,NextAttemptUtc,CorrelationId,IdempotencyKey,CreatedUtc)VALUES(@TenantId,@IntegrationId,N'SMOKE-WEBHOOK',N'https://example.invalid/custsearch-smoke',N'smoke.event',1,N'{"source":"smoke"}',REPLICATE(N'0',64),1,3,5,@Now,N'SMOKE-RUN-CUSTSEARCH-001',N'SMOKE-INTEGRATION-001',@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.Cameras WHERE TenantId=@TenantId AND CameraCode=N'SMOKE-CAMERA-001') INSERT dbo.Cameras(TenantId,StoreId,CameraCode,Name,RtspConfigurationReference,Direction,Status,IsActive,CreatedUtc,UpdatedUtc)VALUES(@TenantId,@StoreId,N'SMOKE-CAMERA-001',N'Smoke Demo Camera',N'env://SMOKE_CAMERA_RTSP',1,1,1,@Now,@Now);
    DECLARE @CameraId bigint=(SELECT Id FROM dbo.Cameras WHERE TenantId=@TenantId AND CameraCode=N'SMOKE-CAMERA-001');
    IF NOT EXISTS(SELECT 1 FROM dbo.CameraZoneConfigurations WHERE TenantId=@TenantId AND CameraId=@CameraId AND ZoneCode=N'SMOKE-ENTRY') INSERT dbo.CameraZoneConfigurations(TenantId,StoreId,CameraId,ZoneCode,Name,ZoneType,GeometryJson,Version,EffectiveUtc,IsActive,CreatedUtc)VALUES(@TenantId,@StoreId,@CameraId,N'SMOKE-ENTRY',N'Smoke Entry Zone',1,N'{"points":[[0,0],[1,0],[1,1],[0,1]]}',1,@Now,1,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.PersonTrackSessions WHERE TenantId=@TenantId AND PersonTrackId=N'SMOKE-TRACK-001') INSERT dbo.PersonTrackSessions(TenantId,StoreId,CameraId,PersonTrackId,StartUtc,EndUtc,Confidence,SubjectKind,TrackingState,UpdatedUtc)VALUES(@TenantId,@StoreId,@CameraId,N'SMOKE-TRACK-001',DATEADD(minute,-20,@Now),DATEADD(minute,-10,@Now),0.90,1,2,@Now);
    IF NOT EXISTS(SELECT 1 FROM dbo.CustomerRecognitionConsents WHERE TenantId=@TenantId AND CustomerId=@CustomerId AND ConsentVersion=N'SMOKE-CONSENT-V1') INSERT dbo.CustomerRecognitionConsents(TenantId,CustomerId,ConsentType,Purpose,GrantedUtc,ConsentVersion,CapturedByUserId,CreatedUtc)VALUES(@TenantId,@CustomerId,1,N'Explicit UAT consent only; no biometric template seeded',@Now,N'SMOKE-CONSENT-V1',@TenantAdminId,@Now);

    IF NOT EXISTS(SELECT 1 FROM dbo.ReportExportJobs WHERE TenantId=@TenantId AND ReportType=N'Tenant.SmokeOperational') INSERT dbo.ReportExportJobs(TenantId,RequestedByUserId,ReportType,FilterJson,Format,Status,ProgressPercent,RequestedUtc,AttemptCount)VALUES(@TenantId,@TenantAdminId,N'Tenant.SmokeOperational',N'{"storeCode":"SMOKE-STORE-001"}',1,1,0,@Now,0);
    IF NOT EXISTS(SELECT 1 FROM dbo.OperationalSettings WHERE Scope=3 AND TenantId=@TenantId AND StoreId=@StoreId AND [Key]=N'Smoke.DemoMode') INSERT dbo.OperationalSettings(Scope,TenantId,StoreId,[Key],ValueJson,CreatedUtc,UpdatedUtc)VALUES(3,@TenantId,@StoreId,N'Smoke.DemoMode',N'{"enabled":true}',@Now,@Now);

    -- A minimal second tenant/store/customer exists solely for executable cross-tenant denial tests.
    IF NOT EXISTS(SELECT 1 FROM dbo.Tenants WHERE TenantCode=N'SMOKE-TENANT-002')
        INSERT dbo.Tenants(TenantCode,LegalName,DisplayName,TimeZone,PrimaryContactName,PrimaryEmail,PrimaryMobile,CountryCode,CurrencyCode,SubscriptionPlanId,SubscriptionStatus,SubscriptionStartsUtc,MaxStores,MaxUsers,MaxCameras,MaxStaff)
        VALUES(N'SMOKE-TENANT-002',N'CustSearch Isolation Retail Private Limited',N'CustSearch Isolation Retail',N'Asia/Kolkata',N'Isolation Owner',N'smoke.tenantb@custsearch.local',N'+910000000004',N'IN',N'INR',@PlanId,2,@Now,2,5,2,5);
    DECLARE @TenantBId bigint=(SELECT Id FROM dbo.Tenants WHERE TenantCode=N'SMOKE-TENANT-002');
    IF NOT EXISTS(SELECT 1 FROM dbo.TenantSubscriptions WHERE TenantId=@TenantBId AND Status IN(1,2,3)) INSERT dbo.TenantSubscriptions(TenantId,SubscriptionPlanId,BillingCycle,Status,StartsUtc,AutoRenew)VALUES(@TenantBId,@PlanId,1,2,@Now,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE TenantId=@TenantBId AND NormalizedName=N'TENANTADMIN') INSERT dbo.Roles(TenantId,Scope,Name,NormalizedName,Description,IsSystem,IsActive,CreatedUtc)VALUES(@TenantBId,2,N'TenantAdmin',N'TENANTADMIN',N'Isolation tenant administrator',1,1,@Now);
    DECLARE @TenantBRoleId bigint=(SELECT Id FROM dbo.Roles WHERE TenantId=@TenantBId AND NormalizedName=N'TENANTADMIN');
    INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT @TenantBRoleId,p.Id FROM dbo.Permissions p WHERE p.Scope=2 AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=@TenantBRoleId AND rp.PermissionId=p.Id);
    IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE TenantId=@TenantBId AND NormalizedEmail=N'SMOKE.TENANTBADMIN@CUSTSEARCH.LOCAL') INSERT dbo.Users(TenantId,Scope,UserName,NormalizedUserName,Email,NormalizedEmail,DisplayName,PasswordHash,SecurityStamp,IsActive,CreatedUtc)VALUES(@TenantBId,2,N'smoke.tenantbadmin',N'SMOKE.TENANTBADMIN',N'smoke.tenantbadmin@custsearch.local',N'SMOKE.TENANTBADMIN@CUSTSEARCH.LOCAL',N'Smoke Tenant B Admin',@PasswordHash,REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N''),1,@Now);
    DECLARE @TenantBAdminId bigint=(SELECT Id FROM dbo.Users WHERE TenantId=@TenantBId AND NormalizedEmail=N'SMOKE.TENANTBADMIN@CUSTSEARCH.LOCAL');
    UPDATE dbo.Users SET PasswordHash=@PasswordHash,SecurityStamp=REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'') WHERE Id=@TenantBAdminId;
    IF NOT EXISTS(SELECT 1 FROM dbo.UserRoles WHERE UserId=@TenantBAdminId AND RoleId=@TenantBRoleId) INSERT dbo.UserRoles(UserId,RoleId,AssignedByUserId)VALUES(@TenantBAdminId,@TenantBRoleId,@PlatformUserId);
    IF NOT EXISTS(SELECT 1 FROM dbo.Stores WHERE TenantId=@TenantBId AND StoreCode=N'SMOKE-STORE-002') INSERT dbo.Stores(TenantId,StoreCode,StoreName,AddressLine1,City,StateOrProvince,PostalCode,CountryCode,TimeZone,IsActive)VALUES(@TenantBId,N'SMOKE-STORE-002',N'Isolation Store',N'2 Isolation Road',N'Ahmedabad',N'Gujarat',N'380002',N'IN',N'Asia/Kolkata',1);
    DECLARE @StoreBId bigint=(SELECT Id FROM dbo.Stores WHERE TenantId=@TenantBId AND StoreCode=N'SMOKE-STORE-002');
    IF NOT EXISTS(SELECT 1 FROM dbo.UserStoreAssignments WHERE TenantId=@TenantBId AND UserId=@TenantBAdminId AND StoreId=@StoreBId) INSERT dbo.UserStoreAssignments(TenantId,UserId,StoreId,IsPrimary,AssignedByUserId)VALUES(@TenantBId,@TenantBAdminId,@StoreBId,1,@TenantBAdminId);
    IF NOT EXISTS(SELECT 1 FROM dbo.Customers WHERE TenantId=@TenantBId AND CustomerCode=N'SMOKE-CUSTOMER-002') INSERT dbo.Customers(TenantId,CustomerCode,FirstName,LastName,Email,IsActive)VALUES(@TenantBId,N'SMOKE-CUSTOMER-002',N'Isolation',N'Customer',N'smoke.customerb@custsearch.local',1);
    DECLARE @CustomerBId bigint=(SELECT Id FROM dbo.Customers WHERE TenantId=@TenantBId AND CustomerCode=N'SMOKE-CUSTOMER-002');
    IF NOT EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments WHERE TenantId=@TenantBId AND CustomerId=@CustomerBId AND StoreId=@StoreBId) INSERT dbo.CustomerStoreAssignments(TenantId,CustomerId,StoreId,IsPrimary,AssignedByUserId)VALUES(@TenantBId,@CustomerBId,@StoreBId,1,@TenantBAdminId);

    COMMIT TRANSACTION;
    SELECT @TenantId TenantId,@StoreId StoreId,@PlatformUserId PlatformUserId,@TenantAdminId TenantAdminUserId,@StaffUserId StaffUserId,@CustomerId CustomerId,@InvoiceId RetailInvoiceId,@CameraId CameraId,@TenantBId IsolationTenantId,@StoreBId IsolationStoreId,@CustomerBId IsolationCustomerId;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
