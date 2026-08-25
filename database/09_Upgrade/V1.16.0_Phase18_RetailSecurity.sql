/* CustSearch AI Phase 18 - reviewable retail-security foundation. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL
   OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')
    THROW 56000,'Phase 16 V1.15.0 must be installed before Phase 18.',1;
GO

IF OBJECT_ID(N'dbo.SecurityRules',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityRules(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityRules PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NULL,
  RuleCode NVARCHAR(100) NOT NULL,Name NVARCHAR(200) NOT NULL,IsEnabled BIT NOT NULL,Severity TINYINT NOT NULL,
  ConfigurationJson NVARCHAR(4000) NOT NULL,Version INT NOT NULL,CreatedByUserId BIGINT NULL,CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityRules_Created DEFAULT SYSUTCDATETIME(),UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityRules_Updated DEFAULT SYSUTCDATETIME(),RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_SecurityRules_Tenant FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_SecurityRules_Store FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),CONSTRAINT FK_SecurityRules_User FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT CK_SecurityRules_Severity CHECK(Severity BETWEEN 1 AND 5),CONSTRAINT CK_SecurityRules_Version CHECK(Version>0),CONSTRAINT CK_SecurityRules_Json CHECK(ISJSON(ConfigurationJson)=1));
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityRules') AND name=N'UX_SecurityRules_Tenant_Default') CREATE UNIQUE INDEX UX_SecurityRules_Tenant_Default ON dbo.SecurityRules(TenantId,RuleCode,Version) WHERE StoreId IS NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityRules') AND name=N'UX_SecurityRules_Store') CREATE UNIQUE INDEX UX_SecurityRules_Store ON dbo.SecurityRules(TenantId,StoreId,RuleCode,Version) WHERE StoreId IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.SecurityIngestionRequests',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityIngestionRequests(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityIngestionRequests PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CameraId BIGINT NOT NULL,
  ServiceKeyId NVARCHAR(100) NOT NULL,IdempotencyKey NVARCHAR(128) NOT NULL,NonceHash BINARY(32) NOT NULL,BodyHash BINARY(32) NOT NULL,SignedUtc DATETIME2(7) NOT NULL,ReceivedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityIngestion_Received DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_SecurityIngestion_Camera FOREIGN KEY(TenantId,StoreId,CameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),CONSTRAINT CK_SecurityIngestion_Time CHECK(SignedUtc<=DATEADD(MINUTE,5,ReceivedUtc) AND SignedUtc>=DATEADD(MINUTE,-5,ReceivedUtc)));
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityIngestionRequests') AND name=N'UX_SecurityIngestion_Idempotency') CREATE UNIQUE INDEX UX_SecurityIngestion_Idempotency ON dbo.SecurityIngestionRequests(ServiceKeyId,IdempotencyKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityIngestionRequests') AND name=N'UX_SecurityIngestion_Nonce') CREATE UNIQUE INDEX UX_SecurityIngestion_Nonce ON dbo.SecurityIngestionRequests(ServiceKeyId,NonceHash);
GO

/* Composite candidate key lets child FKs prove that a zone belongs to the same tenant/store/camera. */
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraZoneConfigurations') AND name=N'UX_CameraZones_Tenant_Store_Camera_Id')
 CREATE UNIQUE INDEX UX_CameraZones_Tenant_Store_Camera_Id ON dbo.CameraZoneConfigurations(TenantId,StoreId,CameraId,Id);
GO

