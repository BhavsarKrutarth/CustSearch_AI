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

BEGIN TRY
    EXEC dbo.WorkerHeartbeat_Upsert @InstanceId=N'',@WorkerName=N'worker',@Status=1,@StartedUtc='2026-08-25';
    THROW 55210,'Invalid worker heartbeat was accepted.',1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER()=55210 THROW;
END CATCH;

BEGIN TRY
    EXEC dbo.SystemSetting_Upsert @TenantId=987654321,@StoreId=123456789,@SettingKey=N'WebhookRetryCount',@ValueType=2,@SettingValue=N'4',@UpdatedByUserId=1,@CorrelationId='phase16-negative';
    THROW 55218,'Cross-tenant/nonexistent store override was accepted.',1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER()=55218 THROW;
END CATCH;

BEGIN TRY
    EXEC dbo.SystemSetting_Upsert @TenantId=987654321,@StoreId=NULL,@SettingKey=N'autolinkhouseholdfromfacesimilarity',@ValueType=1,@SettingValue=N'true',@UpdatedByUserId=1,@CorrelationId='phase16-negative';
    THROW 55219,'Tenant safety override was accepted.',1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER()=55219 THROW;
END CATCH;

BEGIN TRANSACTION;
DECLARE @Token NVARCHAR(32)=REPLACE(CONVERT(NVARCHAR(36),NEWID()),N'-',N'');
DECLARE @PlatformName NVARCHAR(100)=N'phase16-platform-'+@Token;
INSERT dbo.Users(TenantId,Scope,UserName,NormalizedUserName,Email,NormalizedEmail,DisplayName,PasswordHash,SecurityStamp,IsActive,CreatedUtc)
VALUES(NULL,1,@PlatformName,UPPER(@PlatformName),@PlatformName+N'@invalid.test',UPPER(@PlatformName+N'@invalid.test'),N'Phase 16 Platform',N'not-real',CONVERT(NVARCHAR(36),NEWID()),1,SYSUTCDATETIME());
DECLARE @PlatformUserId BIGINT=SCOPE_IDENTITY();

INSERT dbo.Tenants(TenantCode,LegalName,DisplayName,TimeZone,MaxStaff) VALUES(N'P16'+LEFT(@Token,12),N'Phase 16 Tenant',N'Phase 16',N'Asia/Kolkata',5);
DECLARE @TenantId BIGINT=SCOPE_IDENTITY();
DECLARE @TenantName NVARCHAR(100)=N'phase16-tenant-'+@Token;
INSERT dbo.Users(TenantId,Scope,UserName,NormalizedUserName,Email,NormalizedEmail,DisplayName,PasswordHash,SecurityStamp,IsActive,CreatedUtc)
VALUES(@TenantId,2,@TenantName,UPPER(@TenantName),@TenantName+N'@invalid.test',UPPER(@TenantName+N'@invalid.test'),N'Phase 16 Tenant',N'not-real',CONVERT(NVARCHAR(36),NEWID()),1,SYSUTCDATETIME());
DECLARE @TenantUserId BIGINT=SCOPE_IDENTITY();
INSERT dbo.Stores(TenantId,StoreCode,StoreName,AddressLine1,City,StateOrProvince,PostalCode,CountryCode,TimeZone)
VALUES(@TenantId,N'MAIN',N'Phase 16 Store',N'Test',N'Ahmedabad',N'Gujarat',N'380001','IN',N'Asia/Kolkata');
DECLARE @StoreId BIGINT=SCOPE_IDENTITY();
INSERT dbo.Customers(TenantId,CustomerCode,FirstName) VALUES(@TenantId,N'P16-CUSTOMER',N'Privacy');
DECLARE @CustomerId BIGINT=SCOPE_IDENTITY();
INSERT dbo.CustomerRecognitionConsents(TenantId,CustomerId,ConsentType,Purpose,GrantedUtc,ExpiresUtc,ConsentVersion,CapturedByUserId,EvidenceReference,CreatedUtc)
VALUES(@TenantId,@CustomerId,1,N'Phase 16 expired consent',DATEADD(DAY,-90,SYSUTCDATETIME()),DATEADD(DAY,-60,SYSUTCDATETIME()),N'v1',@TenantUserId,N'test-evidence',DATEADD(DAY,-90,SYSUTCDATETIME()));
DECLARE @ConsentId BIGINT=SCOPE_IDENTITY();
INSERT dbo.BiometricTemplates(TenantId,StoreId,CustomerId,ConsentId,EncryptedTemplate,Nonce,AuthenticationTag,EncryptionKeyReference,Algorithm,TemplateVersion,Status,CreatedUtc)
VALUES(@TenantId,@StoreId,@CustomerId,@ConsentId,0x010203,CONVERT(VARBINARY(12),REPLICATE('N',12)),CONVERT(VARBINARY(16),REPLICATE('T',16)),N'test-key',N'test',N'v1',1,DATEADD(DAY,-90,SYSUTCDATETIME()));
DECLARE @TemplateId BIGINT=SCOPE_IDENTITY();
INSERT dbo.AnonymousVisitors(TenantId,StoreId,VisitorCode,FirstSeenUtc,LastSeenUtc,IsActive,CreatedUtc,UpdatedUtc)
VALUES(@TenantId,@StoreId,N'P16-OLD-VISITOR',DATEADD(DAY,-70,SYSUTCDATETIME()),DATEADD(DAY,-60,SYSUTCDATETIME()),0,DATEADD(DAY,-70,SYSUTCDATETIME()),DATEADD(DAY,-60,SYSUTCDATETIME()));
DECLARE @VisitorId BIGINT=SCOPE_IDENTITY();

