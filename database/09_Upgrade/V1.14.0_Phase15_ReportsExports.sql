/* CustSearch AI Phase 15 - tenant/platform reports and asynchronous export jobs. */
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
   OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.13.0')
    THROW 54900,'Phase 14 V1.13.0 must be installed before Phase 15.',1;
GO

/* StorageReference is an opaque application-managed key, never a public URL or client path. */
IF OBJECT_ID(N'dbo.ReportExportJobs',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportExportJobs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReportExportJobs PRIMARY KEY,
        TenantId BIGINT NULL,
        RequestedByUserId BIGINT NOT NULL,
        ReportType NVARCHAR(100) NOT NULL,
        FilterJson NVARCHAR(4000) NOT NULL,
        Format TINYINT NOT NULL,
        Status TINYINT NOT NULL CONSTRAINT DF_ReportExportJobs_Status DEFAULT(1),
        ProgressPercent TINYINT NOT NULL CONSTRAINT DF_ReportExportJobs_Progress DEFAULT(0),
        StorageReference NVARCHAR(500) NULL,
        DownloadFileName NVARCHAR(260) NULL,
        ContentType NVARCHAR(100) NULL,
        ContentLength BIGINT NULL,
        Sha256 CHAR(64) NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        RequestedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ReportExportJobs_RequestedUtc DEFAULT(SYSUTCDATETIME()),
        StartedUtc DATETIME2(7) NULL,
        HeartbeatUtc DATETIME2(7) NULL,
        CompletedUtc DATETIME2(7) NULL,
        ExpiresUtc DATETIME2(7) NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_ReportExportJobs_AttemptCount DEFAULT(0),
        LeaseToken UNIQUEIDENTIFIER NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_ReportExportJobs_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_ReportExportJobs_Users FOREIGN KEY(RequestedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_ReportExportJobs_Scope CHECK(
            (TenantId IS NULL AND ReportType LIKE N'Platform.%') OR
            (TenantId IS NOT NULL AND ReportType LIKE N'Tenant.%')),
        CONSTRAINT CK_ReportExportJobs_FilterJson CHECK(ISJSON(FilterJson)=1 AND DATALENGTH(FilterJson)<=8000),
        CONSTRAINT CK_ReportExportJobs_Format CHECK(Format IN(1,2,3)),
        CONSTRAINT CK_ReportExportJobs_Status CHECK(Status BETWEEN 1 AND 5),
        CONSTRAINT CK_ReportExportJobs_Progress CHECK(ProgressPercent BETWEEN 0 AND 100),
        CONSTRAINT CK_ReportExportJobs_Attempts CHECK(AttemptCount BETWEEN 0 AND 20),
        CONSTRAINT CK_ReportExportJobs_Period CHECK(
            (StartedUtc IS NULL OR StartedUtc>=RequestedUtc) AND
            (HeartbeatUtc IS NULL OR HeartbeatUtc>=RequestedUtc) AND
            (CompletedUtc IS NULL OR CompletedUtc>=RequestedUtc) AND
            (ExpiresUtc IS NULL OR CompletedUtc IS NULL OR ExpiresUtc>CompletedUtc)),
        CONSTRAINT CK_ReportExportJobs_Artifact CHECK(
            (Status=3 AND ProgressPercent=100 AND StorageReference IS NOT NULL AND DownloadFileName IS NOT NULL
                AND ContentType IS NOT NULL AND ContentLength>=0 AND LEN(Sha256)=64 AND CompletedUtc IS NOT NULL AND ExpiresUtc IS NOT NULL)
            OR Status<>3),
        CONSTRAINT CK_ReportExportJobs_Failure CHECK((Status=4 AND ErrorMessage IS NOT NULL AND CompletedUtc IS NOT NULL) OR Status<>4)
    );
END;
GO

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ReportExportJobs') AND name=N'IX_ReportExportJobs_Queue')
    CREATE INDEX IX_ReportExportJobs_Queue ON dbo.ReportExportJobs(Status,RequestedUtc,Id)
    INCLUDE(AttemptCount,HeartbeatUtc) WHERE Status IN(1,2);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ReportExportJobs') AND name=N'IX_ReportExportJobs_Requester')
    CREATE INDEX IX_ReportExportJobs_Requester ON dbo.ReportExportJobs(RequestedByUserId,RequestedUtc DESC,Id DESC)
    INCLUDE(TenantId,ReportType,Format,Status,ProgressPercent,ExpiresUtc);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ReportExportJobs') AND name=N'IX_ReportExportJobs_Tenant_Requester')
    CREATE INDEX IX_ReportExportJobs_Tenant_Requester ON dbo.ReportExportJobs(TenantId,RequestedByUserId,RequestedUtc DESC,Id DESC)
    WHERE TenantId IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ReportExportJobs') AND name=N'IX_ReportExportJobs_Expiry')
    CREATE INDEX IX_ReportExportJobs_Expiry ON dbo.ReportExportJobs(ExpiresUtc,Id)
    INCLUDE(StorageReference,Status) WHERE ExpiresUtc IS NOT NULL;
GO

/* Durable relay from Worker/SQL state changes to the authenticated report SignalR hub. */
IF OBJECT_ID(N'dbo.ReportExportEvents',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportExportEvents
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReportExportEvents PRIMARY KEY,
        ReportExportJobId BIGINT NOT NULL,
        TenantId BIGINT NULL,
        RequestedByUserId BIGINT NOT NULL,
        EventType NVARCHAR(50) NOT NULL,
        JobStatus TINYINT NOT NULL,
        ProgressPercent TINYINT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        ClaimedUtc DATETIME2(7) NULL,
        DeliveredUtc DATETIME2(7) NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_ReportExportEvents_Attempts DEFAULT(0),
        LeaseToken UNIQUEIDENTIFIER NULL,
        LastError NVARCHAR(500) NULL,
        CONSTRAINT FK_ReportExportEvents_Jobs FOREIGN KEY(ReportExportJobId) REFERENCES dbo.ReportExportJobs(Id),
        CONSTRAINT FK_ReportExportEvents_Users FOREIGN KEY(RequestedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_ReportExportEvents_Status CHECK(JobStatus BETWEEN 1 AND 5),
        CONSTRAINT CK_ReportExportEvents_Progress CHECK(ProgressPercent BETWEEN 0 AND 100),
        CONSTRAINT CK_ReportExportEvents_Type CHECK(EventType IN(N'ReportExportQueued',N'ReportExportProgress',N'ReportExportReady',N'ReportExportFailed',N'ReportExportExpired'))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ReportExportEvents') AND name=N'UX_ReportExportEvents_Job_Event_Progress')
    CREATE UNIQUE INDEX UX_ReportExportEvents_Job_Event_Progress ON dbo.ReportExportEvents(ReportExportJobId,EventType,ProgressPercent);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ReportExportEvents') AND name=N'IX_ReportExportEvents_Delivery')
    CREATE INDEX IX_ReportExportEvents_Delivery ON dbo.ReportExportEvents(DeliveredUtc,ClaimedUtc,Id) INCLUDE(AttemptCount) WHERE DeliveredUtc IS NULL;
GO

CREATE OR ALTER PROCEDURE dbo.ReportExportJob_Create
    @TenantId BIGINT=NULL,
    @RequestedByUserId BIGINT,
    @ReportType NVARCHAR(100),
    @FilterJson NVARCHAR(4000),
    @Format TINYINT,
    @IpAddress VARCHAR(64)=NULL,
    @UserAgent NVARCHAR(500)=NULL,
    @CorrelationId VARCHAR(64)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @ReportType=LTRIM(RTRIM(@ReportType));
    SET @CorrelationId=LTRIM(RTRIM(@CorrelationId));
    IF @RequestedByUserId<=0 OR @ReportType=N'' OR ISJSON(@FilterJson)<>1 OR @Format NOT IN(1,2,3) OR NULLIF(@CorrelationId,'') IS NULL
        THROW 54901,'The export request is invalid.',1;

    IF @TenantId IS NULL
    BEGIN
        IF @ReportType NOT LIKE N'Platform.%' OR NOT EXISTS(
            SELECT 1 FROM dbo.Users WHERE Id=@RequestedByUserId AND Scope=1 AND TenantId IS NULL AND IsActive=1)
            THROW 54902,'A valid platform requester is required.',1;
    END
    ELSE IF @ReportType NOT LIKE N'Tenant.%' OR NOT EXISTS(
        SELECT 1 FROM dbo.Users WHERE Id=@RequestedByUserId AND Scope=2 AND TenantId=@TenantId AND IsActive=1)
        THROW 54903,'A valid tenant requester is required.',1;

    DECLARE @RequiredPermission NVARCHAR(150)=CASE @ReportType
      WHEN N'Tenant.DailyVisitors' THEN N'TenantReports.View' WHEN N'Tenant.CurrentVisitors' THEN N'TenantReports.View'
      WHEN N'Tenant.NewCustomers' THEN N'TenantReports.View' WHEN N'Tenant.ReturningCustomers' THEN N'TenantReports.View'
      WHEN N'Tenant.HouseholdVisits' THEN N'Households.View'
      WHEN N'Tenant.RetailSales' THEN N'RetailReports.View' WHEN N'Tenant.StaffPerformance' THEN N'StaffPerformance.View'
      WHEN N'Tenant.RetailInvoices' THEN N'RetailReports.View' WHEN N'Tenant.Payments' THEN N'RetailReports.View'
      WHEN N'Tenant.PersonalSpend' THEN N'RetailReports.View' WHEN N'Tenant.HouseholdSpend' THEN N'RetailReports.View'
      WHEN N'Tenant.ProductSales' THEN N'RetailReports.View' WHEN N'Tenant.ProductCategorySales' THEN N'RetailReports.View'
      WHEN N'Tenant.CustomerPreferences' THEN N'Preferences.View' WHEN N'Tenant.HouseholdPreferences' THEN N'Preferences.View'
      WHEN N'Tenant.CustomerJourneys' THEN N'CustomerJourneys.View'
      WHEN N'Tenant.CustomerDwell' THEN N'DwellAnalytics.View' WHEN N'Tenant.VoiceCommandUsage' THEN N'VoiceCommands.Audit'
      WHEN N'Tenant.FamilyVisitParty' THEN N'VisitParties.View' WHEN N'Tenant.CameraHealth' THEN N'Cameras.View'
      WHEN N'Tenant.Recognition' THEN N'Recognition.View' WHEN N'Tenant.Alerts' THEN N'Alerts.View'
      WHEN N'Tenant.WebhookDelivery' THEN N'Webhooks.View' WHEN N'Tenant.AuditActivity' THEN N'TenantAudit.View' WHEN N'Tenant.UserActivity' THEN N'TenantAudit.View'
      WHEN N'Tenant.IntegrationSync' THEN N'Integrations.View'
      WHEN N'Platform.AuditActivity' THEN N'PlatformAudit.View'
      WHEN N'Platform.TenantOperationalSummary' THEN N'PlatformReports.View' WHEN N'Platform.PlatformBillingInvoices' THEN N'PlatformReports.View' WHEN N'Platform.PaymentCollection' THEN N'PlatformReports.View'
      WHEN N'Platform.SubscriptionExpiry' THEN N'PlatformReports.View' WHEN N'Platform.WebhookFailures' THEN N'PlatformReports.View' END;
    DECLARE @ExportPermission NVARCHAR(150)=CASE WHEN @TenantId IS NULL THEN N'PlatformReports.Export' ELSE N'TenantReports.Export' END;
    IF @RequiredPermission IS NULL THROW 54904,'The report type is not supported.',1;
    IF NOT EXISTS(
        SELECT 1 FROM dbo.UserRoles ur JOIN dbo.Users u ON u.Id=ur.UserId
        JOIN dbo.Roles r ON r.Id=ur.RoleId AND r.IsActive=1 AND r.Scope=u.Scope AND ((r.TenantId IS NULL AND u.TenantId IS NULL) OR r.TenantId=u.TenantId)
        JOIN dbo.RolePermissions rp ON rp.RoleId=r.Id JOIN dbo.Permissions p ON p.Id=rp.PermissionId AND p.IsActive=1
        WHERE ur.UserId=@RequestedByUserId AND p.Name=@ExportPermission)
        THROW 54905,'Report export permission is required.',1;
    IF NOT EXISTS(
        SELECT 1 FROM dbo.UserRoles ur JOIN dbo.Users u ON u.Id=ur.UserId
        JOIN dbo.Roles r ON r.Id=ur.RoleId AND r.IsActive=1 AND r.Scope=u.Scope AND ((r.TenantId IS NULL AND u.TenantId IS NULL) OR r.TenantId=u.TenantId)
        JOIN dbo.RolePermissions rp ON rp.RoleId=r.Id JOIN dbo.Permissions p ON p.Id=rp.PermissionId AND p.IsActive=1
        WHERE ur.UserId=@RequestedByUserId AND p.Name=@RequiredPermission)
        THROW 54906,'The required report permission is missing.',1;

    DECLARE @Id BIGINT,@IsPlatform BIT=CASE WHEN @TenantId IS NULL THEN 1 ELSE 0 END,@AuditTenantId BIGINT=@TenantId,
        @AuditStoreId BIGINT,@AuditAction NVARCHAR(100)=CASE WHEN @TenantId IS NULL THEN N'PlatformReportExportQueued' ELSE N'ReportExportQueued' END,
        @AuditEntityId NVARCHAR(100);
    BEGIN TRY
      BEGIN TRANSACTION;
      INSERT dbo.ReportExportJobs(TenantId,RequestedByUserId,ReportType,FilterJson,Format,Status,ProgressPercent,RequestedUtc)
      VALUES(@TenantId,@RequestedByUserId,@ReportType,@FilterJson,@Format,1,0,SYSUTCDATETIME());
      SET @Id=SCOPE_IDENTITY();
      INSERT dbo.ReportExportEvents(ReportExportJobId,TenantId,RequestedByUserId,EventType,JobStatus,ProgressPercent,CreatedUtc)
      VALUES(@Id,@TenantId,@RequestedByUserId,N'ReportExportQueued',1,0,SYSUTCDATETIME());
      IF @AuditTenantId IS NULL SET @AuditTenantId=TRY_CONVERT(BIGINT,JSON_VALUE(@FilterJson,N'$.tenantId'));
      IF @TenantId IS NOT NULL SET @AuditStoreId=TRY_CONVERT(BIGINT,JSON_VALUE(@FilterJson,N'$.storeId'));
      SET @AuditEntityId=CONVERT(NVARCHAR(100),@Id);
      EXEC dbo.ReportAudit_Write @TenantId=@AuditTenantId,@StoreId=@AuditStoreId,
          @ActorUserId=@RequestedByUserId,@Action=@AuditAction,
          @EntityType=N'ReportExport',@EntityId=@AuditEntityId,@AfterJson=@FilterJson,@IpAddress=@IpAddress,@UserAgent=@UserAgent,@CorrelationId=@CorrelationId;
      COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
      IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
      THROW;
    END CATCH;
    EXEC dbo.ReportExportJob_Get @JobId=@Id,@RequestedByUserId=@RequestedByUserId,@TenantId=@TenantId,@IsPlatform=@IsPlatform;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ReportExportJob_Get
    @JobId BIGINT,
    @RequestedByUserId BIGINT=NULL,
    @TenantId BIGINT=NULL,
    @IsPlatform BIT=0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT Id,TenantId,RequestedByUserId,ReportType,FilterJson,Format,Status,ProgressPercent,
           StorageReference,DownloadFileName,ContentType,ContentLength,Sha256,ErrorMessage,
           RequestedUtc,StartedUtc,HeartbeatUtc,CompletedUtc,ExpiresUtc,AttemptCount,RowVersion
    FROM dbo.ReportExportJobs
    WHERE Id=@JobId
      AND (@RequestedByUserId IS NULL OR RequestedByUserId=@RequestedByUserId)
      AND ((@IsPlatform=1 AND TenantId IS NULL) OR (@IsPlatform=0 AND TenantId=@TenantId));
END;
GO

CREATE OR ALTER PROCEDURE dbo.ReportExportJob_List
    @RequestedByUserId BIGINT,
    @TenantId BIGINT=NULL,
    @IsPlatform BIT=0,
    @Status TINYINT=NULL,
    @Take INT=100
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Take<1 SET @Take=1;
    IF @Take>200 SET @Take=200;
    SELECT TOP(@Take) Id,TenantId,RequestedByUserId,ReportType,Format,Status,ProgressPercent,
           CONVERT(NVARCHAR(500),NULL) StorageReference,DownloadFileName,ContentType,ContentLength,Sha256,ErrorMessage,
           RequestedUtc,StartedUtc,CompletedUtc,ExpiresUtc,AttemptCount
    FROM dbo.ReportExportJobs
    WHERE RequestedByUserId=@RequestedByUserId
      AND ((@IsPlatform=1 AND TenantId IS NULL) OR (@IsPlatform=0 AND TenantId=@TenantId))
      AND (@Status IS NULL OR Status=@Status)
    ORDER BY RequestedUtc DESC,Id DESC;
END;
GO

/* READPAST/UPDLOCK makes claiming safe across multiple Worker instances. Expired leases are reclaimable. */
CREATE OR ALTER PROCEDURE dbo.ReportExportJob_Claim @LeaseSeconds INT=300
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @LeaseSeconds<30 OR @LeaseSeconds>3600 THROW 54904,'LeaseSeconds must be between 30 and 3600.',1;

    DECLARE @Now DATETIME2(7)=SYSUTCDATETIME(),@Lease UNIQUEIDENTIFIER=NEWID();
    ;WITH Candidate AS
    (
        SELECT TOP(1) * FROM dbo.ReportExportJobs WITH(UPDLOCK,READPAST,ROWLOCK)
        WHERE (Status=1 OR (Status=2 AND HeartbeatUtc<DATEADD(SECOND,-@LeaseSeconds,@Now))) AND AttemptCount<5
        ORDER BY RequestedUtc,Id
    )
    UPDATE Candidate SET Status=2,ProgressPercent=CASE WHEN ProgressPercent<1 THEN 1 ELSE ProgressPercent END,
        StartedUtc=COALESCE(StartedUtc,@Now),HeartbeatUtc=@Now,AttemptCount=AttemptCount+1,LeaseToken=@Lease,ErrorMessage=NULL
    OUTPUT inserted.Id,inserted.TenantId,inserted.RequestedByUserId,inserted.ReportType,inserted.FilterJson,
           inserted.Format,inserted.AttemptCount,inserted.LeaseToken,inserted.RequestedUtc;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ReportExportJob_Progress @JobId BIGINT,@LeaseToken UNIQUEIDENTIFIER,@ProgressPercent TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @ProgressPercent<1 OR @ProgressPercent>99 THROW 54905,'Processing progress must be between 1 and 99.',1;
    UPDATE dbo.ReportExportJobs SET ProgressPercent=@ProgressPercent,HeartbeatUtc=SYSUTCDATETIME()
    WHERE Id=@JobId AND Status=2 AND LeaseToken=@LeaseToken;
    IF @@ROWCOUNT<>1 THROW 54906,'The report export lease is no longer valid.',1;
    INSERT dbo.ReportExportEvents(ReportExportJobId,TenantId,RequestedByUserId,EventType,JobStatus,ProgressPercent,CreatedUtc)
    SELECT Id,TenantId,RequestedByUserId,N'ReportExportProgress',Status,ProgressPercent,SYSUTCDATETIME() FROM dbo.ReportExportJobs j
    WHERE Id=@JobId AND NOT EXISTS(SELECT 1 FROM dbo.ReportExportEvents e WHERE e.ReportExportJobId=j.Id AND e.EventType=N'ReportExportProgress' AND e.ProgressPercent=j.ProgressPercent);
END;
GO

CREATE OR ALTER PROCEDURE dbo.ReportExportJob_Complete
    @JobId BIGINT,@LeaseToken UNIQUEIDENTIFIER,@StorageReference NVARCHAR(500),@DownloadFileName NVARCHAR(260),
    @ContentType NVARCHAR(100),@ContentLength BIGINT,@Sha256 CHAR(64),@RetentionHours INT=24
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @RetentionHours<1 OR @RetentionHours>168 OR @ContentLength<0 OR LEN(@Sha256)<>64 OR @Sha256 LIKE '%[^0-9a-f]%'
       OR NULLIF(LTRIM(RTRIM(@StorageReference)),N'') IS NULL OR CHARINDEX(N'..',@StorageReference)>0
        THROW 54907,'The report artifact metadata is invalid.',1;
    DECLARE @Now DATETIME2(7)=SYSUTCDATETIME();
    UPDATE dbo.ReportExportJobs SET Status=3,ProgressPercent=100,StorageReference=@StorageReference,
        DownloadFileName=@DownloadFileName,ContentType=@ContentType,ContentLength=@ContentLength,Sha256=@Sha256,
        ErrorMessage=NULL,HeartbeatUtc=@Now,CompletedUtc=@Now,ExpiresUtc=DATEADD(HOUR,@RetentionHours,@Now),LeaseToken=NULL
    WHERE Id=@JobId AND Status=2 AND LeaseToken=@LeaseToken;
    IF @@ROWCOUNT<>1 THROW 54908,'The report export lease is no longer valid.',1;
    INSERT dbo.ReportExportEvents(ReportExportJobId,TenantId,RequestedByUserId,EventType,JobStatus,ProgressPercent,CreatedUtc)
    SELECT Id,TenantId,RequestedByUserId,N'ReportExportReady',Status,ProgressPercent,@Now FROM dbo.ReportExportJobs j
    WHERE Id=@JobId AND NOT EXISTS(SELECT 1 FROM dbo.ReportExportEvents e WHERE e.ReportExportJobId=j.Id AND e.EventType=N'ReportExportReady' AND e.ProgressPercent=100);
END;
GO

CREATE OR ALTER PROCEDURE dbo.ReportExportJob_Fail @JobId BIGINT,@LeaseToken UNIQUEIDENTIFIER,@ErrorMessage NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@ErrorMessage)),N'') IS NULL THROW 54909,'A safe failure message is required.',1;
    UPDATE dbo.ReportExportJobs SET Status=4,ErrorMessage=LEFT(@ErrorMessage,1000),HeartbeatUtc=SYSUTCDATETIME(),
        CompletedUtc=SYSUTCDATETIME(),LeaseToken=NULL
    WHERE Id=@JobId AND Status=2 AND LeaseToken=@LeaseToken;
    IF @@ROWCOUNT<>1 THROW 54910,'The report export lease is no longer valid.',1;
    INSERT dbo.ReportExportEvents(ReportExportJobId,TenantId,RequestedByUserId,EventType,JobStatus,ProgressPercent,CreatedUtc)
    SELECT Id,TenantId,RequestedByUserId,N'ReportExportFailed',Status,ProgressPercent,SYSUTCDATETIME() FROM dbo.ReportExportJobs j
    WHERE Id=@JobId AND NOT EXISTS(SELECT 1 FROM dbo.ReportExportEvents e WHERE e.ReportExportJobId=j.Id AND e.EventType=N'ReportExportFailed' AND e.ProgressPercent=j.ProgressPercent);