IF OBJECT_ID(N'dbo.SecurityObservations',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityObservations(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityObservations PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CameraId BIGINT NOT NULL,IngestionRequestId BIGINT NOT NULL,
  VisitId BIGINT NULL,PersonTrackSessionId BIGINT NULL,PersonTrackId NVARCHAR(200) NULL,ObservationType TINYINT NOT NULL,OccurredUtc DATETIME2(7) NOT NULL,ZoneId BIGINT NULL,ProductId BIGINT NULL,ProductCategoryId BIGINT NULL,
  Confidence DECIMAL(5,4) NOT NULL,CorrelationId NVARCHAR(64) NOT NULL,ModelVersion NVARCHAR(100) NOT NULL,MetadataJson NVARCHAR(4000) NULL,CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityObservations_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_SecurityObservations_Ingestion FOREIGN KEY(IngestionRequestId) REFERENCES dbo.SecurityIngestionRequests(Id),CONSTRAINT FK_SecurityObservations_Camera FOREIGN KEY(TenantId,StoreId,CameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),
  CONSTRAINT FK_SecurityObservations_Visit FOREIGN KEY(TenantId,StoreId,VisitId) REFERENCES dbo.CustomerVisits(TenantId,StoreId,Id),CONSTRAINT FK_SecurityObservations_Track FOREIGN KEY(TenantId,StoreId,PersonTrackSessionId) REFERENCES dbo.PersonTrackSessions(TenantId,StoreId,Id),
  CONSTRAINT FK_SecurityObservations_Zone FOREIGN KEY(TenantId,StoreId,CameraId,ZoneId) REFERENCES dbo.CameraZoneConfigurations(TenantId,StoreId,CameraId,Id),CONSTRAINT FK_SecurityObservations_Product FOREIGN KEY(TenantId,ProductId) REFERENCES dbo.Products(TenantId,Id),
  CONSTRAINT CK_SecurityObservations_Type CHECK(ObservationType BETWEEN 1 AND 10),CONSTRAINT CK_SecurityObservations_Confidence CHECK(Confidence BETWEEN 0 AND 1),CONSTRAINT CK_SecurityObservations_Metadata CHECK(MetadataJson IS NULL OR (ISJSON(MetadataJson)=1 AND DATALENGTH(MetadataJson)<=8000)));
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityObservations') AND name=N'IX_SecurityObservations_ScopeTrackTime') CREATE INDEX IX_SecurityObservations_ScopeTrackTime ON dbo.SecurityObservations(TenantId,StoreId,PersonTrackId,OccurredUtc DESC) INCLUDE(ObservationType,Confidence,ProductId,ZoneId);
GO

IF OBJECT_ID(N'dbo.SecurityIncidents',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityIncidents(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityIncidents PRIMARY KEY,IncidentNumber NVARCHAR(100) NOT NULL,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,VisitId BIGINT NULL,PersonTrackSessionId BIGINT NULL,PersonTrackId NVARCHAR(200) NULL,CustomerId BIGINT NULL,
  IncidentType TINYINT NOT NULL,Severity TINYINT NOT NULL,RiskScore DECIMAL(6,3) NOT NULL,RuleCode NVARCHAR(100) NOT NULL,RuleVersion INT NOT NULL,Status TINYINT NOT NULL,FirstObservedUtc DATETIME2(7) NOT NULL,ExitObservedUtc DATETIME2(7) NULL,
  EstimatedLossAmount DECIMAL(19,2) NULL,Currency CHAR(3) NOT NULL,AssignedUserId BIGINT NULL,ResolutionCode NVARCHAR(100) NULL,ResolutionNotes NVARCHAR(2000) NULL,ConfirmedByUserId BIGINT NULL,ConfirmedUtc DATETIME2(7) NULL,CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityIncidents_Created DEFAULT SYSUTCDATETIME(),UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityIncidents_Updated DEFAULT SYSUTCDATETIME(),RowVersion ROWVERSION NOT NULL,
  CONSTRAINT UX_SecurityIncidents_Tenant_Store_Id UNIQUE(TenantId,StoreId,Id),CONSTRAINT UX_SecurityIncidents_Tenant_Number UNIQUE(TenantId,IncidentNumber),CONSTRAINT FK_SecurityIncidents_Store FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
  CONSTRAINT FK_SecurityIncidents_Visit FOREIGN KEY(TenantId,StoreId,VisitId) REFERENCES dbo.CustomerVisits(TenantId,StoreId,Id),CONSTRAINT FK_SecurityIncidents_Track FOREIGN KEY(TenantId,StoreId,PersonTrackSessionId) REFERENCES dbo.PersonTrackSessions(TenantId,StoreId,Id),CONSTRAINT FK_SecurityIncidents_Customer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),CONSTRAINT FK_SecurityIncidents_Assigned FOREIGN KEY(AssignedUserId) REFERENCES dbo.Users(Id),CONSTRAINT FK_SecurityIncidents_Confirmed FOREIGN KEY(ConfirmedByUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT CK_SecurityIncidents_Type CHECK(IncidentType=1),CONSTRAINT CK_SecurityIncidents_Severity CHECK(Severity BETWEEN 1 AND 5),CONSTRAINT CK_SecurityIncidents_Risk CHECK(RiskScore BETWEEN 0 AND 100),CONSTRAINT CK_SecurityIncidents_Status CHECK(Status BETWEEN 1 AND 9),CONSTRAINT CK_SecurityIncidents_Confirmed CHECK((Status<>6 AND ConfirmedByUserId IS NULL AND ConfirmedUtc IS NULL) OR (Status=6 AND ConfirmedByUserId IS NOT NULL AND ConfirmedUtc IS NOT NULL AND ResolutionCode IS NOT NULL)));
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityIncidents') AND name=N'IX_SecurityIncidents_ScopeStatusTime') CREATE INDEX IX_SecurityIncidents_ScopeStatusTime ON dbo.SecurityIncidents(TenantId,StoreId,Status,CreatedUtc DESC,Id DESC) INCLUDE(Severity,RiskScore,IncidentNumber,AssignedUserId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityIncidents') AND name=N'IX_SecurityIncidents_TrackOpen') CREATE INDEX IX_SecurityIncidents_TrackOpen ON dbo.SecurityIncidents(TenantId,StoreId,PersonTrackId,UpdatedUtc DESC) WHERE Status<6;
GO

