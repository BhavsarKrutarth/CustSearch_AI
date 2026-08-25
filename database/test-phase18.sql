USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;
BEGIN TRY
 DECLARE @suffix NVARCHAR(20)=CONVERT(NVARCHAR(20),DATEDIFF_BIG(MILLISECOND,'2020-01-01',SYSUTCDATETIME()));
 INSERT dbo.Tenants(TenantCode,LegalName,DisplayName,TimeZone,MaxStaff) VALUES(N'P18'+@suffix,N'Phase 18 Test',N'Phase 18 Test',N'Asia/Kolkata',5);DECLARE @TenantId BIGINT=SCOPE_IDENTITY();
 INSERT dbo.Stores(TenantId,StoreCode,StoreName,AddressLine1,City,StateOrProvince,PostalCode,CountryCode,TimeZone) VALUES(@TenantId,N'SEC',N'Security Test Store',N'Test',N'Surat',N'Gujarat',N'000000','IN',N'Asia/Kolkata');DECLARE @StoreId BIGINT=SCOPE_IDENTITY();
 DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
 INSERT dbo.Cameras(TenantId,StoreId,CameraCode,Name,RtspConfigurationReference,Status,Direction,IsActive,CreatedUtc,UpdatedUtc) VALUES(@TenantId,@StoreId,N'EXIT-1',N'Exit camera',N'secret://phase18-test',1,2,1,@now,@now);DECLARE @CameraId BIGINT=SCOPE_IDENTITY();
 DECLARE @nonce BINARY(32)=HASHBYTES('SHA2_256','p18-nonce'),@body BINARY(32)=HASHBYTES('SHA2_256','p18-body'),@badBody BINARY(32)=HASHBYTES('SHA2_256','changed-body'),@otherNonce BINARY(32)=HASHBYTES('SHA2_256','other');DECLARE @WrongStoreId BIGINT=@StoreId+1;
 DECLARE @first TABLE(Id BIGINT,IngestionRequestId BIGINT,WasDuplicate BIT);INSERT @first EXEC dbo.SecurityObservation_Ingest @TenantId,@StoreId,@CameraId,N'p18-test',N'p18-idem',@nonce,@body,@now,NULL,NULL,N'anon-track',4,@now,NULL,NULL,NULL,0.9000,N'p18-correlation',N'synthetic-1',N'{"source":"synthetic"}';
 IF NOT EXISTS(SELECT 1 FROM @first WHERE WasDuplicate=0) THROW 56110,'First ingestion was not created.',1;
 DECLARE @duplicate TABLE(Id BIGINT,IngestionRequestId BIGINT,WasDuplicate BIT);INSERT @duplicate EXEC dbo.SecurityObservation_Ingest @TenantId,@StoreId,@CameraId,N'p18-test',N'p18-idem',@nonce,@body,@now,NULL,NULL,N'anon-track',4,@now,NULL,NULL,NULL,0.9000,N'p18-correlation',N'synthetic-1',N'{"source":"synthetic"}';
 IF NOT EXISTS(SELECT 1 FROM @duplicate WHERE WasDuplicate=1) OR (SELECT COUNT(*) FROM dbo.SecurityObservations WHERE TenantId=@TenantId)<>1 THROW 56111,'Idempotent replay was not deduplicated.',1;
 BEGIN TRY EXEC dbo.SecurityObservation_Ingest @TenantId,@StoreId,@CameraId,N'p18-test',N'p18-idem',@nonce,@badBody,@now,NULL,NULL,N'anon-track',4,@now,NULL,NULL,NULL,0.9,N'p18',N'synthetic-1',NULL;THROW 56112,'Changed-body replay was accepted.',1;END TRY BEGIN CATCH IF ERROR_NUMBER()<>56006 THROW;END CATCH;
 IF @@TRANCOUNT<>0 ROLLBACK;
 IF EXISTS(SELECT 1 FROM dbo.SecurityIngestionRequests WHERE TenantId=@TenantId) OR EXISTS(SELECT 1 FROM dbo.SecurityObservations WHERE TenantId=@TenantId) THROW 56115,'Replay rejection left synthetic residue.',1;
 BEGIN TRY EXEC dbo.SecurityObservation_Ingest @TenantId,@WrongStoreId,@CameraId,N'p18-test',N'p18-other',@otherNonce,@body,@now,NULL,NULL,N'anon-track',2,@now,NULL,NULL,NULL,0.9,N'p18',N'synthetic-1',NULL;THROW 56113,'Wrong-store camera was accepted.',1;END TRY BEGIN CATCH IF ERROR_NUMBER()<>56005 THROW;END CATCH;
 IF EXISTS(SELECT 1 FROM dbo.SecurityIncidents WHERE TenantId=@TenantId) THROW 56114,'AI observation incorrectly auto-created or confirmed an incident.',1;
 IF @@TRANCOUNT<>0 ROLLBACK;SELECT N'Phase 18 rollback-only database tests passed' Result;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
GO
