/* CustSearch AI Phase 16 - operational settings, audit, worker heartbeat and health. */
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
GO

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL
   OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0')
    THROW 55000,'Phase 15 V1.14.0 must be installed before Phase 16.',1;
GO

/* Values are operational policy only. Secrets belong in a deployment secret provider. */
IF OBJECT_ID(N'dbo.SystemSettings',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
        TenantId BIGINT NULL,
        StoreId BIGINT NULL,
        SettingKey NVARCHAR(100) NOT NULL,
        ValueType TINYINT NOT NULL,
        SettingValue NVARCHAR(1000) NOT NULL,
        Description NVARCHAR(500) NULL,
        UpdatedByUserId BIGINT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SystemSettings_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SystemSettings_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_SystemSettings_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_SystemSettings_Stores FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_SystemSettings_UpdatedBy FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_SystemSettings_Scope CHECK((TenantId IS NULL AND StoreId IS NULL) OR TenantId IS NOT NULL),
        CONSTRAINT CK_SystemSettings_Key CHECK(SettingKey<>N'' AND SettingKey NOT LIKE N'%[^A-Za-z0-9._-]%'),
        CONSTRAINT CK_SystemSettings_ValueType CHECK(ValueType BETWEEN 1 AND 4),
        CONSTRAINT CK_SystemSettings_Value CHECK(
            (ValueType=1 AND SettingValue IN(N'true',N'false')) OR
            (ValueType=2 AND TRY_CONVERT(BIGINT,SettingValue) IS NOT NULL) OR
            (ValueType=3 AND TRY_CONVERT(DECIMAL(19,6),SettingValue) IS NOT NULL) OR
            (ValueType=4 AND DATALENGTH(SettingValue)<=2000))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SystemSettings') AND name=N'UX_SystemSettings_Platform')
    CREATE UNIQUE INDEX UX_SystemSettings_Platform ON dbo.SystemSettings(SettingKey) WHERE TenantId IS NULL AND StoreId IS NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SystemSettings') AND name=N'UX_SystemSettings_Tenant')
    CREATE UNIQUE INDEX UX_SystemSettings_Tenant ON dbo.SystemSettings(TenantId,SettingKey) WHERE TenantId IS NOT NULL AND StoreId IS NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SystemSettings') AND name=N'UX_SystemSettings_Store')
    CREATE UNIQUE INDEX UX_SystemSettings_Store ON dbo.SystemSettings(TenantId,StoreId,SettingKey) WHERE StoreId IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.WorkerHeartbeats',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WorkerHeartbeats
    (
        InstanceId NVARCHAR(100) NOT NULL CONSTRAINT PK_WorkerHeartbeats PRIMARY KEY,
        WorkerName NVARCHAR(100) NOT NULL,
        Status TINYINT NOT NULL,
        StartedUtc DATETIME2(7) NOT NULL,
        LastHeartbeatUtc DATETIME2(7) NOT NULL,
        LastSuccessfulCycleUtc DATETIME2(7) NULL,
        LastErrorUtc DATETIME2(7) NULL,
        LastError NVARCHAR(1000) NULL,
        MetadataJson NVARCHAR(2000) NULL,
        CONSTRAINT CK_WorkerHeartbeats_Status CHECK(Status BETWEEN 1 AND 3),
        CONSTRAINT CK_WorkerHeartbeats_Time CHECK(LastHeartbeatUtc>=StartedUtc AND (LastSuccessfulCycleUtc IS NULL OR LastSuccessfulCycleUtc>=StartedUtc)),
        CONSTRAINT CK_WorkerHeartbeats_Metadata CHECK(MetadataJson IS NULL OR ISJSON(MetadataJson)=1)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.WorkerHeartbeats') AND name=N'IX_WorkerHeartbeats_Health')
    CREATE INDEX IX_WorkerHeartbeats_Health ON dbo.WorkerHeartbeats(LastHeartbeatUtc DESC) INCLUDE(WorkerName,Status,LastSuccessfulCycleUtc,LastErrorUtc);
GO

DECLARE @PlatformPermissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
INSERT @PlatformPermissions VALUES
(N'PlatformSettings.View',N'View platform operational settings.'),
(N'PlatformSettings.Manage',N'Manage platform operational settings.'),
(N'SystemHealth.View',N'View platform operational health.');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 1,p.Name,p.Description,1,SYSUTCDATETIME() FROM @PlatformPermissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);
GO
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p
WHERE r.Scope=1 AND r.IsActive=1 AND p.Scope=1 AND p.IsActive=1
  AND ((r.NormalizedName IN(N'PLATFORMSUPERADMIN',N'PLATFORMOPERATIONSADMIN') AND p.Name IN(N'PlatformSettings.View',N'PlatformSettings.Manage',N'SystemHealth.View'))
    OR (r.NormalizedName IN(N'PLATFORMSUPPORTADMIN',N'PLATFORMAUDITOR') AND p.Name=N'SystemHealth.View'))
  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO

DECLARE @Defaults TABLE(SettingKey NVARCHAR(100),ValueType TINYINT,SettingValue NVARCHAR(1000),Description NVARCHAR(500));
INSERT @Defaults VALUES
(N'RecognitionThreshold',3,N'0.85',N'Minimum recognition confidence.'),(N'ReviewThreshold',3,N'0.70',N'Minimum human-review confidence.'),
(N'FaceQualityThreshold',3,N'0.70',N'Minimum face-template quality.'),(N'PersonDetectionThreshold',3,N'0.60',N'Minimum person-detection confidence.'),
(N'AIProcessingFPS',2,N'5',N'Target AI frames per second.'),(N'SamePersonCooldownSeconds',2,N'30',N'Duplicate event cooldown.'),
(N'PartyDetectionWindowSeconds',2,N'15',N'Visit-party grouping window.'),(N'NotificationCooldownMinutes',2,N'5',N'Notification cooldown.'),
(N'HighValueThreshold',3,N'0',N'Configured factual high-value purchase threshold; zero disables classification.'),(N'SnapshotEnabled',1,N'false',N'Allow evidence snapshots when consent and policy permit.'),
(N'SnapshotRetentionDays',2,N'30',N'Snapshot retention period.'),(N'AnonymousVisitorRetentionDays',2,N'30',N'Anonymous visitor retention period.'),
(N'WebhookRetryCount',2,N'5',N'Maximum webhook attempts.'),(N'WebhookTimeoutSeconds',2,N'30',N'Webhook timeout.'),(N'DemoMode',1,N'false',N'Enable demo workflows outside production.'),
(N'StaffTrackingEnabled',1,N'true',N'Enable disclosed staff tracking.'),(N'StaffZoneTrackingEnabled',1,N'true',N'Enable staff zone tracking.'),
(N'StaffCustomerInteractionTrackingEnabled',1,N'true',N'Enable staff/customer interaction tracking.'),(N'CustomerDwellTrackingEnabled',1,N'true',N'Enable customer dwell tracking.'),
(N'CustomerJourneyTrackingEnabled',1,N'true',N'Enable customer journey tracking.'),(N'StaffAssistedConversionEnabled',1,N'true',N'Enable assisted conversion tracking.'),
(N'AssistedConversionWindowMinutes',2,N'120',N'Assisted conversion window.'),(N'MultiPersonTrackingEnabled',1,N'true',N'Enable multi-person tracking.'),
(N'VisitPartyDetectionEnabled',1,N'true',N'Enable visit-party detection.'),(N'VerifiedHouseholdContextEnabled',1,N'true',N'Allow verified household context.'),
(N'FamilyGroupTrackingEnabled',1,N'true',N'Enable verified family group tracking.'),(N'AutoSuggestFrequentCoVisitorsEnabled',1,N'true',N'Suggest frequent co-visitors for human review.'),
(N'AutoLinkHouseholdFromFaceSimilarity',1,N'false',N'Must remain false; never infer family relationship from facial similarity.'),
(N'VoiceCommandEnabled',1,N'true',N'Enable configured staff voice commands.'),(N'VoiceCommandDefaultLanguageCode',4,N'en-IN',N'Default voice language.'),
(N'VoiceCommandConfirmationMode',4,N'Ambiguous',N'Voice confirmation policy.'),(N'VoiceCommandSessionTimeoutSeconds',2,N'30',N'Voice session timeout.'),
(N'AllowVoiceCategoryCreate',1,N'false',N'Prevent unreviewed category creation from voice.' );
INSERT dbo.SystemSettings(TenantId,StoreId,SettingKey,ValueType,SettingValue,Description,UpdatedByUserId,CreatedUtc,UpdatedUtc)
SELECT NULL,NULL,d.SettingKey,d.ValueType,d.SettingValue,d.Description,NULL,SYSUTCDATETIME(),SYSUTCDATETIME()
FROM @Defaults d WHERE NOT EXISTS(SELECT 1 FROM dbo.SystemSettings s WHERE s.TenantId IS NULL AND s.StoreId IS NULL AND s.SettingKey=d.SettingKey);
GO

CREATE OR ALTER PROCEDURE dbo.SystemSetting_List
    @TenantId BIGINT=NULL,@StoreId BIGINT=NULL,@IncludeInherited BIT=1
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @StoreId IS NOT NULL AND (@TenantId IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Stores WHERE Id=@StoreId AND TenantId=@TenantId))
        THROW 55001,'The store is outside the authorized tenant.',1;
    IF @IncludeInherited=0
    BEGIN
        SELECT Id,TenantId,StoreId,SettingKey,ValueType,SettingValue,Description,UpdatedByUserId,CreatedUtc,UpdatedUtc,
            CASE WHEN StoreId IS NOT NULL THEN N'Store' WHEN TenantId IS NOT NULL THEN N'Tenant' ELSE N'Platform' END SourceScope
        FROM dbo.SystemSettings
        WHERE ((@TenantId IS NULL AND TenantId IS NULL AND StoreId IS NULL) OR
               (@TenantId IS NOT NULL AND TenantId=@TenantId AND ((@StoreId IS NULL AND StoreId IS NULL) OR StoreId=@StoreId)))
        ORDER BY SettingKey; RETURN;
    END;
    ;WITH Candidates AS
    (
        SELECT s.*,CASE WHEN s.StoreId=@StoreId AND @StoreId IS NOT NULL THEN 3 WHEN s.TenantId=@TenantId AND s.StoreId IS NULL AND @TenantId IS NOT NULL THEN 2 ELSE 1 END Priority
        FROM dbo.SystemSettings s
        WHERE (s.TenantId IS NULL AND s.StoreId IS NULL)
           OR (@TenantId IS NOT NULL AND s.TenantId=@TenantId AND s.StoreId IS NULL)
           OR (@StoreId IS NOT NULL AND s.TenantId=@TenantId AND s.StoreId=@StoreId)
    ), Ranked AS
    (
        SELECT *,ROW_NUMBER() OVER(PARTITION BY SettingKey ORDER BY Priority DESC,Id DESC) rn FROM Candidates
    )
    SELECT Id,TenantId,StoreId,SettingKey,ValueType,SettingValue,Description,UpdatedByUserId,CreatedUtc,UpdatedUtc,
        CASE Priority WHEN 3 THEN N'Store' WHEN 2 THEN N'Tenant' ELSE N'Platform' END SourceScope
    FROM Ranked WHERE rn=1 ORDER BY SettingKey;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SystemSetting_Upsert
    @TenantId BIGINT=NULL,@StoreId BIGINT=NULL,@SettingKey NVARCHAR(100),@ValueType TINYINT,@SettingValue NVARCHAR(1000),
    @Description NVARCHAR(500)=NULL,@UpdatedByUserId BIGINT,@IpAddress VARCHAR(64)=NULL,@UserAgent NVARCHAR(500)=NULL,@CorrelationId VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @SettingKey=LTRIM(RTRIM(@SettingKey)); SET @SettingValue=LTRIM(RTRIM(@SettingValue));
    SET @CorrelationId=LTRIM(RTRIM(@CorrelationId));
    IF @SettingKey=N'' OR @SettingKey LIKE N'%[^A-Za-z0-9._-]%' OR @ValueType NOT BETWEEN 1 AND 4 OR NULLIF(@CorrelationId,'') IS NULL
        THROW 55002,'The setting key or type is invalid.',1;
    IF (@ValueType=1 AND @SettingValue NOT IN(N'true',N'false')) OR (@ValueType=2 AND TRY_CONVERT(BIGINT,@SettingValue) IS NULL)
       OR (@ValueType=3 AND TRY_CONVERT(DECIMAL(19,6),@SettingValue) IS NULL) OR (@ValueType=4 AND DATALENGTH(@SettingValue)>2000)
        THROW 55003,'The setting value does not match its declared type.',1;
    IF @TenantId IS NOT NULL AND @SettingKey=N'AutoLinkHouseholdFromFaceSimilarity'
        THROW 55006,'This safety setting is platform-controlled.',1;
    IF @StoreId IS NOT NULL AND (@TenantId IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Stores WHERE Id=@StoreId AND TenantId=@TenantId))
        THROW 55004,'The store is outside the authorized tenant.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Id=@UpdatedByUserId AND IsActive=1 AND
        ((@TenantId IS NULL AND Scope=1 AND TenantId IS NULL) OR (@TenantId IS NOT NULL AND Scope=2 AND TenantId=@TenantId)))
        THROW 55005,'The setting actor is outside the authorized scope.',1;

    BEGIN TRANSACTION;
    DECLARE @Id BIGINT;
    SELECT @Id=Id FROM dbo.SystemSettings WITH(UPDLOCK,HOLDLOCK)
    WHERE SettingKey=@SettingKey AND ((@TenantId IS NULL AND TenantId IS NULL AND StoreId IS NULL)
      OR (TenantId=@TenantId AND ((@StoreId IS NULL AND StoreId IS NULL) OR StoreId=@StoreId)));
    IF @Id IS NULL
    BEGIN
        INSERT dbo.SystemSettings(TenantId,StoreId,SettingKey,ValueType,SettingValue,Description,UpdatedByUserId,CreatedUtc,UpdatedUtc)
        VALUES(@TenantId,@StoreId,@SettingKey,@ValueType,@SettingValue,NULLIF(LTRIM(RTRIM(@Description)),N''),@UpdatedByUserId,SYSUTCDATETIME(),SYSUTCDATETIME());
        SET @Id=SCOPE_IDENTITY();
    END
    ELSE UPDATE dbo.SystemSettings SET ValueType=@ValueType,SettingValue=@SettingValue,
        Description=COALESCE(NULLIF(LTRIM(RTRIM(@Description)),N''),Description),UpdatedByUserId=@UpdatedByUserId,UpdatedUtc=SYSUTCDATETIME() WHERE Id=@Id;
    INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,BeforeJson,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc)
    SELECT @TenantId,@StoreId,@UpdatedByUserId,CASE WHEN @TenantId IS NULL THEN N'PlatformUser' ELSE N'TenantUser' END,N'SystemSettingUpserted',N'SystemSetting',CONVERT(NVARCHAR(30),@Id),NULL,
        (SELECT @SettingKey SettingKey,@ValueType ValueType,@SettingValue SettingValue FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@IpAddress,LEFT(@UserAgent,500),@CorrelationId,SYSUTCDATETIME();
    COMMIT TRANSACTION;
    SELECT Id,TenantId,StoreId,SettingKey,ValueType,SettingValue,Description,UpdatedByUserId,CreatedUtc,UpdatedUtc,
        CASE WHEN StoreId IS NOT NULL THEN N'Store' WHEN TenantId IS NOT NULL THEN N'Tenant' ELSE N'Platform' END SourceScope
    FROM dbo.SystemSettings WHERE Id=@Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.WorkerHeartbeat_Upsert
    @InstanceId NVARCHAR(100),@WorkerName NVARCHAR(100),@Status TINYINT,@StartedUtc DATETIME2(7),
    @LastSuccessfulCycleUtc DATETIME2(7)=NULL,@LastError NVARCHAR(1000)=NULL,@MetadataJson NVARCHAR(2000)=NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    SET @InstanceId=LTRIM(RTRIM(@InstanceId)); SET @WorkerName=LTRIM(RTRIM(@WorkerName));
    IF @InstanceId=N'' OR @WorkerName=N'' OR @Status NOT BETWEEN 1 AND 3 OR @StartedUtc>SYSUTCDATETIME() OR (@MetadataJson IS NOT NULL AND ISJSON(@MetadataJson)<>1)
        THROW 55010,'Worker heartbeat is invalid.',1;
    MERGE dbo.WorkerHeartbeats WITH(HOLDLOCK) target
    USING(SELECT @InstanceId InstanceId) source ON source.InstanceId=target.InstanceId
    WHEN MATCHED THEN UPDATE SET WorkerName=@WorkerName,Status=@Status,LastHeartbeatUtc=SYSUTCDATETIME(),
        LastSuccessfulCycleUtc=COALESCE(@LastSuccessfulCycleUtc,target.LastSuccessfulCycleUtc),LastErrorUtc=CASE WHEN @LastError IS NULL THEN target.LastErrorUtc ELSE SYSUTCDATETIME() END,
        LastError=LEFT(@LastError,1000),MetadataJson=@MetadataJson
    WHEN NOT MATCHED THEN INSERT(InstanceId,WorkerName,Status,StartedUtc,LastHeartbeatUtc,LastSuccessfulCycleUtc,LastErrorUtc,LastError,MetadataJson)
        VALUES(@InstanceId,@WorkerName,@Status,@StartedUtc,SYSUTCDATETIME(),@LastSuccessfulCycleUtc,CASE WHEN @LastError IS NULL THEN NULL ELSE SYSUTCDATETIME() END,LEFT(@LastError,1000),@MetadataJson);
END;
GO

CREATE OR ALTER PROCEDURE dbo.AuditLog_Search
    @TenantId BIGINT=NULL,@AllowedStoreIdsJson NVARCHAR(MAX)=NULL,@TenantWide BIT=0,@StoreId BIGINT=NULL,@Action NVARCHAR(100)=NULL,
    @EntityType NVARCHAR(100)=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@PageNumber INT=1,@PageSize INT=50
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @TenantId IS NOT NULL AND (@AllowedStoreIdsJson IS NULL OR ISJSON(@AllowedStoreIdsJson)<>1) THROW 55020,'Authorized store scope is required.',1;
    IF @PageNumber<1 OR @PageSize<1 OR @PageSize>200 OR (@FromUtc IS NOT NULL AND @ToUtc IS NOT NULL AND @FromUtc>=@ToUtc) THROW 55021,'Audit search parameters are invalid.',1;
    DECLARE @Stores TABLE(Id BIGINT PRIMARY KEY);
    IF @TenantId IS NOT NULL INSERT @Stores SELECT DISTINCT TRY_CONVERT(BIGINT,[value]) FROM OPENJSON(@AllowedStoreIdsJson) WHERE TRY_CONVERT(BIGINT,[value])>0;
    ;WITH Scoped AS
    (
        SELECT a.Id,a.TenantId,a.StoreId,a.UserId,a.ActorType,a.Action,a.EntityType,a.EntityId,a.IpAddress,a.CorrelationId,a.CreatedUtc
        FROM dbo.AuditLogs a
        WHERE (@TenantId IS NULL OR (a.TenantId=@TenantId AND (@TenantWide=1 OR EXISTS(SELECT 1 FROM @Stores s WHERE s.Id=a.StoreId))))
          AND (@StoreId IS NULL OR a.StoreId=@StoreId) AND (@Action IS NULL OR a.Action=@Action) AND (@EntityType IS NULL OR a.EntityType=@EntityType)
          AND (@FromUtc IS NULL OR a.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR a.CreatedUtc<@ToUtc)
    )
    SELECT *,COUNT_BIG(*) OVER() TotalCount FROM Scoped ORDER BY CreatedUtc DESC,Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SystemHealth_Get
    @WorkerWarningSeconds INT=120
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @WorkerWarningSeconds<30 OR @WorkerWarningSeconds>3600 THROW 55030,'Worker warning threshold is invalid.',1;
    SELECT DB_NAME() DatabaseName,CONVERT(NVARCHAR(128),SERVERPROPERTY('ServerName')) ServerName,
        CONVERT(NVARCHAR(128),SERVERPROPERTY('ProductVersion')) ProductVersion,SYSUTCDATETIME() CheckedUtc,N'Healthy' Status;
    SELECT InstanceId,WorkerName,Status,StartedUtc,LastHeartbeatUtc,LastSuccessfulCycleUtc,LastErrorUtc,LastError,
        CASE WHEN Status=2 OR LastHeartbeatUtc<DATEADD(SECOND,-@WorkerWarningSeconds,SYSUTCDATETIME()) THEN N'Offline' WHEN Status=3 OR LastErrorUtc>LastSuccessfulCycleUtc THEN N'Warning' ELSE N'Healthy' END HealthStatus
    FROM dbo.WorkerHeartbeats ORDER BY WorkerName,InstanceId;
    SELECT
      (SELECT COUNT_BIG(*) FROM dbo.ReportExportJobs WHERE Status IN(1,2)) ReportQueueDepth,
      (SELECT COUNT_BIG(*) FROM dbo.IntegrationOutbox WHERE Status IN(1,2)) WebhookQueueDepth,
      (SELECT COUNT_BIG(*) FROM dbo.NotificationOutbox WHERE Status IN(1,2)) NotificationQueueDepth,
      (SELECT COUNT_BIG(*) FROM dbo.ReportExportEvents WHERE DeliveredUtc IS NULL) ReportEventBacklog;
    SELECT COUNT_BIG(*) TotalCameras,COALESCE(SUM(CASE WHEN Status=2 AND IsActive=1 THEN CONVERT(BIGINT,1) ELSE 0 END),0) OnlineCameras,
      COALESCE(SUM(CASE WHEN Status<>2 OR IsActive=0 THEN CONVERT(BIGINT,1) ELSE 0 END),0) NonOnlineCameras FROM dbo.Cameras;
END;
GO

/* Expiry is retryable: the opaque key remains until the Worker confirms file deletion. */
CREATE OR ALTER PROCEDURE dbo.ReportExportJob_Expire @Take INT=100
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @Take<1 SET @Take=1; IF @Take>1000 SET @Take=1000;
    DECLARE @Expired TABLE(Id BIGINT PRIMARY KEY,TenantId BIGINT NULL,RequestedByUserId BIGINT,StorageReference NVARCHAR(500));
    ;WITH Due AS
    (
        SELECT TOP(@Take) * FROM dbo.ReportExportJobs WITH(UPDLOCK,READPAST,ROWLOCK)
        WHERE (Status=3 AND ExpiresUtc<=SYSUTCDATETIME()) OR (Status=5 AND StorageReference IS NOT NULL)
        ORDER BY ExpiresUtc,Id
    )
    UPDATE Due SET Status=5,LeaseToken=NULL
    OUTPUT inserted.Id,inserted.TenantId,inserted.RequestedByUserId,inserted.StorageReference INTO @Expired;
    INSERT dbo.ReportExportEvents(ReportExportJobId,TenantId,RequestedByUserId,EventType,JobStatus,ProgressPercent,CreatedUtc)
    SELECT e.Id,e.TenantId,e.RequestedByUserId,N'ReportExportExpired',5,100,SYSUTCDATETIME() FROM @Expired e
    WHERE NOT EXISTS(SELECT 1 FROM dbo.ReportExportEvents x WHERE x.ReportExportJobId=e.Id AND x.EventType=N'ReportExportExpired' AND x.ProgressPercent=100);
    SELECT Id,StorageReference FROM @Expired WHERE StorageReference IS NOT NULL ORDER BY Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ReportExportJob_ArtifactDeleted @JobId BIGINT,@StorageReference NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @JobId<=0 OR NULLIF(LTRIM(RTRIM(@StorageReference)),N'') IS NULL THROW 55040,'Artifact cleanup acknowledgement is invalid.',1;
    UPDATE dbo.ReportExportJobs SET StorageReference=NULL WHERE Id=@JobId AND Status=5 AND StorageReference=@StorageReference;
    IF @@ROWCOUNT<>1 THROW 55041,'The expired artifact cleanup target is no longer valid.',1;
END;
GO

/* Privacy cleanup is bounded and preserves relational integrity/audit evidence. */
CREATE OR ALTER PROCEDURE dbo.OperationalRetention_Run @BatchSize INT=100,@RecognitionMetadataRetentionDays INT=30
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @BatchSize<1 OR @BatchSize>1000 OR @RecognitionMetadataRetentionDays<0 OR @RecognitionMetadataRetentionDays>3650 THROW 55050,'Retention options are invalid.',1;
    DECLARE @Now DATETIME2(7)=SYSUTCDATETIME(),@TemplatesDisabled INT=0,@TemplatesDeleted INT=0,@VisitorsDeleted INT=0;
    DECLARE @DisabledTemplates TABLE(TenantId BIGINT); DECLARE @DeletedTemplates TABLE(TenantId BIGINT);
    BEGIN TRANSACTION;

    ;WITH Due AS
    (
        SELECT TOP(@BatchSize) t.* FROM dbo.BiometricTemplates t WITH(UPDLOCK,READPAST,ROWLOCK)
        JOIN dbo.CustomerRecognitionConsents c ON c.Id=t.ConsentId AND c.TenantId=t.TenantId
        WHERE t.Status=1 AND c.WithdrawnUtc IS NULL AND c.ExpiresUtc<=@Now
        ORDER BY c.ExpiresUtc,t.Id
    )
    UPDATE Due SET EncryptedTemplate=0x,Nonce=0x,AuthenticationTag=0x,Status=2,DisabledUtc=@Now,RetentionUntilUtc=DATEADD(DAY,@RecognitionMetadataRetentionDays,@Now)
    OUTPUT inserted.TenantId INTO @DisabledTemplates;
    SET @TemplatesDisabled=@@ROWCOUNT;
    ;WITH Due AS
    (
        SELECT TOP(@BatchSize) * FROM dbo.BiometricTemplates WITH(UPDLOCK,READPAST,ROWLOCK)
        WHERE Status=2 AND RetentionUntilUtc<=@Now ORDER BY RetentionUntilUtc,Id
    )
    UPDATE Due SET Status=3,DeletedUtc=@Now OUTPUT inserted.TenantId INTO @DeletedTemplates;
    SET @TemplatesDeleted=@@ROWCOUNT;
    DECLARE @Visitors TABLE(Id BIGINT PRIMARY KEY,TenantId BIGINT,StoreId BIGINT);
    ;WITH EffectiveRetention AS
    (
        SELECT v.Id,v.TenantId,v.StoreId,v.LastSeenUtc,
          COALESCE(TRY_CONVERT(INT,ss.SettingValue),TRY_CONVERT(INT,ts.SettingValue),TRY_CONVERT(INT,ps.SettingValue),30) RetentionDays
        FROM dbo.AnonymousVisitors v
        LEFT JOIN dbo.SystemSettings ss ON ss.TenantId=v.TenantId AND ss.StoreId=v.StoreId AND ss.SettingKey=N'AnonymousVisitorRetentionDays'
        LEFT JOIN dbo.SystemSettings ts ON ts.TenantId=v.TenantId AND ts.StoreId IS NULL AND ts.SettingKey=N'AnonymousVisitorRetentionDays'
        LEFT JOIN dbo.SystemSettings ps ON ps.TenantId IS NULL AND ps.StoreId IS NULL AND ps.SettingKey=N'AnonymousVisitorRetentionDays'
        WHERE v.ConvertedCustomerId IS NULL
    ), Due AS
    (
        SELECT TOP(@BatchSize) * FROM EffectiveRetention
        WHERE RetentionDays BETWEEN 1 AND 3650 AND LastSeenUtc<DATEADD(DAY,-RetentionDays,@Now)
        ORDER BY LastSeenUtc,Id
    )
    INSERT @Visitors SELECT Id,TenantId,StoreId FROM Due;
    DELETE m FROM dbo.VisitPartyMembers m JOIN @Visitors v ON v.Id=m.AnonymousVisitorId AND v.TenantId=m.TenantId AND v.StoreId=m.StoreId;
    DELETE a FROM dbo.AnonymousVisitors a JOIN @Visitors v ON v.Id=a.Id AND v.TenantId=a.TenantId AND v.StoreId=a.StoreId;
    SET @VisitorsDeleted=@@ROWCOUNT;

    INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,BeforeJson,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc)
    SELECT TenantId,StoreId,NULL,N'Worker',N'AnonymousVisitorRetentionDeleted',N'AnonymousVisitor',NULL,NULL,
      CONCAT(N'{"deletedCount":',COUNT_BIG(*),N'}'),NULL,NULL,CONCAT('retention-',CONVERT(VARCHAR(36),NEWID())),@Now
    FROM @Visitors GROUP BY TenantId,StoreId;
    ;WITH Tenants AS(SELECT TenantId FROM @DisabledTemplates UNION SELECT TenantId FROM @DeletedTemplates)
    INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,BeforeJson,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc)
    SELECT x.TenantId,NULL,NULL,N'Worker',N'RecognitionRetentionApplied',N'BiometricTemplate',NULL,NULL,
      CONCAT(N'{"templatesDisabled":',(SELECT COUNT(*) FROM @DisabledTemplates d WHERE d.TenantId=x.TenantId),N',"templatesMarkedDeleted":',(SELECT COUNT(*) FROM @DeletedTemplates d WHERE d.TenantId=x.TenantId),N'}'),
      NULL,NULL,CONCAT('retention-',CONVERT(VARCHAR(36),NEWID())),@Now FROM Tenants x;
    COMMIT TRANSACTION;
    SELECT @TemplatesDisabled TemplatesDisabled,@TemplatesDeleted TemplatesMarkedDeleted,@VisitorsDeleted AnonymousVisitorsDeleted;
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')
    INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
    VALUES(N'V1.15.0',N'Phase 16 operational settings, audit search, worker heartbeat and system health',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')<>1 THROW 55090,'V1.15.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.SystemSettings',N'U') IS NULL OR OBJECT_ID(N'dbo.WorkerHeartbeats',N'U') IS NULL THROW 55091,'Phase 16 operational tables are missing.',1;
IF OBJECT_ID(N'dbo.SystemSetting_List',N'P') IS NULL OR OBJECT_ID(N'dbo.SystemSetting_Upsert',N'P') IS NULL OR OBJECT_ID(N'dbo.WorkerHeartbeat_Upsert',N'P') IS NULL OR OBJECT_ID(N'dbo.AuditLog_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.SystemHealth_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.ReportExportJob_ArtifactDeleted',N'P') IS NULL OR OBJECT_ID(N'dbo.OperationalRetention_Run',N'P') IS NULL THROW 55092,'Phase 16 procedures are incomplete.',1;
GO