END;
GO

CREATE OR ALTER PROCEDURE dbo.ReportExportJob_Expire @Take INT=100
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Take<1 SET @Take=1;
    IF @Take>1000 SET @Take=1000;
    ;WITH Expired AS
    (
        SELECT TOP(@Take) * FROM dbo.ReportExportJobs WITH(UPDLOCK,READPAST,ROWLOCK)
        WHERE Status=3 AND ExpiresUtc<=SYSUTCDATETIME()
        ORDER BY ExpiresUtc,Id
    )
    UPDATE Expired SET Status=5,StorageReference=NULL,LeaseToken=NULL
    OUTPUT deleted.Id,deleted.StorageReference;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ReportExportEvent_Claim @LeaseSeconds INT=60,@Take INT=50
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @LeaseSeconds<15 OR @LeaseSeconds>600 OR @Take<1 OR @Take>200 THROW 54911,'Event claim options are invalid.',1;
    DECLARE @Now DATETIME2(7)=SYSUTCDATETIME(),@Lease UNIQUEIDENTIFIER=NEWID();
    ;WITH Due AS
    (
        SELECT TOP(@Take) * FROM dbo.ReportExportEvents WITH(UPDLOCK,READPAST,ROWLOCK)
        WHERE DeliveredUtc IS NULL AND (ClaimedUtc IS NULL OR ClaimedUtc<DATEADD(SECOND,-@LeaseSeconds,@Now)) AND AttemptCount<10
        ORDER BY Id
    )
    UPDATE Due SET ClaimedUtc=@Now,LeaseToken=@Lease,AttemptCount=AttemptCount+1
    OUTPUT inserted.Id,inserted.ReportExportJobId,inserted.TenantId,inserted.RequestedByUserId,inserted.EventType,inserted.JobStatus,inserted.ProgressPercent,inserted.CreatedUtc,inserted.LeaseToken;