/* Archived incidents retain confirmed-loss provenance; other non-confirmed states cannot fabricate it. */
IF OBJECT_ID(N'dbo.CK_SecurityIncidents_Confirmed',N'C') IS NOT NULL ALTER TABLE dbo.SecurityIncidents DROP CONSTRAINT CK_SecurityIncidents_Confirmed;
ALTER TABLE dbo.SecurityIncidents WITH CHECK ADD CONSTRAINT CK_SecurityIncidents_Confirmed CHECK((ConfirmedByUserId IS NULL AND ConfirmedUtc IS NULL) OR (Status IN(6,9) AND ConfirmedByUserId IS NOT NULL AND ConfirmedUtc IS NOT NULL AND ResolutionCode IS NOT NULL));
GO

IF OBJECT_ID(N'dbo.SecurityIncidentItems',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityIncidentItems(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityIncidentItems PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,SecurityIncidentId BIGINT NOT NULL,ProductId BIGINT NULL,ProductCategoryId BIGINT NULL,DisplayDescription NVARCHAR(400) NOT NULL,Quantity DECIMAL(18,4) NULL,UnitValue DECIMAL(19,2) NULL,ProductConfidence DECIMAL(5,4) NULL,PaymentMatchStatus TINYINT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityIncidentItems_Created DEFAULT SYSUTCDATETIME(),CONSTRAINT FK_SecurityIncidentItems_Incident FOREIGN KEY(TenantId,StoreId,SecurityIncidentId) REFERENCES dbo.SecurityIncidents(TenantId,StoreId,Id),CONSTRAINT FK_SecurityIncidentItems_Product FOREIGN KEY(TenantId,ProductId) REFERENCES dbo.Products(TenantId,Id),CONSTRAINT CK_SecurityIncidentItems_Quantity CHECK(Quantity IS NULL OR Quantity>0),CONSTRAINT CK_SecurityIncidentItems_Confidence CHECK(ProductConfidence IS NULL OR ProductConfidence BETWEEN 0 AND 1),CONSTRAINT CK_SecurityIncidentItems_Payment CHECK(PaymentMatchStatus BETWEEN 1 AND 5));
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityIncidentItems') AND name=N'IX_SecurityIncidentItems_Incident') CREATE INDEX IX_SecurityIncidentItems_Incident ON dbo.SecurityIncidentItems(TenantId,StoreId,SecurityIncidentId,Id);
GO

IF OBJECT_ID(N'dbo.SecurityIncidentEvidence',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityIncidentEvidence(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityIncidentEvidence PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,SecurityIncidentId BIGINT NOT NULL,EvidenceType TINYINT NOT NULL,CameraId BIGINT NULL,CapturedUtc DATETIME2(7) NOT NULL,StorageObjectKey NVARCHAR(500) NOT NULL,ContentHash BINARY(32) NOT NULL,StartUtc DATETIME2(7) NULL,EndUtc DATETIME2(7) NULL,RetentionUntilUtc DATETIME2(7) NOT NULL,IsRestricted BIT NOT NULL CONSTRAINT DF_SecurityEvidence_Restricted DEFAULT(1),DeletedUtc DATETIME2(7) NULL,CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityEvidence_Created DEFAULT SYSUTCDATETIME(),CONSTRAINT FK_SecurityEvidence_Incident FOREIGN KEY(TenantId,StoreId,SecurityIncidentId) REFERENCES dbo.SecurityIncidents(TenantId,StoreId,Id),CONSTRAINT FK_SecurityEvidence_Camera FOREIGN KEY(TenantId,StoreId,CameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),CONSTRAINT CK_SecurityEvidence_Key CHECK(StorageObjectKey<>N'' AND StorageObjectKey NOT LIKE N'%..%' AND StorageObjectKey NOT LIKE N'%:%' AND LEFT(StorageObjectKey,1) NOT IN(N'/',N'\')),CONSTRAINT CK_SecurityEvidence_Clip CHECK((StartUtc IS NULL AND EndUtc IS NULL) OR (StartUtc IS NOT NULL AND EndUtc>=StartUtc)),CONSTRAINT CK_SecurityEvidence_Retention CHECK(RetentionUntilUtc>=CapturedUtc));
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityIncidentEvidence') AND name=N'IX_SecurityEvidence_Retention') CREATE INDEX IX_SecurityEvidence_Retention ON dbo.SecurityIncidentEvidence(RetentionUntilUtc,DeletedUtc,Id) INCLUDE(TenantId,StoreId,SecurityIncidentId,StorageObjectKey);
GO

IF OBJECT_ID(N'dbo.SecurityIncidentActions',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityIncidentActions(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityIncidentActions PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,SecurityIncidentId BIGINT NOT NULL,ActionType NVARCHAR(100) NOT NULL,FromStatus TINYINT NULL,ToStatus TINYINT NULL,UserId BIGINT NULL,ActorType NVARCHAR(50) NOT NULL,ReasonCode NVARCHAR(100) NULL,Notes NVARCHAR(2000) NULL,OccurredUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityActions_Occurred DEFAULT SYSUTCDATETIME(),CorrelationId NVARCHAR(64) NOT NULL,CONSTRAINT FK_SecurityActions_Incident FOREIGN KEY(TenantId,StoreId,SecurityIncidentId) REFERENCES dbo.SecurityIncidents(TenantId,StoreId,Id),CONSTRAINT FK_SecurityActions_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_SecurityActions_Status CHECK((FromStatus IS NULL OR FromStatus BETWEEN 1 AND 9) AND (ToStatus IS NULL OR ToStatus BETWEEN 1 AND 9)));
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityIncidentActions') AND name=N'IX_SecurityActions_Timeline') CREATE INDEX IX_SecurityActions_Timeline ON dbo.SecurityIncidentActions(TenantId,StoreId,SecurityIncidentId,OccurredUtc,Id);
GO

IF OBJECT_ID(N'dbo.SecurityNotificationDeliveries',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityNotificationDeliveries(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityNotificationDeliveries PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,SecurityIncidentId BIGINT NOT NULL,Channel NVARCHAR(30) NOT NULL,RecipientUserId BIGINT NULL,DestinationReference NVARCHAR(200) NULL,Status TINYINT NOT NULL,AttemptCount INT NOT NULL CONSTRAINT DF_SecurityDelivery_Attempts DEFAULT(0),QueuedUtc DATETIME2(7) NOT NULL,NextAttemptUtc DATETIME2(7) NOT NULL,SentUtc DATETIME2(7) NULL,AcknowledgedUtc DATETIME2(7) NULL,ProviderMessageId NVARCHAR(200) NULL,FailureCode NVARCHAR(100) NULL,IdempotencyKey NVARCHAR(200) NOT NULL,CONSTRAINT FK_SecurityDelivery_Incident FOREIGN KEY(TenantId,StoreId,SecurityIncidentId) REFERENCES dbo.SecurityIncidents(TenantId,StoreId,Id),CONSTRAINT FK_SecurityDelivery_User FOREIGN KEY(RecipientUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_SecurityDelivery_Status CHECK(Status BETWEEN 1 AND 5),CONSTRAINT CK_SecurityDelivery_Attempts CHECK(AttemptCount>=0));
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityNotificationDeliveries') AND name=N'UX_SecurityDelivery_Idempotency') CREATE UNIQUE INDEX UX_SecurityDelivery_Idempotency ON dbo.SecurityNotificationDeliveries(IdempotencyKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityNotificationDeliveries') AND name=N'IX_SecurityDelivery_Work') CREATE INDEX IX_SecurityDelivery_Work ON dbo.SecurityNotificationDeliveries(Status,NextAttemptUtc,Id);
GO

IF OBJECT_ID(N'dbo.SecurityPaymentCorrelations',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.SecurityPaymentCorrelations(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityPaymentCorrelations PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,SecurityIncidentId BIGINT NOT NULL,InvoiceId BIGINT NULL,TransactionReference NVARCHAR(200) NULL,MatchType TINYINT NOT NULL,MatchScore DECIMAL(5,4) NOT NULL,MatchedUtc DATETIME2(7) NOT NULL,Notes NVARCHAR(1000) NULL,CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SecurityPayment_Created DEFAULT SYSUTCDATETIME(),CONSTRAINT FK_SecurityPayment_Incident FOREIGN KEY(TenantId,StoreId,SecurityIncidentId) REFERENCES dbo.SecurityIncidents(TenantId,StoreId,Id),CONSTRAINT FK_SecurityPayment_Invoice FOREIGN KEY(TenantId,StoreId,InvoiceId) REFERENCES dbo.RetailInvoices(TenantId,StoreId,Id),CONSTRAINT CK_SecurityPayment_Type CHECK(MatchType BETWEEN 1 AND 5),CONSTRAINT CK_SecurityPayment_Score CHECK(MatchScore BETWEEN 0 AND 1));
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SecurityPaymentCorrelations') AND name=N'IX_SecurityPayment_Incident') CREATE INDEX IX_SecurityPayment_Incident ON dbo.SecurityPaymentCorrelations(TenantId,StoreId,SecurityIncidentId,MatchedUtc DESC);
GO

DECLARE @Permissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
INSERT @Permissions VALUES
(N'Security.Incidents.View',N'View store-scoped security incidents.'),(N'Security.Incidents.Acknowledge',N'Acknowledge security incidents.'),(N'Security.Incidents.Assign',N'Assign incident reviewers.'),(N'Security.Incidents.Review',N'Review security incidents.'),(N'Security.Incidents.ConfirmLoss',N'Confirm a reviewed loss with reason.'),(N'Security.Incidents.Resolve',N'Resolve security incidents.'),(N'Security.Evidence.View',N'View restricted incident evidence.'),(N'Security.Evidence.Export',N'Export restricted incident evidence.'),(N'Security.Settings.View',N'View security settings.'),(N'Security.Settings.Manage',N'Manage security settings.'),(N'Security.Rules.View',N'View versioned security rules.'),(N'Security.Rules.Manage',N'Manage versioned security rules.'),(N'Security.Reports.View',N'View security reports.');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) SELECT 2,p.Name,p.Description,1,SYSUTCDATETIME() FROM @Permissions p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO

DECLARE @Defaults TABLE(SettingKey NVARCHAR(100),ValueType TINYINT,SettingValue NVARCHAR(1000),Description NVARCHAR(500));
INSERT @Defaults VALUES
(N'SecurityMonitoringEnabled',1,N'false',N'Shadow-mode security observation processing.'),(N'UnpaidExitDetectionEnabled',1,N'false',N'Reviewable unpaid-exit candidate generation.'),(N'RealtimeSecurityAlertsEnabled',1,N'false',N'Real-time security notifications after store acceptance.'),(N'EvidenceClipEnabled',1,N'false',N'Protected evidence clip capture.'),(N'SecurityMinimumConfidence',3,N'0.70',N'Minimum normalized observation confidence.'),(N'CriticalAlertMinimumConfidence',3,N'0.92',N'Minimum critical multi-signal confidence.'),(N'ExitGracePeriodSeconds',2,N'15',N'Grace period before evaluating exit.'),(N'CheckoutCorrelationWindowMinutes',2,N'30',N'POS correlation window.'),(N'IncidentDeduplicationWindowSeconds',2,N'120',N'Open incident duplicate suppression window.'),(N'EvidencePreEventSeconds',2,N'10',N'Evidence before event.'),(N'EvidencePostEventSeconds',2,N'20',N'Evidence after event.'),(N'UnreviewedEvidenceRetentionDays',2,N'14',N'Unreviewed evidence retention.'),(N'FalsePositiveEvidenceRetentionDays',2,N'7',N'False-positive evidence retention.'),(N'ConfirmedIncidentRetentionDays',2,N'365',N'Confirmed incident retention pending policy review.');
INSERT dbo.SystemSettings(TenantId,StoreId,SettingKey,ValueType,SettingValue,Description,UpdatedByUserId,CreatedUtc,UpdatedUtc) SELECT NULL,NULL,d.SettingKey,d.ValueType,d.SettingValue,d.Description,NULL,SYSUTCDATETIME(),SYSUTCDATETIME() FROM @Defaults d WHERE NOT EXISTS(SELECT 1 FROM dbo.SystemSettings s WHERE s.TenantId IS NULL AND s.StoreId IS NULL AND s.SettingKey=d.SettingKey);
GO

CREATE OR ALTER PROCEDURE dbo.SecurityObservation_Ingest
 @TenantId BIGINT,@StoreId BIGINT,@CameraId BIGINT,@ServiceKeyId NVARCHAR(100),@IdempotencyKey NVARCHAR(128),@NonceHash BINARY(32),@BodyHash BINARY(32),@SignedUtc DATETIME2(7),
 @VisitId BIGINT=NULL,@PersonTrackSessionId BIGINT=NULL,@PersonTrackId NVARCHAR(200)=NULL,@ObservationType TINYINT,@OccurredUtc DATETIME2(7),@ZoneId BIGINT=NULL,@ProductId BIGINT=NULL,@ProductCategoryId BIGINT=NULL,@Confidence DECIMAL(5,4),@CorrelationId NVARCHAR(64),@ModelVersion NVARCHAR(100),@MetadataJson NVARCHAR(4000)=NULL
AS
BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @TenantId<=0 OR @StoreId<=0 OR @CameraId<=0 OR NULLIF(@ServiceKeyId,N'') IS NULL OR NULLIF(@IdempotencyKey,N'') IS NULL OR @NonceHash IS NULL OR @BodyHash IS NULL THROW 56001,'Signed ingestion identity is incomplete.',1;
 IF ABS(DATEDIFF(SECOND,@SignedUtc,SYSUTCDATETIME()))>300 THROW 56002,'Signed ingestion timestamp is expired or outside allowed clock skew.',1;
 IF @ObservationType NOT BETWEEN 1 AND 10 OR @Confidence NOT BETWEEN 0 AND 1 OR @OccurredUtc>DATEADD(MINUTE,1,SYSUTCDATETIME()) THROW 56003,'Observation values are invalid.',1;
 IF @MetadataJson IS NOT NULL AND (ISJSON(@MetadataJson)<>1 OR DATALENGTH(@MetadataJson)>8000) THROW 56004,'Observation metadata is invalid or too large.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Cameras WHERE TenantId=@TenantId AND StoreId=@StoreId AND Id=@CameraId AND IsActive=1) THROW 56005,'Camera is inactive or outside the signed tenant/store scope.',1;
 BEGIN TRY BEGIN TRAN;
  DECLARE @RequestId BIGINT;
  SELECT @RequestId=Id FROM dbo.SecurityIngestionRequests WITH(UPDLOCK,HOLDLOCK) WHERE ServiceKeyId=@ServiceKeyId AND IdempotencyKey=@IdempotencyKey;
  IF @RequestId IS NOT NULL
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.SecurityIngestionRequests WHERE Id=@RequestId AND TenantId=@TenantId AND StoreId=@StoreId AND CameraId=@CameraId AND BodyHash=@BodyHash AND NonceHash=@NonceHash) THROW 56006,'Idempotency key replayed with different signed content.',1;
   SELECT o.Id,o.IngestionRequestId,CAST(1 AS BIT) WasDuplicate FROM dbo.SecurityObservations o WHERE o.IngestionRequestId=@RequestId ORDER BY o.Id;COMMIT;RETURN;
  END;
  IF EXISTS(SELECT 1 FROM dbo.SecurityIngestionRequests WITH(UPDLOCK,HOLDLOCK) WHERE ServiceKeyId=@ServiceKeyId AND NonceHash=@NonceHash) THROW 56007,'Signed nonce was already used.',1;
  INSERT dbo.SecurityIngestionRequests(TenantId,StoreId,CameraId,ServiceKeyId,IdempotencyKey,NonceHash,BodyHash,SignedUtc,ReceivedUtc) VALUES(@TenantId,@StoreId,@CameraId,@ServiceKeyId,@IdempotencyKey,@NonceHash,@BodyHash,@SignedUtc,SYSUTCDATETIME());SET @RequestId=SCOPE_IDENTITY();
  INSERT dbo.SecurityObservations(TenantId,StoreId,CameraId,IngestionRequestId,VisitId,PersonTrackSessionId,PersonTrackId,ObservationType,OccurredUtc,ZoneId,ProductId,ProductCategoryId,Confidence,CorrelationId,ModelVersion,MetadataJson) VALUES(@TenantId,@StoreId,@CameraId,@RequestId,@VisitId,@PersonTrackSessionId,@PersonTrackId,@ObservationType,@OccurredUtc,@ZoneId,@ProductId,@ProductCategoryId,@Confidence,@CorrelationId,@ModelVersion,@MetadataJson);
  SELECT Id,IngestionRequestId,CAST(0 AS BIT) WasDuplicate FROM dbo.SecurityObservations WHERE Id=SCOPE_IDENTITY();COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.SecurityRule_List @TenantId BIGINT,@StoreId BIGINT=NULL
AS BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @StoreId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Stores WHERE TenantId=@TenantId AND Id=@StoreId) THROW 56008,'Store is outside the authorized tenant.',1;
 ;WITH Ranked AS(SELECT r.*,ROW_NUMBER() OVER(PARTITION BY RuleCode ORDER BY CASE WHEN StoreId=@StoreId THEN 0 ELSE 1 END,Version DESC) rn FROM dbo.SecurityRules r WHERE TenantId=@TenantId AND (StoreId IS NULL OR StoreId=@StoreId))
 SELECT Id,TenantId,StoreId,RuleCode,Name,IsEnabled,Severity,ConfigurationJson,Version,CreatedByUserId,CreatedUtc,UpdatedUtc,RowVersion FROM Ranked WHERE rn=1 ORDER BY RuleCode;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SecurityRule_CreateVersion @TenantId BIGINT,@StoreId BIGINT=NULL,@RuleCode NVARCHAR(100),@Name NVARCHAR(200),@IsEnabled BIT,@Severity TINYINT,@ConfigurationJson NVARCHAR(4000),@UserId BIGINT,@CorrelationId NVARCHAR(64)
AS BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;BEGIN TRY BEGIN TRAN;
 IF @StoreId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Stores WHERE TenantId=@TenantId AND Id=@StoreId) THROW 56009,'Store is outside the authorized tenant.',1;
 IF @Severity NOT BETWEEN 1 AND 5 OR ISJSON(@ConfigurationJson)<>1 OR DATALENGTH(@ConfigurationJson)>8000 THROW 56012,'Rule configuration is invalid.',1;
 DECLARE @Version INT;SELECT @Version=ISNULL(MAX(Version),0)+1 FROM dbo.SecurityRules WITH(UPDLOCK,HOLDLOCK) WHERE TenantId=@TenantId AND ((StoreId=@StoreId) OR (StoreId IS NULL AND @StoreId IS NULL)) AND RuleCode=@RuleCode;
 INSERT dbo.SecurityRules(TenantId,StoreId,RuleCode,Name,IsEnabled,Severity,ConfigurationJson,Version,CreatedByUserId) VALUES(@TenantId,@StoreId,@RuleCode,@Name,@IsEnabled,@Severity,@ConfigurationJson,@Version,@UserId);
 DECLARE @Id BIGINT=SCOPE_IDENTITY();INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,BeforeJson,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc) VALUES(@TenantId,@StoreId,@UserId,N'User',N'SecurityRule.VersionCreated',N'SecurityRule',CONVERT(NVARCHAR(50),@Id),NULL,JSON_OBJECT(N'RuleCode':@RuleCode,N'Version':@Version,N'Enabled':@IsEnabled),NULL,NULL,@CorrelationId,SYSUTCDATETIME());
 COMMIT;SELECT Id,TenantId,StoreId,RuleCode,Name,IsEnabled,Severity,ConfigurationJson,Version,RowVersion FROM dbo.SecurityRules WHERE Id=@Id;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH END;
GO

CREATE OR ALTER PROCEDURE dbo.SecurityIncident_Search @TenantId BIGINT,@AuthorizedStoreIdsJson NVARCHAR(MAX),@Status TINYINT=NULL,@MinimumSeverity TINYINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@PageNumber INT=1,@PageSize INT=50 AS
BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @TenantId<=0 OR ISJSON(@AuthorizedStoreIdsJson)<>1 OR @PageNumber<1 OR @PageSize NOT BETWEEN 1 AND 200 THROW 56010,'Invalid incident search scope or paging.',1;
 DECLARE @Stores TABLE(Id BIGINT PRIMARY KEY);INSERT @Stores SELECT DISTINCT TRY_CONVERT(BIGINT,[value]) FROM OPENJSON(@AuthorizedStoreIdsJson) WHERE TRY_CONVERT(BIGINT,[value]) IS NOT NULL;
 IF EXISTS(SELECT 1 FROM @Stores a WHERE NOT EXISTS(SELECT 1 FROM dbo.Stores s WHERE s.TenantId=@TenantId AND s.Id=a.Id)) THROW 56011,'An authorized store is outside the tenant.',1;
 SELECT i.Id,i.IncidentNumber,i.StoreId,i.IncidentType,i.Severity,i.RiskScore,i.Status,i.FirstObservedUtc,i.ExitObservedUtc,i.EstimatedLossAmount,i.Currency,i.AssignedUserId,i.ResolutionCode,i.CreatedUtc,i.UpdatedUtc,COUNT_BIG(*) OVER() TotalCount
 FROM dbo.SecurityIncidents i JOIN @Stores a ON a.Id=i.StoreId WHERE i.TenantId=@TenantId AND (@Status IS NULL OR i.Status=@Status) AND (@MinimumSeverity IS NULL OR i.Severity>=@MinimumSeverity) AND (@FromUtc IS NULL OR i.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.CreatedUtc<@ToUtc)
 ORDER BY i.CreatedUtc DESC,i.Id DESC OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SecurityIncident_Get @TenantId BIGINT,@AuthorizedStoreIdsJson NVARCHAR(MAX),@IncidentId BIGINT AS
BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;
 DECLARE @Stores TABLE(Id BIGINT PRIMARY KEY);INSERT @Stores SELECT DISTINCT TRY_CONVERT(BIGINT,[value]) FROM OPENJSON(@AuthorizedStoreIdsJson) WHERE TRY_CONVERT(BIGINT,[value]) IS NOT NULL;
 SELECT i.* FROM dbo.SecurityIncidents i JOIN @Stores a ON a.Id=i.StoreId WHERE i.TenantId=@TenantId AND i.Id=@IncidentId;
 SELECT x.* FROM dbo.SecurityIncidentItems x JOIN @Stores a ON a.Id=x.StoreId WHERE x.TenantId=@TenantId AND x.SecurityIncidentId=@IncidentId ORDER BY x.Id;
 SELECT e.Id,e.SecurityIncidentId,e.EvidenceType,e.CameraId,e.CapturedUtc,e.ContentHash,e.StartUtc,e.EndUtc,e.RetentionUntilUtc,e.IsRestricted,e.DeletedUtc,e.CreatedUtc FROM dbo.SecurityIncidentEvidence e JOIN @Stores a ON a.Id=e.StoreId WHERE e.TenantId=@TenantId AND e.SecurityIncidentId=@IncidentId ORDER BY e.CapturedUtc,e.Id;
 SELECT a.Id,a.ActionType,a.FromStatus,a.ToStatus,a.UserId,a.ActorType,a.ReasonCode,a.Notes,a.OccurredUtc,a.CorrelationId FROM dbo.SecurityIncidentActions a JOIN @Stores s ON s.Id=a.StoreId WHERE a.TenantId=@TenantId AND a.SecurityIncidentId=@IncidentId ORDER BY a.OccurredUtc,a.Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SecurityIncident_Transition @TenantId BIGINT,@StoreId BIGINT,@IncidentId BIGINT,@ToStatus TINYINT,@UserId BIGINT,@ReasonCode NVARCHAR(100)=NULL,@Notes NVARCHAR(2000)=NULL,@ExpectedRowVersion BINARY(8),@CorrelationId NVARCHAR(64) AS
BEGIN SET NOCOUNT ON;SET XACT_ABORT ON;BEGIN TRY BEGIN TRAN;
 DECLARE @From TINYINT;SELECT @From=Status FROM dbo.SecurityIncidents WITH(UPDLOCK,HOLDLOCK) WHERE TenantId=@TenantId AND StoreId=@StoreId AND Id=@IncidentId AND RowVersion=@ExpectedRowVersion;
 IF @From IS NULL THROW 56020,'Incident not found in scope or was modified.',1;
 IF NOT ((@From=3 AND @ToStatus=4) OR (@From IN(3,4) AND @ToStatus=5) OR (@From=5 AND @ToStatus IN(6,7,8)) OR (@From IN(6,7,8) AND @ToStatus=9) OR (@From=4 AND @ToStatus=8)) THROW 56021,'Invalid incident state transition.',1;
 IF @ToStatus IN(6,7) AND NULLIF(LTRIM(RTRIM(@ReasonCode)),N'') IS NULL THROW 56022,'Confirmed loss and false positive require a reason.',1;
 UPDATE dbo.SecurityIncidents SET Status=@ToStatus,ResolutionCode=CASE WHEN @ToStatus IN(6,7,8) THEN @ReasonCode ELSE ResolutionCode END,ResolutionNotes=CASE WHEN @ToStatus IN(6,7,8) THEN @Notes ELSE ResolutionNotes END,ConfirmedByUserId=CASE WHEN @ToStatus=6 THEN @UserId WHEN @ToStatus=9 THEN ConfirmedByUserId ELSE NULL END,ConfirmedUtc=CASE WHEN @ToStatus=6 THEN SYSUTCDATETIME() WHEN @ToStatus=9 THEN ConfirmedUtc ELSE NULL END,UpdatedUtc=SYSUTCDATETIME() WHERE TenantId=@TenantId AND StoreId=@StoreId AND Id=@IncidentId;
 INSERT dbo.SecurityIncidentActions(TenantId,StoreId,SecurityIncidentId,ActionType,FromStatus,ToStatus,UserId,ActorType,ReasonCode,Notes,OccurredUtc,CorrelationId) VALUES(@TenantId,@StoreId,@IncidentId,N'StatusChanged',@From,@ToStatus,@UserId,N'Human',@ReasonCode,@Notes,SYSUTCDATETIME(),@CorrelationId);
 INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,BeforeJson,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc) VALUES(@TenantId,@StoreId,@UserId,N'User',N'SecurityIncident.StatusChanged',N'SecurityIncident',CONVERT(NVARCHAR(50),@IncidentId),JSON_OBJECT(N'Status':@From),JSON_OBJECT(N'Status':@ToStatus,N'ReasonCode':@ReasonCode),NULL,NULL,@CorrelationId,SYSUTCDATETIME());
 COMMIT;SELECT Id,Status,RowVersion,UpdatedUtc FROM dbo.SecurityIncidents WHERE Id=@IncidentId;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.16.0') INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.16.0',N'Phase 18 reviewable tenant/store retail-security foundation',SYSUTCDATETIME(),SUSER_SNAME());
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.16.0')<>1 THROW 56090,'V1.16.0 must exist exactly once.',1;
GO
