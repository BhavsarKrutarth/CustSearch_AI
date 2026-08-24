/*
 CustSearch AI — Phase 11 production database upgrade
 Version: V1.10.0
 Scope: tenant/store alerts, durable real-time recovery events and transactional notification outbox.
 Rules: SQL Server 2022, repeat-safe, no EF migrations, UTC timestamps, no provider credentials.
*/
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

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.9.0')
    THROW 53000,'Phase 10 V1.9.0 must be installed before Phase 11.',1;
GO

-- 11A — Authoritative tenant/store alert domain. StoreId NULL means tenant-wide.
IF OBJECT_ID(N'dbo.Alerts',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Alerts
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Alerts PRIMARY KEY,
        AlertType NVARCHAR(100) NOT NULL,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NULL,
        Severity TINYINT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(2000) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId NVARCHAR(100) NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        AcknowledgedUtc DATETIME2(7) NULL,
        AcknowledgedByUserId BIGINT NULL,
        ResolvedUtc DATETIME2(7) NULL,
        Status TINYINT NOT NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        DeduplicationKey NVARCHAR(200) NOT NULL,
        CONSTRAINT FK_Alerts_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_Alerts_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_Alerts_AcknowledgedByUser FOREIGN KEY(AcknowledgedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Alerts_Severity CHECK(Severity BETWEEN 1 AND 3),
        CONSTRAINT CK_Alerts_Status CHECK(Status BETWEEN 1 AND 5),
        CONSTRAINT CK_Alerts_Acknowledgement CHECK((AcknowledgedUtc IS NULL AND AcknowledgedByUserId IS NULL) OR (AcknowledgedUtc IS NOT NULL AND AcknowledgedByUserId IS NOT NULL)),
        CONSTRAINT CK_Alerts_Resolution CHECK(ResolvedUtc IS NULL OR ResolvedUtc>=CreatedUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Alerts') AND name=N'UX_Alerts_Tenant_Id') CREATE UNIQUE INDEX UX_Alerts_Tenant_Id ON dbo.Alerts(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Alerts') AND name=N'UX_Alerts_Tenant_DeduplicationKey') CREATE UNIQUE INDEX UX_Alerts_Tenant_DeduplicationKey ON dbo.Alerts(TenantId,DeduplicationKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Alerts') AND name=N'IX_Alerts_Tenant_Store_Status_Created') CREATE INDEX IX_Alerts_Tenant_Store_Status_Created ON dbo.Alerts(TenantId,StoreId,Status,CreatedUtc DESC,Id DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Alerts') AND name=N'IX_Alerts_Tenant_Entity') CREATE INDEX IX_Alerts_Tenant_Entity ON dbo.Alerts(TenantId,EntityType,EntityId);
GO

-- 11D/11E — Durable ordered events remain authoritative for reconnect recovery.
IF OBJECT_ID(N'dbo.RealtimeEvents',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RealtimeEvents
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RealtimeEvents PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NULL,
        AlertId BIGINT NOT NULL,
        EventName NVARCHAR(100) NOT NULL,
        ContractVersion INT NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        OccurredUtc DATETIME2(7) NOT NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        DeduplicationKey NVARCHAR(200) NOT NULL,
        CONSTRAINT FK_RealtimeEvents_Alerts_TenantAlert FOREIGN KEY(TenantId,AlertId) REFERENCES dbo.Alerts(TenantId,Id),
        CONSTRAINT FK_RealtimeEvents_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT CK_RealtimeEvents_ContractVersion CHECK(ContractVersion>=1),
        CONSTRAINT CK_RealtimeEvents_Payload CHECK(ISJSON(PayloadJson)=1)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RealtimeEvents') AND name=N'UX_RealtimeEvents_Tenant_Id') CREATE UNIQUE INDEX UX_RealtimeEvents_Tenant_Id ON dbo.RealtimeEvents(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RealtimeEvents') AND name=N'UX_RealtimeEvents_Tenant_DeduplicationKey') CREATE UNIQUE INDEX UX_RealtimeEvents_Tenant_DeduplicationKey ON dbo.RealtimeEvents(TenantId,DeduplicationKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RealtimeEvents') AND name=N'IX_RealtimeEvents_Tenant_Store_Cursor') CREATE INDEX IX_RealtimeEvents_Tenant_Store_Cursor ON dbo.RealtimeEvents(TenantId,StoreId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RealtimeEvents') AND name=N'IX_RealtimeEvents_Tenant_Occurred') CREATE INDEX IX_RealtimeEvents_Tenant_Occurred ON dbo.RealtimeEvents(TenantId,OccurredUtc DESC);