EXEC dbo.SystemSetting_Upsert @TenantId=NULL,@StoreId=NULL,@SettingKey=N'Phase16TestSetting',@ValueType=2,@SettingValue=N'10',@Description=N'test',@UpdatedByUserId=@PlatformUserId,@CorrelationId='phase16-platform-setting';
EXEC dbo.SystemSetting_Upsert @TenantId=@TenantId,@StoreId=NULL,@SettingKey=N'Phase16TestSetting',@ValueType=2,@SettingValue=N'20',@Description=N'test',@UpdatedByUserId=@TenantUserId,@CorrelationId='phase16-tenant-setting';
EXEC dbo.SystemSetting_Upsert @TenantId=@TenantId,@StoreId=@StoreId,@SettingKey=N'Phase16TestSetting',@ValueType=2,@SettingValue=N'30',@Description=N'test',@UpdatedByUserId=@TenantUserId,@CorrelationId='phase16-store-setting';
DECLARE @Effective TABLE(Id BIGINT,TenantId BIGINT,StoreId BIGINT,SettingKey NVARCHAR(100),ValueType TINYINT,SettingValue NVARCHAR(1000),Description NVARCHAR(500),UpdatedByUserId BIGINT,CreatedUtc DATETIME2(7),UpdatedUtc DATETIME2(7),SourceScope NVARCHAR(20));
INSERT @Effective EXEC dbo.SystemSetting_List @TenantId=@TenantId,@StoreId=@StoreId,@IncludeInherited=1;
IF NOT EXISTS(SELECT 1 FROM @Effective WHERE SettingKey=N'Phase16TestSetting' AND SettingValue=N'30' AND SourceScope=N'Store') THROW 55211,'Store precedence failed.',1;
DELETE @Effective;
INSERT @Effective EXEC dbo.SystemSetting_List @TenantId=@TenantId,@StoreId=NULL,@IncludeInherited=1;
IF NOT EXISTS(SELECT 1 FROM @Effective WHERE SettingKey=N'Phase16TestSetting' AND SettingValue=N'20' AND SourceScope=N'Tenant') THROW 55212,'Tenant precedence failed.',1;

EXEC dbo.WorkerHeartbeat_Upsert @InstanceId=N'phase16-test',@WorkerName=N'CustSearch.Worker',@Status=1,@StartedUtc='2026-08-25T00:00:00Z',@LastSuccessfulCycleUtc='2026-08-25T00:00:01Z',@MetadataJson=N'{"test":true}';
IF NOT EXISTS(SELECT 1 FROM dbo.WorkerHeartbeats WHERE InstanceId=N'phase16-test' AND Status=1) THROW 55214,'Worker heartbeat was not persisted.',1;
IF (SELECT COUNT(*) FROM dbo.AuditLogs WHERE UserId IN(@PlatformUserId,@TenantUserId) AND Action=N'SystemSettingUpserted')<>3 THROW 55215,'Settings audit rows are incomplete.',1;

DECLARE @Retention TABLE(TemplatesDisabled INT,TemplatesMarkedDeleted INT,AnonymousVisitorsDeleted INT);
INSERT @Retention EXEC dbo.OperationalRetention_Run @BatchSize=100,@RecognitionMetadataRetentionDays=30;
IF NOT EXISTS(SELECT 1 FROM @Retention WHERE TemplatesDisabled=1 AND AnonymousVisitorsDeleted=1) THROW 55220,'Retention counts are inaccurate.',1;
IF EXISTS(SELECT 1 FROM dbo.AnonymousVisitors WHERE Id=@VisitorId) THROW 55221,'Expired anonymous visitor was retained.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.BiometricTemplates WHERE Id=@TemplateId AND Status=2 AND DATALENGTH(EncryptedTemplate)=0 AND DATALENGTH(Nonce)=0 AND DATALENGTH(AuthenticationTag)=0) THROW 55222,'Expired-consent template was not disabled and erased.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AuditLogs WHERE TenantId=@TenantId AND Action=N'RecognitionRetentionApplied') OR NOT EXISTS(SELECT 1 FROM dbo.AuditLogs WHERE TenantId=@TenantId AND StoreId=@StoreId AND Action=N'AnonymousVisitorRetentionDeleted') THROW 55223,'Retention audit evidence is incomplete.',1;

DECLARE @Audit TABLE(Id BIGINT,TenantId BIGINT,StoreId BIGINT,UserId BIGINT,ActorType NVARCHAR(50),Action NVARCHAR(100),EntityType NVARCHAR(100),EntityId NVARCHAR(100),IpAddress VARCHAR(64),CorrelationId VARCHAR(64),CreatedUtc DATETIME2(7),TotalCount BIGINT);
INSERT @Audit EXEC dbo.AuditLog_Search @TenantId=@TenantId,@AllowedStoreIdsJson=N'[]',@TenantWide=1,@Action=N'SystemSettingUpserted',@PageNumber=1,@PageSize=10;
IF (SELECT COUNT(*) FROM @Audit)<>2 OR EXISTS(SELECT 1 FROM @Audit WHERE TenantId<>@TenantId) THROW 55216,'Tenant audit isolation failed.',1;

ROLLBACK TRANSACTION;
IF EXISTS(SELECT 1 FROM dbo.Users WHERE Id IN(@PlatformUserId,@TenantUserId)) OR EXISTS(SELECT 1 FROM dbo.WorkerHeartbeats WHERE InstanceId=N'phase16-test') THROW 55217,'Rollback-only test data leaked.',1;
SELECT N'Phase 16 rollback-only database tests passed.' Result;
GO
