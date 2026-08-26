/* Transactional synthetic verification. Does not open a camera stream and leaves no business rows. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT OFF;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @TenantId BIGINT,@StoreId BIGINT,@CameraId BIGINT,@UserId BIGINT;
SELECT TOP(1)@TenantId=c.TenantId,@StoreId=c.StoreId,@CameraId=c.Id FROM dbo.Cameras c WHERE c.IsActive=1 ORDER BY c.Id;
SELECT TOP(1)@UserId=u.Id FROM dbo.Users u WHERE u.TenantId=@TenantId AND u.IsActive=1 ORDER BY u.Id;
IF @TenantId IS NULL OR @UserId IS NULL THROW 56190,'Synthetic Phase 18 prerequisites are unavailable.',1;

/* Wrong-tenant camera must fail before any ingestion row is inserted. */
BEGIN TRY
 DECLARE @WrongTenant BIGINT=@TenantId+987654,@WrongNow DATETIME2(7)=SYSUTCDATETIME();
 EXEC dbo.SecurityObservation_Ingest @TenantId=@WrongTenant,@StoreId=@StoreId,@CameraId=@CameraId,@ServiceKeyId=N'phase18-test',@IdempotencyKey=N'wrong-tenant',@NonceHash=0x01,@BodyHash=0x02,@SignedUtc=@WrongNow,@ObservationType=1,@OccurredUtc=@WrongNow,@Confidence=.9,@CorrelationId=N'phase18-wrong-tenant',@ModelVersion=N'test';
 THROW 56191,'Wrong-tenant camera was accepted.',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()=56191 THROW; END CATCH;

SET XACT_ABORT ON;
BEGIN TRAN;
BEGIN TRY
 DECLARE @Suffix NVARCHAR(32)=REPLACE(CONVERT(NVARCHAR(36),NEWID()),N'-',N'');
 DECLARE @Idempotency NVARCHAR(128)=N'phase18-'+@Suffix,@Nonce BINARY(32)=HASHBYTES('SHA2_256',N'nonce-'+@Suffix),@Body BINARY(32)=HASHBYTES('SHA2_256',N'body-'+@Suffix);
 DECLARE @Now DATETIME2(7)=SYSUTCDATETIME();
 DECLARE @First TABLE(Id BIGINT,IngestionRequestId BIGINT,WasDuplicate BIT);
 DECLARE @Second TABLE(Id BIGINT,IngestionRequestId BIGINT,WasDuplicate BIT);
 INSERT @First EXEC dbo.SecurityObservation_Ingest @TenantId=@TenantId,@StoreId=@StoreId,@CameraId=@CameraId,@ServiceKeyId=N'phase18-test',@IdempotencyKey=@Idempotency,@NonceHash=@Nonce,@BodyHash=@Body,@SignedUtc=@Now,@PersonTrackId=@Suffix,@ObservationType=4,@OccurredUtc=@Now,@Confidence=.91,@CorrelationId=@Suffix,@ModelVersion=N'phase18-synthetic';
 INSERT @Second EXEC dbo.SecurityObservation_Ingest @TenantId=@TenantId,@StoreId=@StoreId,@CameraId=@CameraId,@ServiceKeyId=N'phase18-test',@IdempotencyKey=@Idempotency,@NonceHash=@Nonce,@BodyHash=@Body,@SignedUtc=@Now,@PersonTrackId=@Suffix,@ObservationType=4,@OccurredUtc=@Now,@Confidence=.91,@CorrelationId=@Suffix,@ModelVersion=N'phase18-synthetic';
 IF NOT EXISTS(SELECT 1 FROM @First WHERE WasDuplicate=0) OR NOT EXISTS(SELECT 1 FROM @Second WHERE WasDuplicate=1) THROW 56192,'Observation idempotency failed.',1;

 DECLARE @RuleCode NVARCHAR(100)=N'SYN-'+@Suffix;
 EXEC dbo.SecurityRule_CreateVersion @TenantId=@TenantId,@StoreId=@StoreId,@RuleCode=@RuleCode,@Name=N'Synthetic rule',@IsEnabled=1,@Severity=3,@ConfigurationJson=N'{"riskThreshold":70}',@UserId=@UserId,@CorrelationId=@Suffix;
 EXEC dbo.SecurityRule_CreateVersion @TenantId=@TenantId,@StoreId=@StoreId,@RuleCode=@RuleCode,@Name=N'Synthetic rule',@IsEnabled=1,@Severity=3,@ConfigurationJson=N'{"riskThreshold":75}',@UserId=@UserId,@CorrelationId=@Suffix;
 IF (SELECT COUNT(*) FROM dbo.SecurityRules WHERE TenantId=@TenantId AND StoreId=@StoreId AND RuleCode=@RuleCode)<>2 OR (SELECT MAX(Version) FROM dbo.SecurityRules WHERE TenantId=@TenantId AND StoreId=@StoreId AND RuleCode=@RuleCode)<>2 THROW 56194,'Rule versioning failed.',1;

 DECLARE @IncidentId BIGINT,@IncidentNumber NVARCHAR(100)=N'SYN-'+@Suffix;
 INSERT dbo.SecurityIncidents(IncidentNumber,TenantId,StoreId,PersonTrackId,IncidentType,Severity,RiskScore,RuleCode,RuleVersion,Status,FirstObservedUtc,ExitObservedUtc,Currency)VALUES(@IncidentNumber,@TenantId,@StoreId,@Suffix,1,4,80,N'SYNTHETIC',1,2,SYSUTCDATETIME(),SYSUTCDATETIME(),N'INR');SET @IncidentId=SCOPE_IDENTITY();
 INSERT dbo.SecurityIncidentActions(TenantId,StoreId,SecurityIncidentId,ActionType,FromStatus,ToStatus,ActorType,CorrelationId)VALUES(@TenantId,@StoreId,@IncidentId,N'CandidateCreated',1,2,N'System',@Suffix);
 DECLARE @Version BINARY(8)=(SELECT RowVersion FROM dbo.SecurityIncidents WHERE Id=@IncidentId);
 EXEC dbo.SecurityIncident_Transition @TenantId,@StoreId,@IncidentId,5,@UserId,NULL,N'Synthetic review',@Version,@Suffix;
 SET @Version=(SELECT RowVersion FROM dbo.SecurityIncidents WHERE Id=@IncidentId);
 EXEC dbo.SecurityIncident_Transition @TenantId,@StoreId,@IncidentId,7,@UserId,N'SYNTHETIC_FALSE_POSITIVE',N'Expected scenario outcome',@Version,@Suffix;
 IF (SELECT Status FROM dbo.SecurityIncidents WHERE Id=@IncidentId)<>7 OR (SELECT COUNT(*) FROM dbo.SecurityIncidentActions WHERE SecurityIncidentId=@IncidentId)<3 THROW 56193,'Audited incident state flow failed.',1;
 ROLLBACK;
 SELECT N'Phase 18 synthetic SQL tests passed' Result;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
GO
