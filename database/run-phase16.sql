/* CustSearch AI Phase 16 — operational settings, worker coordination, health and audited retention. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;SET XACT_ABORT ON;SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;SET CONCAT_NULL_YIELDS_NULL ON;SET ARITHABORT ON;SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0') THROW 55100,'Phase 15 V1.14.0 must be installed before Phase 16.',1;
GO
IF OBJECT_ID(N'dbo.OperationalSettings',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.OperationalSettings(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OperationalSettings PRIMARY KEY,Scope TINYINT NOT NULL,TenantId BIGINT NULL,StoreId BIGINT NULL,[Key] NVARCHAR(120) NOT NULL,ValueJson NVARCHAR(4000) NOT NULL,
  CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_OperationalSettings_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_OperationalSettings_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
  CONSTRAINT CK_OperationalSettings_Scope CHECK((Scope=1 AND TenantId IS NULL AND StoreId IS NULL) OR(Scope=2 AND TenantId IS NOT NULL AND StoreId IS NULL)OR(Scope=3 AND TenantId IS NOT NULL AND StoreId IS NOT NULL)),
  CONSTRAINT CK_OperationalSettings_Json CHECK(ISJSON(ValueJson)=1)
 );
 CREATE UNIQUE INDEX UX_OperationalSettings_ScopeKey ON dbo.OperationalSettings(Scope,TenantId,StoreId,[Key]);
END;
GO
IF OBJECT_ID(N'dbo.OperationalSecretReferences',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.OperationalSecretReferences(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OperationalSecretReferences PRIMARY KEY,Scope TINYINT NOT NULL,TenantId BIGINT NULL,StoreId BIGINT NULL,[Key] NVARCHAR(120) NOT NULL,Reference NVARCHAR(250) NOT NULL,
  CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,
  CONSTRAINT FK_OperationalSecretReferences_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_OperationalSecretReferences_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
  CONSTRAINT CK_OperationalSecretReferences_Scope CHECK((Scope=1 AND TenantId IS NULL AND StoreId IS NULL) OR(Scope=2 AND TenantId IS NOT NULL AND StoreId IS NULL)OR(Scope=3 AND TenantId IS NOT NULL AND StoreId IS NOT NULL))
 );
 CREATE UNIQUE INDEX UX_OperationalSecretReferences_ScopeKey ON dbo.OperationalSecretReferences(Scope,TenantId,StoreId,[Key]);
END;
GO
IF OBJECT_ID(N'dbo.WorkerControls',N'U') IS NULL
 CREATE TABLE dbo.WorkerControls(WorkerType NVARCHAR(80) NOT NULL CONSTRAINT PK_WorkerControls PRIMARY KEY,IsPaused BIT NOT NULL CONSTRAINT DF_WorkerControls_Paused DEFAULT(0),Reason NVARCHAR(500) NULL,UpdatedByUserId BIGINT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,CONSTRAINT FK_WorkerControls_Users FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id));
GO
IF OBJECT_ID(N'dbo.WorkerLeases',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.WorkerLeases(WorkerType NVARCHAR(80) NOT NULL CONSTRAINT PK_WorkerLeases PRIMARY KEY,LeaseId UNIQUEIDENTIFIER NOT NULL,OwnerId NVARCHAR(150) NOT NULL,AcquiredUtc DATETIME2(7) NOT NULL,RenewedUtc DATETIME2(7) NOT NULL,ExpiresUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,CONSTRAINT CK_WorkerLeases_Period CHECK(ExpiresUtc>=RenewedUtc));
 CREATE INDEX IX_WorkerLeases_Expires ON dbo.WorkerLeases(ExpiresUtc);
END;
GO
IF OBJECT_ID(N'dbo.WorkerHeartbeats',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.WorkerHeartbeats(InstanceId NVARCHAR(150) NOT NULL,WorkerType NVARCHAR(80) NOT NULL,StartedUtc DATETIME2(7) NOT NULL,LastHeartbeatUtc DATETIME2(7) NOT NULL,IsReady BIT NOT NULL,LastError NVARCHAR(1000) NULL,CONSTRAINT PK_WorkerHeartbeats PRIMARY KEY(InstanceId,WorkerType));
 CREATE INDEX IX_WorkerHeartbeats_LastHeartbeat ON dbo.WorkerHeartbeats(LastHeartbeatUtc);
END;
GO
/* Preserve and adapt legacy Phase 16 heartbeat rows for the current Worker entity. */
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'WorkerType') IS NULL
 ALTER TABLE dbo.WorkerHeartbeats ADD WorkerType NVARCHAR(80) NOT NULL CONSTRAINT DF_WorkerHeartbeats_WorkerType DEFAULT(N'custsearch-worker') WITH VALUES;
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'IsReady') IS NULL
BEGIN
 ALTER TABLE dbo.WorkerHeartbeats ADD IsReady BIT NOT NULL CONSTRAINT DF_WorkerHeartbeats_IsReady DEFAULT(0) WITH VALUES;
 IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'Status') IS NOT NULL EXEC(N'UPDATE dbo.WorkerHeartbeats SET IsReady=CASE WHEN Status=1 THEN 1 ELSE 0 END;');