END;
GO
CREATE OR ALTER PROCEDURE dbo.ReportExportEvent_Complete @EventId BIGINT,@LeaseToken UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE dbo.ReportExportEvents SET DeliveredUtc=SYSUTCDATETIME(),LeaseToken=NULL,LastError=NULL WHERE Id=@EventId AND DeliveredUtc IS NULL AND LeaseToken=@LeaseToken;
    IF @@ROWCOUNT<>1 THROW 54912,'The report export event lease is no longer valid.',1;
END;
GO
CREATE OR ALTER PROCEDURE dbo.ReportExportEvent_Fail @EventId BIGINT,@LeaseToken UNIQUEIDENTIFIER,@ErrorMessage NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE dbo.ReportExportEvents SET ClaimedUtc=NULL,LeaseToken=NULL,LastError=LEFT(COALESCE(NULLIF(@ErrorMessage,N''),N'Delivery failed.'),500) WHERE Id=@EventId AND DeliveredUtc IS NULL AND LeaseToken=@LeaseToken;
    IF @@ROWCOUNT<>1 THROW 54913,'The report export event lease is no longer valid.',1;
END;
GO

/* Worker re-resolves the requester's current role/store authority instead of trusting queued filter JSON. */
CREATE OR ALTER PROCEDURE dbo.ReportExportRequesterScope_Get @TenantId BIGINT=NULL,@RequestedByUserId BIGINT,@ReportType NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @RequiredPermission NVARCHAR(150)=CASE @ReportType
      WHEN N'Tenant.DailyVisitors' THEN N'TenantReports.View' WHEN N'Tenant.CurrentVisitors' THEN N'TenantReports.View'
      WHEN N'Tenant.NewCustomers' THEN N'TenantReports.View' WHEN N'Tenant.ReturningCustomers' THEN N'TenantReports.View'
      WHEN N'Tenant.HouseholdVisits' THEN N'Households.View'
      WHEN N'Tenant.RetailSales' THEN N'RetailReports.View' WHEN N'Tenant.StaffPerformance' THEN N'StaffPerformance.View'
      WHEN N'Tenant.RetailInvoices' THEN N'RetailReports.View' WHEN N'Tenant.Payments' THEN N'RetailReports.View'
      WHEN N'Tenant.PersonalSpend' THEN N'RetailReports.View' WHEN N'Tenant.HouseholdSpend' THEN N'RetailReports.View'
      WHEN N'Tenant.ProductSales' THEN N'RetailReports.View' WHEN N'Tenant.ProductCategorySales' THEN N'RetailReports.View'
      WHEN N'Tenant.CustomerPreferences' THEN N'Preferences.View' WHEN N'Tenant.HouseholdPreferences' THEN N'Preferences.View'
      WHEN N'Tenant.CustomerJourneys' THEN N'CustomerJourneys.View'
      WHEN N'Tenant.CustomerDwell' THEN N'DwellAnalytics.View' WHEN N'Tenant.VoiceCommandUsage' THEN N'VoiceCommands.Audit'
      WHEN N'Tenant.FamilyVisitParty' THEN N'VisitParties.View' WHEN N'Tenant.CameraHealth' THEN N'Cameras.View'
      WHEN N'Tenant.Recognition' THEN N'Recognition.View' WHEN N'Tenant.Alerts' THEN N'Alerts.View'
      WHEN N'Tenant.WebhookDelivery' THEN N'Webhooks.View' WHEN N'Tenant.AuditActivity' THEN N'TenantAudit.View' WHEN N'Tenant.UserActivity' THEN N'TenantAudit.View'
      WHEN N'Tenant.IntegrationSync' THEN N'Integrations.View'
      WHEN N'Platform.AuditActivity' THEN N'PlatformAudit.View'
      WHEN N'Platform.TenantOperationalSummary' THEN N'PlatformReports.View' WHEN N'Platform.PlatformBillingInvoices' THEN N'PlatformReports.View' WHEN N'Platform.PaymentCollection' THEN N'PlatformReports.View'
      WHEN N'Platform.SubscriptionExpiry' THEN N'PlatformReports.View' WHEN N'Platform.WebhookFailures' THEN N'PlatformReports.View' END;
    IF @RequiredPermission IS NULL RETURN;
    DECLARE @ExportPermission NVARCHAR(150)=CASE WHEN @TenantId IS NULL THEN N'PlatformReports.Export' ELSE N'TenantReports.Export' END;
    IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Id=@RequestedByUserId AND ((@TenantId IS NULL AND TenantId IS NULL) OR TenantId=@TenantId) AND Scope=CASE WHEN @TenantId IS NULL THEN 1 ELSE 2 END AND IsActive=1) RETURN;
    IF NOT EXISTS(SELECT 1 FROM dbo.UserRoles ur JOIN dbo.Users u ON u.Id=ur.UserId JOIN dbo.Roles r ON r.Id=ur.RoleId AND r.IsActive=1 AND r.Scope=u.Scope AND ((r.TenantId IS NULL AND u.TenantId IS NULL) OR r.TenantId=u.TenantId) JOIN dbo.RolePermissions rp ON rp.RoleId=r.Id JOIN dbo.Permissions p ON p.Id=rp.PermissionId AND p.IsActive=1 WHERE ur.UserId=@RequestedByUserId AND p.Name=@ExportPermission) RETURN;
    IF NOT EXISTS(SELECT 1 FROM dbo.UserRoles ur JOIN dbo.Users u ON u.Id=ur.UserId JOIN dbo.Roles r ON r.Id=ur.RoleId AND r.IsActive=1 AND r.Scope=u.Scope AND ((r.TenantId IS NULL AND u.TenantId IS NULL) OR r.TenantId=u.TenantId) JOIN dbo.RolePermissions rp ON rp.RoleId=r.Id JOIN dbo.Permissions p ON p.Id=rp.PermissionId AND p.IsActive=1 WHERE ur.UserId=@RequestedByUserId AND p.Name=@RequiredPermission) RETURN;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(
        SELECT 1 FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.Id=ur.RoleId
        WHERE ur.UserId=@RequestedByUserId AND r.TenantId=@TenantId AND r.IsActive=1
          AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER')) THEN 1 ELSE 0 END) TenantWide;
    SELECT usa.StoreId FROM dbo.UserStoreAssignments usa JOIN dbo.Stores s ON s.Id=usa.StoreId AND s.TenantId=usa.TenantId
    WHERE usa.TenantId=@TenantId AND usa.UserId=@RequestedByUserId AND s.IsActive=1 ORDER BY usa.StoreId;
END;
GO

/* Append-only access evidence. Platform actions retain the selected target tenant when one exists. */
CREATE OR ALTER PROCEDURE dbo.ReportAudit_Write
    @TenantId BIGINT=NULL,@StoreId BIGINT=NULL,@ActorUserId BIGINT,@Action NVARCHAR(100),
    @EntityType NVARCHAR(100),@EntityId NVARCHAR(100)=NULL,@AfterJson NVARCHAR(4000),
    @IpAddress VARCHAR(64)=NULL,@UserAgent NVARCHAR(500)=NULL,@CorrelationId VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Action=LTRIM(RTRIM(@Action));SET @EntityType=LTRIM(RTRIM(@EntityType));SET @CorrelationId=LTRIM(RTRIM(@CorrelationId));
    IF @Action NOT IN(N'ReportPreviewed',N'PlatformReportPreviewed',N'ReportExportQueued',N'PlatformReportExportQueued',N'ReportExportDownloaded',N'PlatformReportExportDownloaded')
       OR @EntityType<>N'ReportExport' OR @CorrelationId='' OR ISJSON(@AfterJson)<>1 THROW 54914,'The report audit request is invalid.',1;
    IF @TenantId IS NULL AND @Action=N'PlatformReportExportDownloaded' AND TRY_CONVERT(BIGINT,@EntityId)>0
        SELECT @TenantId=TRY_CONVERT(BIGINT,JSON_VALUE(FilterJson,N'$.tenantId')) FROM dbo.ReportExportJobs WHERE Id=TRY_CONVERT(BIGINT,@EntityId) AND RequestedByUserId=@ActorUserId AND TenantId IS NULL;
    IF @TenantId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Tenants WHERE Id=@TenantId) THROW 54915,'The report audit tenant is invalid.',1;
    IF @StoreId IS NOT NULL AND (@TenantId IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Stores WHERE Id=@StoreId AND TenantId=@TenantId)) THROW 54916,'The report audit store is invalid.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Id=@ActorUserId AND IsActive=1 AND ((Scope=1 AND TenantId IS NULL) OR (Scope=2 AND TenantId=@TenantId))) THROW 54917,'The report audit actor is outside the requested scope.',1;
    INSERT dbo.AuditLogs(TenantId,StoreId,UserId,ActorType,Action,EntityType,EntityId,BeforeJson,AfterJson,IpAddress,UserAgent,CorrelationId,CreatedUtc)
    VALUES(@TenantId,@StoreId,@ActorUserId,CASE WHEN EXISTS(SELECT 1 FROM dbo.Users WHERE Id=@ActorUserId AND Scope=1) THEN N'PlatformUser' ELSE N'TenantUser' END,@Action,@EntityType,NULLIF(@EntityId,N''),NULL,@AfterJson,NULLIF(@IpAddress,''),NULLIF(@UserAgent,N''),@CorrelationId,SYSUTCDATETIME());