GO

-- 11B/11G — Transactional channel outbox. External adapters run only after commit.
IF OBJECT_ID(N'dbo.NotificationOutbox',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationOutbox
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationOutbox PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NULL,
        AlertId BIGINT NOT NULL,
        RealtimeEventId BIGINT NOT NULL,
        Channel NVARCHAR(30) NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        ContractVersion INT NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        Status TINYINT NOT NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_NotificationOutbox_AttemptCount DEFAULT(0),
        NextAttemptUtc DATETIME2(7) NOT NULL,
        LastError NVARCHAR(2000) NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        IdempotencyKey NVARCHAR(200) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        ProcessedUtc DATETIME2(7) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_NotificationOutbox_Alerts_TenantAlert FOREIGN KEY(TenantId,AlertId) REFERENCES dbo.Alerts(TenantId,Id),
        CONSTRAINT FK_NotificationOutbox_RealtimeEvents_TenantEvent FOREIGN KEY(TenantId,RealtimeEventId) REFERENCES dbo.RealtimeEvents(TenantId,Id),
        CONSTRAINT FK_NotificationOutbox_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT CK_NotificationOutbox_Status CHECK(Status BETWEEN 1 AND 6),
        CONSTRAINT CK_NotificationOutbox_AttemptCount CHECK(AttemptCount>=0),
        CONSTRAINT CK_NotificationOutbox_ContractVersion CHECK(ContractVersion>=1),
        CONSTRAINT CK_NotificationOutbox_Payload CHECK(ISJSON(PayloadJson)=1),
        CONSTRAINT CK_NotificationOutbox_Processed CHECK((Status IN(3,6) AND ProcessedUtc IS NOT NULL) OR (Status NOT IN(3,6) AND ProcessedUtc IS NULL))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.NotificationOutbox') AND name=N'UX_NotificationOutbox_IdempotencyKey') CREATE UNIQUE INDEX UX_NotificationOutbox_IdempotencyKey ON dbo.NotificationOutbox(IdempotencyKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.NotificationOutbox') AND name=N'IX_NotificationOutbox_Status_NextAttempt') CREATE INDEX IX_NotificationOutbox_Status_NextAttempt ON dbo.NotificationOutbox(Status,NextAttemptUtc,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.NotificationOutbox') AND name=N'IX_NotificationOutbox_Tenant_Status_Created') CREATE INDEX IX_NotificationOutbox_Tenant_Status_Created ON dbo.NotificationOutbox(TenantId,Status,CreatedUtc);
GO

-- Tenant/store reads always accept server-authorized store IDs, never browser TenantId.
CREATE OR ALTER PROCEDURE dbo.Alert_Search
    @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@Status TINYINT=NULL,@Take INT=100
AS
BEGIN
    SET NOCOUNT ON;
    IF @Take<1 SET @Take=1;IF @Take>200 SET @Take=200;
    IF @StoreId IS NOT NULL AND @AllowedStoreIdsCsv IS NOT NULL AND NOT EXISTS(SELECT 1 FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') s WHERE TRY_CONVERT(BIGINT,s.value)=@StoreId) RETURN;
    SELECT TOP(@Take) Id,AlertType,StoreId,Severity,Title,Message,EntityType,EntityId,CreatedUtc,AcknowledgedUtc,AcknowledgedByUserId,ResolvedUtc,Status,CorrelationId,DeduplicationKey
    FROM dbo.Alerts
    WHERE TenantId=@TenantId AND (@StoreId IS NULL OR StoreId=@StoreId) AND (@Status IS NULL OR Status=@Status)
      AND (StoreId IS NULL OR @AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') s WHERE TRY_CONVERT(BIGINT,s.value)=StoreId))
    ORDER BY CreatedUtc DESC,Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.AlertRecovery_Get
    @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@AfterEventId BIGINT=0,@Take INT=200
AS
BEGIN
    SET NOCOUNT ON;
    IF @AfterEventId<0 THROW 53020,'Recovery cursor cannot be negative.',1;
    IF @Take<1 SET @Take=1;IF @Take>500 SET @Take=500;
    SELECT TOP(@Take) Id EventId,EventName,ContractVersion,PayloadJson,OccurredUtc,StoreId,CorrelationId
    FROM dbo.RealtimeEvents
    WHERE TenantId=@TenantId AND Id>@AfterEventId
      AND (StoreId IS NULL OR @AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') s WHERE TRY_CONVERT(BIGINT,s.value)=StoreId))
    ORDER BY Id;
END;
GO

-- READPAST/UPDLOCK claims avoid duplicate processing between concurrent dispatchers.
CREATE OR ALTER PROCEDURE dbo.NotificationOutbox_Claim @BatchSize INT=50,@UtcNow DATETIME2(7)=NULL
AS
BEGIN
    SET NOCOUNT ON;SET XACT_ABORT ON;
    IF @UtcNow IS NULL SET @UtcNow=SYSUTCDATETIME();IF @BatchSize<1 SET @BatchSize=1;IF @BatchSize>200 SET @BatchSize=200;
    ;WITH Due AS
    (
        SELECT TOP(@BatchSize) * FROM dbo.NotificationOutbox WITH(UPDLOCK,READPAST,ROWLOCK)
        WHERE Status IN(1,4,5,2) AND NextAttemptUtc<=@UtcNow ORDER BY NextAttemptUtc,Id
    )
    UPDATE Due SET Status=2,AttemptCount=AttemptCount+1,NextAttemptUtc=DATEADD(MINUTE,2,@UtcNow),LastError=NULL
    OUTPUT inserted.Id,inserted.TenantId,inserted.StoreId,inserted.AlertId,inserted.RealtimeEventId,inserted.Channel,inserted.EventType,inserted.ContractVersion,inserted.PayloadJson,inserted.AttemptCount,inserted.CorrelationId,inserted.IdempotencyKey;
END;
GO

CREATE OR ALTER PROCEDURE dbo.NotificationOutbox_Metrics @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
      SUM(CASE WHEN Status IN(1,2,4,5) THEN CONVERT(BIGINT,1) ELSE 0 END) OutboxBacklog,
      SUM(CASE WHEN Status=3 THEN CONVERT(BIGINT,1) ELSE 0 END) DeliverySuccesses,
      SUM(CASE WHEN Status IN(4,5,6) THEN CONVERT(BIGINT,1) ELSE 0 END) DeliveryFailures,
      SUM(CASE WHEN AttemptCount>1 THEN CONVERT(BIGINT,AttemptCount-1) ELSE 0 END) Retries,
      SUM(CASE WHEN Status=6 THEN CONVERT(BIGINT,1) ELSE 0 END) DeadLetters,
      MIN(CASE WHEN Status IN(1,2,4,5) THEN CreatedUtc END) OldestPendingUtc
    FROM dbo.NotificationOutbox WHERE TenantId=@TenantId;
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.10.0')
    INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.10.0',N'Phase 11 tenant/store alerts, durable real-time recovery and transactional notification outbox',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.10.0')<>1 THROW 53090,'V1.10.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.Alerts',N'U') IS NULL OR OBJECT_ID(N'dbo.RealtimeEvents',N'U') IS NULL OR OBJECT_ID(N'dbo.NotificationOutbox',N'U') IS NULL THROW 53091,'Phase 11 tables are incomplete.',1;
IF OBJECT_ID(N'dbo.Alert_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.AlertRecovery_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.NotificationOutbox_Claim',N'P') IS NULL OR OBJECT_ID(N'dbo.NotificationOutbox_Metrics',N'P') IS NULL THROW 53092,'Phase 11 procedures are incomplete.',1;
GO