END;
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'WorkerName') IS NOT NULL AND EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.WorkerHeartbeats') AND name=N'WorkerName' AND default_object_id=0)
 ALTER TABLE dbo.WorkerHeartbeats ADD CONSTRAINT DF_WorkerHeartbeats_WorkerName DEFAULT(N'CustSearch.Worker') FOR WorkerName;
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'Status') IS NOT NULL AND EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.WorkerHeartbeats') AND name=N'Status' AND default_object_id=0)
 ALTER TABLE dbo.WorkerHeartbeats ADD CONSTRAINT DF_WorkerHeartbeats_StatusCompat DEFAULT(1) FOR Status;
GO
IF OBJECT_ID(N'dbo.RetentionPolicies',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RetentionPolicies(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetentionPolicies PRIMARY KEY,Domain TINYINT NOT NULL,TenantId BIGINT NULL,StoreId BIGINT NULL,RetentionDays INT NOT NULL,Enabled BIT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,
 CONSTRAINT FK_RetentionPolicies_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_RetentionPolicies_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),CONSTRAINT CK_RetentionPolicies_Domain CHECK(Domain BETWEEN 1 AND 7),CONSTRAINT CK_RetentionPolicies_Days CHECK(RetentionDays BETWEEN 1 AND 36500),CONSTRAINT CK_RetentionPolicies_Scope CHECK(StoreId IS NULL OR TenantId IS NOT NULL));
 CREATE UNIQUE INDEX UX_RetentionPolicies_Scope ON dbo.RetentionPolicies(Domain,TenantId,StoreId);
END;
GO
IF OBJECT_ID(N'dbo.RetentionRuns',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.RetentionRuns(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetentionRuns PRIMARY KEY,PolicyId BIGINT NOT NULL,RunId UNIQUEIDENTIFIER NOT NULL,DeletedCount INT NOT NULL CONSTRAINT DF_RetentionRuns_Deleted DEFAULT(0),Status NVARCHAR(30) NOT NULL,Error NVARCHAR(2000) NULL,StartedUtc DATETIME2(7) NOT NULL,CompletedUtc DATETIME2(7) NULL,CONSTRAINT FK_RetentionRuns_Policies FOREIGN KEY(PolicyId) REFERENCES dbo.RetentionPolicies(Id),CONSTRAINT CK_RetentionRuns_Status CHECK(Status IN(N'Processing',N'Completed',N'Failed')));
 CREATE UNIQUE INDEX UX_RetentionRuns_RunId ON dbo.RetentionRuns(RunId);CREATE INDEX IX_RetentionRuns_PolicyStarted ON dbo.RetentionRuns(PolicyId,StartedUtc);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_AuditLogs_ImmutableUpdate ON dbo.AuditLogs INSTEAD OF UPDATE AS THROW 55120,'Audit entries are immutable.',1;
GO
CREATE OR ALTER PROCEDURE dbo.OperationalRetention_Run @PolicyId BIGINT,@BatchSize INT,@UtcNow DATETIME2(7)
AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @BatchSize NOT BETWEEN 1 AND 5000 THROW 55130,'Retention batch size is invalid.',1;
 DECLARE @Domain TINYINT,@TenantId BIGINT,@StoreId BIGINT,@Days INT,@Enabled BIT,@Deleted INT=0,@Cutoff DATETIME2(7);
 SELECT @Domain=Domain,@TenantId=TenantId,@StoreId=StoreId,@Days=RetentionDays,@Enabled=Enabled FROM dbo.RetentionPolicies WHERE Id=@PolicyId;
 IF @Domain IS NULL THROW 55131,'Retention policy was not found.',1;
 IF @Enabled=0 BEGIN SELECT 0;RETURN;END;
 SET @Cutoff=DATEADD(DAY,-@Days,@UtcNow);
 BEGIN TRANSACTION;
 IF @Domain=1
 BEGIN
  SELECT TOP(@BatchSize) Id INTO #AlertTargets FROM dbo.Alerts WHERE CreatedUtc<@Cutoff AND Status IN(4,5) AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId) ORDER BY Id;
  DELETE n FROM dbo.NotificationOutbox n JOIN #AlertTargets t ON t.Id=n.AlertId;
  DELETE r FROM dbo.RealtimeEvents r JOIN #AlertTargets t ON t.Id=r.AlertId;
  DELETE a FROM dbo.Alerts a JOIN #AlertTargets t ON t.Id=a.Id;SET @Deleted=@@ROWCOUNT;
 END
 ELSE IF @Domain=2 BEGIN DELETE TOP(@BatchSize) FROM dbo.IntegrationDeliveryLogs WHERE CreatedUtc<@Cutoff AND(@TenantId IS NULL OR TenantId=@TenantId);SET @Deleted=@@ROWCOUNT;END
 ELSE IF @Domain=3 BEGIN DELETE TOP(@BatchSize) FROM dbo.ExportJobs WHERE Status=5 AND FilePath IS NULL AND ExpiresUtc<@Cutoff AND(@TenantId IS NULL OR TenantId=@TenantId);SET @Deleted=@@ROWCOUNT;END
 ELSE IF @Domain=4 BEGIN DELETE TOP(@BatchSize) FROM dbo.CameraOperationalEvents WHERE ReceivedUtc<@Cutoff AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId);SET @Deleted=@@ROWCOUNT;END
 ELSE IF @Domain=5
 BEGIN
  DELETE TOP(@BatchSize) FROM dbo.RecognitionCandidates WHERE CreatedUtc<@Cutoff AND Status IN(3,4) AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId);SET @Deleted=@@ROWCOUNT;
  DELETE TOP(@BatchSize) FROM dbo.BiometricTemplates WHERE Status=3 AND RetentionUntilUtc<=@UtcNow AND NOT EXISTS(SELECT 1 FROM dbo.RecognitionCandidates c WHERE c.BiometricTemplateId=BiometricTemplates.Id) AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId);SET @Deleted=@Deleted+@@ROWCOUNT;
 END
 ELSE IF @Domain=6 BEGIN DELETE TOP(@BatchSize) FROM dbo.AuditLogs WHERE CreatedUtc<@Cutoff AND(@TenantId IS NULL OR TenantId=@TenantId)AND(@StoreId IS NULL OR StoreId=@StoreId);SET @Deleted=@@ROWCOUNT;END
 ELSE IF @Domain=7 SET @Deleted=0;
 INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,BeforeJson,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc)
 VALUES(@TenantId,@StoreId,NULL,N'Worker',N'RetentionExecuted',N'RetentionPolicy',CONVERT(NVARCHAR(100),@PolicyId),NULL,CONCAT(N'{"deleted":',@Deleted,N',"domain":',@Domain,N'}'),NULL,NULL,CONVERT(NVARCHAR(64),NEWID()),@UtcNow);
 COMMIT TRANSACTION;SELECT @Deleted;