END;
GO

/* Static allow-listed report branches prevent caller-controlled SQL object or column names. */
CREATE OR ALTER PROCEDURE dbo.TenantReport_Get
    @TenantId BIGINT,
    @AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,
    @ReportType NVARCHAR(100),
    @StoreId BIGINT=NULL,
    @FromUtc DATETIME2(7)=NULL,
    @ToUtc DATETIME2(7)=NULL,
    @Take INT=10000
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @TenantId<=0 OR @ReportType NOT LIKE N'Tenant.%' THROW 54920,'A valid tenant report is required.',1;
    IF @FromUtc IS NOT NULL AND @ToUtc IS NOT NULL AND @FromUtc>=@ToUtc THROW 54921,'FromUtc must be earlier than ToUtc.',1;
    IF @Take<1 SET @Take=1;
    IF @Take>10000 SET @Take=10000;
    IF @StoreId IS NOT NULL AND @AllowedStoreIdsCsv IS NOT NULL
       AND NOT EXISTS(SELECT 1 FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value)=@StoreId)
        THROW 54922,'The requested store is outside the authorized scope.',1;

    IF @ReportType=N'Tenant.DailyVisitors'
    BEGIN
        SELECT TOP(@Take) CONVERT(date,v.EnteredUtc) VisitDate,v.StoreId,s.StoreName,
               COUNT_BIG(*) VisitCount,COUNT_BIG(DISTINCT v.CustomerId) CustomerCount,
               SUM(CASE WHEN v.ExitedUtc IS NULL THEN 1 ELSE 0 END) CurrentVisitorCount
        FROM dbo.CustomerVisits v JOIN dbo.Stores s ON s.TenantId=v.TenantId AND s.Id=v.StoreId
        WHERE v.TenantId=@TenantId AND (@StoreId IS NULL OR v.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR v.EnteredUtc>=@FromUtc) AND (@ToUtc IS NULL OR v.EnteredUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        GROUP BY CONVERT(date,v.EnteredUtc),v.StoreId,s.StoreName ORDER BY VisitDate DESC,v.StoreId;
        RETURN;
    END;
    IF @ReportType=N'Tenant.CurrentVisitors'
    BEGIN
        SELECT TOP(@Take) v.Id VisitId,v.StoreId,s.StoreName,v.CustomerId,c.CustomerCode,
               CONCAT(c.FirstName,CASE WHEN c.LastName IS NULL THEN N'' ELSE N' '+c.LastName END) CustomerName,v.EnteredUtc,v.Source,v.Status
        FROM dbo.CustomerVisits v JOIN dbo.Stores s ON s.TenantId=v.TenantId AND s.Id=v.StoreId
        JOIN dbo.Customers c ON c.TenantId=v.TenantId AND c.Id=v.CustomerId
        WHERE v.TenantId=@TenantId AND v.ExitedUtc IS NULL AND (@StoreId IS NULL OR v.StoreId=@StoreId)
          AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY v.EnteredUtc DESC,v.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Tenant.NewCustomers'
    BEGIN
        SELECT DISTINCT TOP(@Take) c.Id CustomerId,c.CustomerCode,c.FirstName,c.LastName,c.Mobile,c.Email,c.IsActive,c.CreatedUtc
        FROM dbo.Customers c LEFT JOIN dbo.CustomerStoreAssignments a ON a.TenantId=c.TenantId AND a.CustomerId=c.Id
        WHERE c.TenantId=@TenantId AND (@StoreId IS NULL OR a.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR c.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR c.CreatedUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR a.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY c.CreatedUtc DESC,c.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Tenant.ReturningCustomers'
    BEGIN
        SELECT TOP(@Take) c.Id CustomerId,c.CustomerCode,c.FirstName,c.LastName,COUNT_BIG(*) VisitCount,MIN(v.EnteredUtc) FirstVisitUtc,MAX(v.EnteredUtc) LatestVisitUtc
        FROM dbo.CustomerVisits v JOIN dbo.Customers c ON c.TenantId=v.TenantId AND c.Id=v.CustomerId
        WHERE v.TenantId=@TenantId AND (@StoreId IS NULL OR v.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR v.EnteredUtc>=@FromUtc) AND (@ToUtc IS NULL OR v.EnteredUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        GROUP BY c.Id,c.CustomerCode,c.FirstName,c.LastName HAVING COUNT_BIG(*)>=2 ORDER BY LatestVisitUtc DESC,c.Id;
        RETURN;
    END;
    IF @ReportType=N'Tenant.HouseholdVisits'
    BEGIN
        SELECT TOP(@Take) h.Id HouseholdId,h.HouseholdCode,h.Name HouseholdName,v.Id VisitId,v.VisitCode,v.StoreId,s.StoreName,v.CustomerId,v.EnteredUtc,v.ExitedUtc
        FROM dbo.Households h JOIN dbo.HouseholdMembers hm ON hm.TenantId=h.TenantId AND hm.HouseholdId=h.Id AND hm.IsActive=1 AND hm.IsVerified=1
        JOIN dbo.CustomerVisits v ON v.TenantId=hm.TenantId AND v.CustomerId=hm.CustomerId JOIN dbo.Stores s ON s.TenantId=v.TenantId AND s.Id=v.StoreId
        WHERE h.TenantId=@TenantId AND (@StoreId IS NULL OR v.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR v.EnteredUtc>=@FromUtc) AND (@ToUtc IS NULL OR v.EnteredUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY v.EnteredUtc DESC,v.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Tenant.RetailSales'
    BEGIN
        SELECT TOP(@Take) CONVERT(date,i.InvoiceUtc) SalesDate,i.StoreId,s.StoreName,COUNT_BIG(*) InvoiceCount,
               SUM(i.GrandTotal) NetSales,SUM(i.PaidAmount) PaidAmount,SUM(i.BalanceAmount) OutstandingAmount
        FROM dbo.RetailInvoices i JOIN dbo.Stores s ON s.TenantId=i.TenantId AND s.Id=i.StoreId
        WHERE i.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@StoreId IS NULL OR i.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        GROUP BY CONVERT(date,i.InvoiceUtc),i.StoreId,s.StoreName ORDER BY SalesDate DESC,i.StoreId;
        RETURN;
    END;
    IF @ReportType=N'Tenant.RetailInvoices'
    BEGIN
        SELECT TOP(@Take) i.Id InvoiceId,i.InvoiceNumber,i.StoreId,s.StoreName,i.CustomerId,i.HouseholdId,i.InvoiceUtc,i.Subtotal,i.DiscountAmount,i.TaxAmount,i.GrandTotal,i.PaidAmount,i.BalanceAmount,i.Status
        FROM dbo.RetailInvoices i JOIN dbo.Stores s ON s.TenantId=i.TenantId AND s.Id=i.StoreId
        WHERE i.TenantId=@TenantId AND (@StoreId IS NULL OR i.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY i.InvoiceUtc DESC,i.Id DESC;RETURN;
    END;
    IF @ReportType=N'Tenant.Payments'
    BEGIN
        SELECT TOP(@Take) p.Id PaymentId,p.StoreId,s.StoreName,p.InvoiceId,i.InvoiceNumber,p.PaymentReference,p.PaymentMethod,p.Amount,p.PaymentUtc,p.Status,p.ExternalTransactionId
        FROM dbo.RetailInvoicePayments p JOIN dbo.Stores s ON s.TenantId=p.TenantId AND s.Id=p.StoreId JOIN dbo.RetailInvoices i ON i.TenantId=p.TenantId AND i.Id=p.InvoiceId
        WHERE p.TenantId=@TenantId AND (@StoreId IS NULL OR p.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR p.PaymentUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.PaymentUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY p.PaymentUtc DESC,p.Id DESC;RETURN;
    END;
    IF @ReportType=N'Tenant.PersonalSpend'
    BEGIN
        SELECT TOP(@Take) a.CustomerId,c.CustomerCode,c.FirstName,c.LastName,COUNT_BIG(DISTINCT a.InvoiceId) InvoiceCount,SUM(a.AmountAttributed) AttributedSpend
        FROM dbo.RetailInvoiceItemAttributions a JOIN dbo.RetailInvoices i ON i.TenantId=a.TenantId AND i.Id=a.InvoiceId AND i.Status IN(2,3,4)
        JOIN dbo.Customers c ON c.TenantId=a.TenantId AND c.Id=a.CustomerId
        WHERE a.TenantId=@TenantId AND (@StoreId IS NULL OR i.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        GROUP BY a.CustomerId,c.CustomerCode,c.FirstName,c.LastName ORDER BY AttributedSpend DESC,a.CustomerId;RETURN;
    END;
    IF @ReportType=N'Tenant.HouseholdSpend'
    BEGIN
        SELECT TOP(@Take) h.Id HouseholdId,h.HouseholdCode,h.Name HouseholdName,COUNT_BIG(i.Id) InvoiceCount,SUM(i.GrandTotal) HouseholdSpend
        FROM dbo.Households h JOIN dbo.RetailInvoices i ON i.TenantId=h.TenantId AND i.HouseholdId=h.Id AND i.Status IN(2,3,4)
        WHERE h.TenantId=@TenantId AND (@StoreId IS NULL OR i.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        GROUP BY h.Id,h.HouseholdCode,h.Name ORDER BY HouseholdSpend DESC,h.Id;RETURN;
    END;
    IF @ReportType IN(N'Tenant.ProductSales',N'Tenant.ProductCategorySales')
    BEGIN
        IF @ReportType=N'Tenant.ProductSales'
            SELECT TOP(@Take) x.ProductId,x.ProductCodeSnapshot ProductCode,x.ProductNameSnapshot ProductName,SUM(x.Quantity) Quantity,SUM(x.LineTotal) SalesTotal
            FROM dbo.RetailInvoiceItems x JOIN dbo.RetailInvoices i ON i.TenantId=x.TenantId AND i.Id=x.InvoiceId AND i.Status IN(2,3,4)
            WHERE x.TenantId=@TenantId AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)
              AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
            GROUP BY x.ProductId,x.ProductCodeSnapshot,x.ProductNameSnapshot ORDER BY SalesTotal DESC,x.ProductId;
        ELSE
            SELECT TOP(@Take) x.CategoryId,x.CategoryNameSnapshot CategoryName,SUM(x.Quantity) Quantity,SUM(x.LineTotal) SalesTotal
            FROM dbo.RetailInvoiceItems x JOIN dbo.RetailInvoices i ON i.TenantId=x.TenantId AND i.Id=x.InvoiceId AND i.Status IN(2,3,4)
            WHERE x.TenantId=@TenantId AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)
              AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
            GROUP BY x.CategoryId,x.CategoryNameSnapshot ORDER BY SalesTotal DESC,x.CategoryId;
        RETURN;
    END;
    IF @ReportType=N'Tenant.StaffPerformance'
    BEGIN
        SELECT TOP(@Take) sp.Id StaffProfileId,sp.EmployeeCode,CONCAT(sp.FirstName,N' ',sp.LastName) StaffName,
               ss.StoreId,s.StoreName,COUNT_BIG(ss.Id) ShiftCount,
               SUM(CASE WHEN ss.ActualEndsUtc IS NOT NULL THEN DATEDIFF(MINUTE,ss.StartsUtc,ss.ActualEndsUtc) ELSE 0 END) CompletedMinutes
        FROM dbo.StaffProfiles sp LEFT JOIN dbo.StaffShifts ss ON ss.TenantId=sp.TenantId AND ss.StaffProfileId=sp.Id
          AND (@StoreId IS NULL OR ss.StoreId=@StoreId) AND (@FromUtc IS NULL OR ss.StartsUtc>=@FromUtc) AND (@ToUtc IS NULL OR ss.StartsUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR ss.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        LEFT JOIN dbo.Stores s ON s.TenantId=ss.TenantId AND s.Id=ss.StoreId
        WHERE sp.TenantId=@TenantId AND (@AllowedStoreIdsCsv IS NULL OR ss.Id IS NOT NULL)
        GROUP BY sp.Id,sp.EmployeeCode,sp.FirstName,sp.LastName,ss.StoreId,s.StoreName ORDER BY StaffName,ss.StoreId;
        RETURN;
    END;
    IF @ReportType=N'Tenant.CustomerDwell'
    BEGIN
        SELECT TOP(@Take) p.Id TrackId,p.StoreId,s.StoreName,p.CameraId,c.Name CameraName,p.CustomerId,
               p.StartUtc,p.EndUtc,CASE WHEN p.EndUtc IS NULL THEN NULL ELSE DATEDIFF(SECOND,p.StartUtc,p.EndUtc) END DwellSeconds,p.Confidence
        FROM dbo.PersonTrackSessions p JOIN dbo.Stores s ON s.TenantId=p.TenantId AND s.Id=p.StoreId
        JOIN dbo.Cameras c ON c.TenantId=p.TenantId AND c.StoreId=p.StoreId AND c.Id=p.CameraId
        WHERE p.TenantId=@TenantId AND p.SubjectKind=2 AND (@StoreId IS NULL OR p.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR p.StartUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.StartUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY p.StartUtc DESC,p.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Tenant.CustomerJourneys'
    BEGIN
        SELECT TOP(@Take) v.Id VisitId,v.VisitCode,v.StoreId,s.StoreName,v.CustomerId,c.CustomerCode,v.EnteredUtc,v.ExitedUtc,
               COUNT_BIG(i.Id) InvoiceCount,COALESCE(SUM(CASE WHEN i.Status IN(2,3,4) THEN i.GrandTotal ELSE 0 END),0) FinalizedSales
        FROM dbo.CustomerVisits v JOIN dbo.Stores s ON s.TenantId=v.TenantId AND s.Id=v.StoreId JOIN dbo.Customers c ON c.TenantId=v.TenantId AND c.Id=v.CustomerId
        LEFT JOIN dbo.RetailInvoices i ON i.TenantId=v.TenantId AND i.StoreId=v.StoreId AND i.CustomerVisitId=v.Id
        WHERE v.TenantId=@TenantId AND (@StoreId IS NULL OR v.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR v.EnteredUtc>=@FromUtc) AND (@ToUtc IS NULL OR v.EnteredUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        GROUP BY v.Id,v.VisitCode,v.StoreId,s.StoreName,v.CustomerId,c.CustomerCode,v.EnteredUtc,v.ExitedUtc ORDER BY v.EnteredUtc DESC,v.Id DESC;RETURN;
    END;
    IF @ReportType=N'Tenant.CustomerPreferences'
    BEGIN
        SELECT TOP(@Take) p.Id,p.CustomerId,c.CustomerCode,p.PreferenceType,p.ReferenceId,p.Value,p.Score,p.WeightVersionId,p.CalculatedUtc
        FROM dbo.CustomerPreferenceScores p JOIN dbo.Customers c ON c.TenantId=p.TenantId AND c.Id=p.CustomerId
        WHERE p.TenantId=@TenantId AND (@FromUtc IS NULL OR p.CalculatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.CalculatedUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments a WHERE a.TenantId=p.TenantId AND a.CustomerId=p.CustomerId AND a.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)))
        ORDER BY p.CalculatedUtc DESC,p.Id DESC;RETURN;
    END;
    IF @ReportType=N'Tenant.HouseholdPreferences'
    BEGIN
        SELECT TOP(@Take) p.Id,p.HouseholdId,h.HouseholdCode,h.Name HouseholdName,p.PreferenceType,p.ReferenceId,p.Value,p.Source,p.Reason,p.CreatedUtc
        FROM dbo.HouseholdPreferenceTags p JOIN dbo.Households h ON h.TenantId=p.TenantId AND h.Id=p.HouseholdId
        WHERE p.TenantId=@TenantId AND p.IsActive=1 AND (@FromUtc IS NULL OR p.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.CreatedUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.HouseholdMembers hm JOIN dbo.CustomerStoreAssignments a ON a.TenantId=hm.TenantId AND a.CustomerId=hm.CustomerId WHERE hm.TenantId=p.TenantId AND hm.HouseholdId=p.HouseholdId AND hm.IsActive=1 AND a.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)))
        ORDER BY p.CreatedUtc DESC,p.Id DESC;RETURN;
    END;
    IF @ReportType=N'Tenant.VoiceCommandUsage'
    BEGIN
        SELECT TOP(@Take) v.Id,v.StoreId,s.StoreName,v.StaffUserId,v.CustomerId,v.MatchedTrigger,
               v.RecognitionConfidence,v.ConfirmationRequired,v.Status,v.CreatedUtc,v.ResolvedUtc
        FROM dbo.VoiceCommandSessions v JOIN dbo.Stores s ON s.TenantId=v.TenantId AND s.Id=v.StoreId
        WHERE v.TenantId=@TenantId AND (@StoreId IS NULL OR v.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR v.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR v.CreatedUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY v.CreatedUtc DESC,v.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Tenant.FamilyVisitParty'
    BEGIN
        SELECT TOP(@Take) p.Id VisitPartyId,p.PartyCode,p.StoreId,s.StoreName,p.StartedUtc,p.EndedUtc,p.Source,p.Status,
               COUNT_BIG(m.Id) MemberCount,SUM(CASE WHEN m.CustomerId IS NOT NULL THEN 1 ELSE 0 END) KnownCustomerCount
        FROM dbo.VisitParties p JOIN dbo.Stores s ON s.TenantId=p.TenantId AND s.Id=p.StoreId
        LEFT JOIN dbo.VisitPartyMembers m ON m.TenantId=p.TenantId AND m.StoreId=p.StoreId AND m.VisitPartyId=p.Id
        WHERE p.TenantId=@TenantId AND (@StoreId IS NULL OR p.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR p.StartedUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.StartedUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        GROUP BY p.Id,p.PartyCode,p.StoreId,s.StoreName,p.StartedUtc,p.EndedUtc,p.Source,p.Status
        ORDER BY p.StartedUtc DESC,p.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Tenant.CameraHealth'
    BEGIN
        SELECT TOP(@Take) c.Id CameraId,c.StoreId,s.StoreName,c.CameraCode,c.Name,c.Status,c.IsActive,c.LastHeartbeatUtc,
               DATEDIFF(MINUTE,c.LastHeartbeatUtc,SYSUTCDATETIME()) MinutesSinceHeartbeat
        FROM dbo.Cameras c JOIN dbo.Stores s ON s.TenantId=c.TenantId AND s.Id=c.StoreId
        WHERE c.TenantId=@TenantId AND (@StoreId IS NULL OR c.StoreId=@StoreId)
          AND (@AllowedStoreIdsCsv IS NULL OR c.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY c.StoreId,c.CameraCode;
        RETURN;
    END;
    IF @ReportType=N'Tenant.Recognition'
    BEGIN
        SELECT TOP(@Take) r.Id,r.StoreId,s.StoreName,r.CustomerId,r.Confidence,r.Quality,r.Status,r.CreatedUtc,r.ReviewedUtc,r.ReviewedByUserId
        FROM dbo.RecognitionCandidates r JOIN dbo.Stores s ON s.TenantId=r.TenantId AND s.Id=r.StoreId
        WHERE r.TenantId=@TenantId AND (@StoreId IS NULL OR r.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR r.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR r.CreatedUtc<@ToUtc)
          AND (@AllowedStoreIdsCsv IS NULL OR r.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY r.CreatedUtc DESC,r.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Tenant.Alerts'
    BEGIN
        SELECT TOP(@Take) a.Id,a.StoreId,s.StoreName,a.AlertType,a.Severity,a.Title,a.Status,a.CreatedUtc,a.AcknowledgedUtc,a.ResolvedUtc
        FROM dbo.Alerts a LEFT JOIN dbo.Stores s ON s.TenantId=a.TenantId AND s.Id=a.StoreId
        WHERE a.TenantId=@TenantId AND (@StoreId IS NULL OR a.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR a.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR a.CreatedUtc<@ToUtc)
          AND (a.StoreId IS NULL OR @AllowedStoreIdsCsv IS NULL OR a.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY a.CreatedUtc DESC,a.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Tenant.WebhookDelivery'
    BEGIN
        SELECT TOP(@Take) l.Id,l.IntegrationConfigurationId,l.Provider,l.Direction,l.Status,l.DurationMilliseconds,l.HttpStatusCode,l.ErrorCategory,l.CreatedUtc
        FROM dbo.IntegrationDeliveryLogs l
        WHERE l.TenantId=@TenantId AND (@FromUtc IS NULL OR l.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR l.CreatedUtc<@ToUtc)
        ORDER BY l.CreatedUtc DESC,l.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Tenant.IntegrationSync'
    BEGIN
        SELECT TOP(@Take) x.Direction,x.Id,x.IntegrationConfigurationId,x.EventType,x.Status,x.AttemptCount,x.OccurredUtc
        FROM(
          SELECT N'Inbound' Direction,e.Id,e.IntegrationConfigurationId,e.EventType,e.Status,CONVERT(INT,NULL) AttemptCount,e.ReceivedUtc OccurredUtc FROM dbo.IntegrationInboundEvents e WHERE e.TenantId=@TenantId
          UNION ALL
          SELECT N'Outbound',o.Id,o.IntegrationConfigurationId,o.EventType,o.Status,o.AttemptCount,o.CreatedUtc FROM dbo.IntegrationOutbox o WHERE o.TenantId=@TenantId
        )x
        WHERE @AllowedStoreIdsCsv IS NULL AND (@FromUtc IS NULL OR x.OccurredUtc>=@FromUtc) AND (@ToUtc IS NULL OR x.OccurredUtc<@ToUtc)
        ORDER BY x.OccurredUtc DESC,x.Id DESC;RETURN;
    END;
    IF @ReportType IN(N'Tenant.AuditActivity',N'Tenant.UserActivity')
    BEGIN
        SELECT TOP(@Take) a.Id,a.StoreId,a.UserId,a.ActorType,a.Action,a.EntityType,a.EntityId,a.CorrelationId,a.CreatedUtc
        FROM dbo.AuditLogs a
        WHERE a.TenantId=@TenantId AND (@StoreId IS NULL OR a.StoreId=@StoreId)
          AND (@FromUtc IS NULL OR a.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR a.CreatedUtc<@ToUtc)
          AND (a.StoreId IS NULL OR @AllowedStoreIdsCsv IS NULL OR a.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
        ORDER BY a.CreatedUtc DESC,a.Id DESC;
        RETURN;
    END;
    THROW 54923,'The tenant report type is not supported.',1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.PlatformReport_Get
    @ReportType NVARCHAR(100),@TenantId BIGINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@Take INT=10000
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @ReportType NOT LIKE N'Platform.%' THROW 54930,'A valid platform report is required.',1;
    IF @FromUtc IS NOT NULL AND @ToUtc IS NOT NULL AND @FromUtc>=@ToUtc THROW 54931,'FromUtc must be earlier than ToUtc.',1;
    IF @Take<1 SET @Take=1;
    IF @Take>10000 SET @Take=10000;

    IF @ReportType=N'Platform.TenantOperationalSummary'
    BEGIN
        SELECT TOP(@Take) t.Id TenantId,t.TenantCode,t.DisplayName,t.IsActive,t.IsSuspended,t.SubscriptionStatus,
            (SELECT COUNT_BIG(*) FROM dbo.Stores s WHERE s.TenantId=t.Id) StoreCount,
            (SELECT COUNT_BIG(*) FROM dbo.Users u WHERE u.TenantId=t.Id) UserCount,
            (SELECT COUNT_BIG(*) FROM dbo.Customers c WHERE c.TenantId=t.Id) ShopperCount,
            (SELECT COUNT_BIG(*) FROM dbo.Cameras c WHERE c.TenantId=t.Id) CameraCount,
            (SELECT COUNT_BIG(*) FROM dbo.CustomerVisits v WHERE v.TenantId=t.Id AND (@FromUtc IS NULL OR v.EnteredUtc>=@FromUtc) AND (@ToUtc IS NULL OR v.EnteredUtc<@ToUtc)) VisitCount,
            (SELECT COALESCE(SUM(i.GrandTotal),0) FROM dbo.RetailInvoices i WHERE i.TenantId=t.Id AND i.Status IN(2,3,4) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)) RetailSales,
            (SELECT COUNT_BIG(*) FROM dbo.Alerts a WHERE a.TenantId=t.Id AND (@FromUtc IS NULL OR a.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR a.CreatedUtc<@ToUtc)) AlertCount
        FROM dbo.Tenants t WHERE (@TenantId IS NULL OR t.Id=@TenantId) ORDER BY t.TenantCode;
        RETURN;
    END;
    IF @ReportType=N'Platform.PlatformBillingInvoices'
    BEGIN
        SELECT TOP(@Take) i.Id,i.TenantId,t.TenantCode,t.DisplayName,i.InvoiceNumber,i.Currency,i.InvoiceUtc,i.DueUtc,i.Status,i.Total,i.PaidAmount,(i.Total-i.PaidAmount) BalanceAmount
        FROM dbo.PlatformInvoices i JOIN dbo.Tenants t ON t.Id=i.TenantId
        WHERE (@TenantId IS NULL OR i.TenantId=@TenantId) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)
        ORDER BY i.InvoiceUtc DESC,i.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Platform.PaymentCollection'
    BEGIN
        SELECT TOP(@Take) p.Id PaymentId,p.TenantId,t.TenantCode,t.DisplayName,p.PlatformInvoiceId,i.InvoiceNumber,p.PaymentMethod,p.Amount,p.Currency,p.GatewayReference,p.TransactionReference,p.PaymentUtc,p.Status
        FROM dbo.PlatformPayments p JOIN dbo.Tenants t ON t.Id=p.TenantId JOIN dbo.PlatformInvoices i ON i.Id=p.PlatformInvoiceId AND i.TenantId=p.TenantId
        WHERE (@TenantId IS NULL OR p.TenantId=@TenantId) AND (@FromUtc IS NULL OR p.PaymentUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.PaymentUtc<@ToUtc)
        ORDER BY p.PaymentUtc DESC,p.Id DESC;RETURN;
    END;
    IF @ReportType=N'Platform.SubscriptionExpiry'
    BEGIN
        SELECT TOP(@Take) s.Id SubscriptionId,s.TenantId,t.TenantCode,t.DisplayName,p.PlanCode,p.PlanName,s.Status,s.CurrentPeriodEndUtc,s.EndsUtc,s.CancelAtPeriodEnd
        FROM dbo.TenantSubscriptions s JOIN dbo.Tenants t ON t.Id=s.TenantId JOIN dbo.SubscriptionPlans p ON p.Id=s.SubscriptionPlanId
        WHERE (@TenantId IS NULL OR s.TenantId=@TenantId) AND (@FromUtc IS NULL OR COALESCE(s.CurrentPeriodEndUtc,s.EndsUtc)>=@FromUtc) AND (@ToUtc IS NULL OR COALESCE(s.CurrentPeriodEndUtc,s.EndsUtc)<@ToUtc)
        ORDER BY COALESCE(s.CurrentPeriodEndUtc,s.EndsUtc),s.Id;
        RETURN;
    END;
    IF @ReportType=N'Platform.WebhookFailures'
    BEGIN
        SELECT TOP(@Take) l.Id,l.TenantId,t.TenantCode,l.Provider,l.Direction,l.Status,l.HttpStatusCode,l.ErrorCategory,l.DurationMilliseconds,l.CreatedUtc
        FROM dbo.IntegrationDeliveryLogs l JOIN dbo.Tenants t ON t.Id=l.TenantId
        WHERE l.Status<>2 AND (@TenantId IS NULL OR l.TenantId=@TenantId) AND (@FromUtc IS NULL OR l.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR l.CreatedUtc<@ToUtc)
        ORDER BY l.CreatedUtc DESC,l.Id DESC;
        RETURN;
    END;
    IF @ReportType=N'Platform.AuditActivity'
    BEGIN
        SELECT TOP(@Take) a.Id,a.TenantId,t.TenantCode,a.StoreId,a.UserId,a.ActorType,a.Action,a.EntityType,a.EntityId,a.CorrelationId,a.CreatedUtc
        FROM dbo.AuditLogs a LEFT JOIN dbo.Tenants t ON t.Id=a.TenantId
        WHERE (@TenantId IS NULL OR a.TenantId=@TenantId) AND (@FromUtc IS NULL OR a.CreatedUtc>=@FromUtc) AND (@ToUtc IS NULL OR a.CreatedUtc<@ToUtc)
        ORDER BY a.CreatedUtc DESC,a.Id DESC;
        RETURN;
    END;
    THROW 54932,'The platform report type is not supported.',1;
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0')
    INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
    VALUES(N'V1.14.0',N'Phase 15 tenant/platform operational reports and asynchronous export job lifecycle',SYSUTCDATETIME(),SUSER_SNAME());
GO

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0')<>1 THROW 54990,'V1.14.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.ReportExportJobs',N'U') IS NULL OR OBJECT_ID(N'dbo.ReportExportEvents',N'U') IS NULL THROW 54991,'Phase 15 report export tables are missing.',1;
IF OBJECT_ID(N'dbo.ReportExportJob_Claim',N'P') IS NULL OR OBJECT_ID(N'dbo.ReportExportRequesterScope_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.ReportAudit_Write',N'P') IS NULL OR OBJECT_ID(N'dbo.ReportExportEvent_Claim',N'P') IS NULL OR OBJECT_ID(N'dbo.TenantReport_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.PlatformReport_Get',N'P') IS NULL THROW 54992,'Phase 15 procedures are incomplete.',1;
GO