END;
GO
DECLARE @WorkerTypes TABLE(WorkerType NVARCHAR(80));INSERT @WorkerTypes VALUES(N'notifications'),(N'integrations'),(N'exports'),(N'retention'),(N'cctv-operations');
INSERT dbo.WorkerControls(WorkerType,IsPaused,Reason,UpdatedByUserId,UpdatedUtc)SELECT WorkerType,0,NULL,NULL,SYSUTCDATETIME()FROM @WorkerTypes s WHERE NOT EXISTS(SELECT 1 FROM dbo.WorkerControls x WHERE x.WorkerType=s.WorkerType);
GO
DECLARE @Defaults TABLE(Domain TINYINT,Days INT);INSERT @Defaults VALUES(1,365),(2,180),(3,30),(4,30),(5,30),(6,2555),(7,7);
INSERT dbo.RetentionPolicies(Domain,TenantId,StoreId,RetentionDays,Enabled,CreatedUtc,UpdatedUtc)SELECT Domain,NULL,NULL,Days,1,SYSUTCDATETIME(),SYSUTCDATETIME()FROM @Defaults d WHERE NOT EXISTS(SELECT 1 FROM dbo.RetentionPolicies p WHERE p.Domain=d.Domain AND p.TenantId IS NULL AND p.StoreId IS NULL);
GO
DECLARE @Permissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));INSERT @Permissions VALUES(N'PlatformOperations.View',N'View platform health queues settings and retention.'),(N'PlatformOperations.Manage',N'Manage workers settings secret references retention and dead letters.');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)SELECT 1,Name,Description,1,SYSUTCDATETIME()FROM @Permissions p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);
INSERT dbo.RolePermissions(RoleId,PermissionId)SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Name IN(N'PlatformOperations.View',N'PlatformOperations.Manage')WHERE r.Scope=1 AND r.IsActive=1 AND r.NormalizedName IN(N'PLATFORMSUPERADMIN',N'PLATFORMOPERATIONSADMIN')AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)VALUES(N'V1.15.0',N'Phase 16 operational settings worker controls leases health retention and audit hardening',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF(SELECT COUNT(*)FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')<>1 THROW 55190,'V1.15.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.OperationalSettings',N'U')IS NULL OR OBJECT_ID(N'dbo.WorkerLeases',N'U')IS NULL OR OBJECT_ID(N'dbo.RetentionPolicies',N'U')IS NULL OR OBJECT_ID(N'dbo.OperationalRetention_Run',N'P')IS NULL THROW 55191,'Phase 16 operational objects are incomplete.',1;
GO

