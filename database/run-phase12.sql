/*
 CustSearch AI — Phase 12 standalone database runner
 Version: V1.11.0
 Scope: tenant integration configuration, authenticated inbound receipts, outbound outbox and delivery audit.
 Security: only opaque credential/signing-secret references are stored; no provider secret values or full inbound payloads.
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

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.10.0') THROW 54000,'Phase 11 V1.10.0 must be installed before Phase 12.',1;
GO

IF OBJECT_ID(N'dbo.IntegrationConfigurations',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationConfigurations
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationConfigurations PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        Provider NVARCHAR(100) NOT NULL,
        IntegrationType TINYINT NOT NULL,
        Enabled BIT NOT NULL,
        EndpointBaseUrl NVARCHAR(500) NOT NULL,
        CredentialReference NVARCHAR(200) NULL,
        WebhookSigningSecretReference NVARCHAR(200) NULL,
        PreviousWebhookSigningSecretReference NVARCHAR(200) NULL,
        PreviousSigningSecretValidUntilUtc DATETIME2(7) NULL,
        TimeoutSeconds INT NOT NULL,
        RetryMaxAttempts INT NOT NULL,
        RetryBaseDelaySeconds INT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        UpdatedUtc DATETIME2(7) NOT NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_IntegrationConfigurations_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT CK_IntegrationConfigurations_Type CHECK(IntegrationType BETWEEN 1 AND 4),
        CONSTRAINT CK_IntegrationConfigurations_Endpoint CHECK(EndpointBaseUrl LIKE N'https://%' AND EndpointBaseUrl NOT LIKE N'%@%'),
        CONSTRAINT CK_IntegrationConfigurations_Timeout CHECK(TimeoutSeconds BETWEEN 1 AND 120),
        CONSTRAINT CK_IntegrationConfigurations_Retry CHECK(RetryMaxAttempts BETWEEN 1 AND 10 AND RetryBaseDelaySeconds BETWEEN 1 AND 300),
        CONSTRAINT CK_IntegrationConfigurations_Period CHECK(UpdatedUtc>=CreatedUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationConfigurations') AND name=N'UX_IntegrationConfigurations_Tenant_Id') CREATE UNIQUE INDEX UX_IntegrationConfigurations_Tenant_Id ON dbo.IntegrationConfigurations(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationConfigurations') AND name=N'UX_IntegrationConfigurations_Tenant_Provider_Type') CREATE UNIQUE INDEX UX_IntegrationConfigurations_Tenant_Provider_Type ON dbo.IntegrationConfigurations(TenantId,Provider,IntegrationType);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationConfigurations') AND name=N'IX_IntegrationConfigurations_Tenant_Enabled_Updated') CREATE INDEX IX_IntegrationConfigurations_Tenant_Enabled_Updated ON dbo.IntegrationConfigurations(TenantId,Enabled,UpdatedUtc DESC);
GO

IF OBJECT_ID(N'dbo.IntegrationInboundEvents',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationInboundEvents
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationInboundEvents PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        IntegrationConfigurationId BIGINT NOT NULL,
        ProviderEventId NVARCHAR(200) NOT NULL,
        IdempotencyKey NVARCHAR(200) NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        ContractVersion INT NOT NULL,
        PayloadHash CHAR(64) NOT NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        ProviderTimestampUtc DATETIME2(7) NOT NULL,
        ReceivedUtc DATETIME2(7) NOT NULL,
        ProcessedUtc DATETIME2(7) NULL,
        Status TINYINT NOT NULL,
        CONSTRAINT FK_IntegrationInboundEvents_Configuration FOREIGN KEY(TenantId,IntegrationConfigurationId) REFERENCES dbo.IntegrationConfigurations(TenantId,Id),
        CONSTRAINT CK_IntegrationInboundEvents_Status CHECK(Status BETWEEN 1 AND 3),
        CONSTRAINT CK_IntegrationInboundEvents_ContractVersion CHECK(ContractVersion>=1),
        CONSTRAINT CK_IntegrationInboundEvents_Hash CHECK(PayloadHash NOT LIKE N'%[^0-9a-f]%' AND LEN(PayloadHash)=64),
        CONSTRAINT CK_IntegrationInboundEvents_Period CHECK(ProcessedUtc IS NULL OR ProcessedUtc>=ReceivedUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'UX_IntegrationInboundEvents_Tenant_Config_Event') CREATE UNIQUE INDEX UX_IntegrationInboundEvents_Tenant_Config_Event ON dbo.IntegrationInboundEvents(TenantId,IntegrationConfigurationId,ProviderEventId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'UX_IntegrationInboundEvents_Tenant_Config_Idempotency') CREATE UNIQUE INDEX UX_IntegrationInboundEvents_Tenant_Config_Idempotency ON dbo.IntegrationInboundEvents(TenantId,IntegrationConfigurationId,IdempotencyKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'UX_IntegrationInboundEvents_Tenant_Id') CREATE UNIQUE INDEX UX_IntegrationInboundEvents_Tenant_Id ON dbo.IntegrationInboundEvents(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'IX_IntegrationInboundEvents_Tenant_Received') CREATE INDEX IX_IntegrationInboundEvents_Tenant_Received ON dbo.IntegrationInboundEvents(TenantId,ReceivedUtc DESC,Id DESC);
GO

IF OBJECT_ID(N'dbo.IntegrationOutbox',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationOutbox
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationOutbox PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        IntegrationConfigurationId BIGINT NOT NULL,
        Provider NVARCHAR(100) NOT NULL,
        Destination NVARCHAR(500) NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        ContractVersion INT NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        PayloadHash CHAR(64) NOT NULL,
        Status TINYINT NOT NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_IntegrationOutbox_AttemptCount DEFAULT(0),
        MaxAttempts INT NOT NULL,
        RetryBaseDelaySeconds INT NOT NULL,
        NextAttemptUtc DATETIME2(7) NOT NULL,
        LastResponseCode INT NULL,
        LastError NVARCHAR(2000) NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        IdempotencyKey NVARCHAR(200) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        DeliveredUtc DATETIME2(7) NULL,
        CompletedUtc DATETIME2(7) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_IntegrationOutbox_Configuration FOREIGN KEY(TenantId,IntegrationConfigurationId) REFERENCES dbo.IntegrationConfigurations(TenantId,Id),
        CONSTRAINT CK_IntegrationOutbox_Status CHECK(Status BETWEEN 1 AND 6),
        CONSTRAINT CK_IntegrationOutbox_Attempts CHECK(AttemptCount>=0 AND MaxAttempts BETWEEN 1 AND 10),
        CONSTRAINT CK_IntegrationOutbox_RetryBase CHECK(RetryBaseDelaySeconds BETWEEN 1 AND 300),
        CONSTRAINT CK_IntegrationOutbox_ContractVersion CHECK(ContractVersion>=1),
        CONSTRAINT CK_IntegrationOutbox_Payload CHECK(ISJSON(PayloadJson)=1),
        CONSTRAINT CK_IntegrationOutbox_Hash CHECK(PayloadHash NOT LIKE N'%[^0-9a-f]%' AND LEN(PayloadHash)=64),
        CONSTRAINT CK_IntegrationOutbox_Completion CHECK((Status=3 AND DeliveredUtc IS NOT NULL AND CompletedUtc IS NOT NULL) OR (Status=6 AND CompletedUtc IS NOT NULL) OR (Status NOT IN(3,6) AND CompletedUtc IS NULL))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationOutbox') AND name=N'UX_IntegrationOutbox_Tenant_Id') CREATE UNIQUE INDEX UX_IntegrationOutbox_Tenant_Id ON dbo.IntegrationOutbox(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationOutbox') AND name=N'UX_IntegrationOutbox_Tenant_Idempotency') CREATE UNIQUE INDEX UX_IntegrationOutbox_Tenant_Idempotency ON dbo.IntegrationOutbox(TenantId,IdempotencyKey);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationOutbox') AND name=N'IX_IntegrationOutbox_Status_NextAttempt') CREATE INDEX IX_IntegrationOutbox_Status_NextAttempt ON dbo.IntegrationOutbox(Status,NextAttemptUtc,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationOutbox') AND name=N'IX_IntegrationOutbox_Tenant_Config_Created') CREATE INDEX IX_IntegrationOutbox_Tenant_Config_Created ON dbo.IntegrationOutbox(TenantId,IntegrationConfigurationId,CreatedUtc DESC);
GO

IF OBJECT_ID(N'dbo.IntegrationDeliveryLogs',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationDeliveryLogs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationDeliveryLogs PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        IntegrationConfigurationId BIGINT NOT NULL,
        InboundEventId BIGINT NULL,
        OutboxMessageId BIGINT NULL,
        CorrelationId NVARCHAR(64) NOT NULL,
        Provider NVARCHAR(100) NOT NULL,
        Direction TINYINT NOT NULL,
        Status TINYINT NOT NULL,
        DurationMilliseconds BIGINT NOT NULL,
        HttpStatusCode INT NULL,
        ErrorCategory NVARCHAR(100) NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_IntegrationDeliveryLogs_Configuration FOREIGN KEY(TenantId,IntegrationConfigurationId) REFERENCES dbo.IntegrationConfigurations(TenantId,Id),
        CONSTRAINT FK_IntegrationDeliveryLogs_Inbound FOREIGN KEY(TenantId,InboundEventId) REFERENCES dbo.IntegrationInboundEvents(TenantId,Id),
        CONSTRAINT FK_IntegrationDeliveryLogs_Outbox FOREIGN KEY(TenantId,OutboxMessageId) REFERENCES dbo.IntegrationOutbox(TenantId,Id),
        CONSTRAINT CK_IntegrationDeliveryLogs_Direction CHECK(Direction BETWEEN 1 AND 2),
        CONSTRAINT CK_IntegrationDeliveryLogs_Status CHECK(Status BETWEEN 1 AND 5),
        CONSTRAINT CK_IntegrationDeliveryLogs_Source CHECK((InboundEventId IS NULL AND OutboxMessageId IS NOT NULL) OR (InboundEventId IS NOT NULL AND OutboxMessageId IS NULL)),
        CONSTRAINT CK_IntegrationDeliveryLogs_Duration CHECK(DurationMilliseconds>=0),
        CONSTRAINT CK_IntegrationDeliveryLogs_Http CHECK(HttpStatusCode IS NULL OR HttpStatusCode BETWEEN 100 AND 599)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationDeliveryLogs') AND name=N'IX_IntegrationDeliveryLogs_Tenant_Config_Created') CREATE INDEX IX_IntegrationDeliveryLogs_Tenant_Config_Created ON dbo.IntegrationDeliveryLogs(TenantId,IntegrationConfigurationId,CreatedUtc DESC,Id DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationDeliveryLogs') AND name=N'IX_IntegrationDeliveryLogs_Tenant_Direction_Status') CREATE INDEX IX_IntegrationDeliveryLogs_Tenant_Direction_Status ON dbo.IntegrationDeliveryLogs(TenantId,Direction,Status,CreatedUtc DESC);
GO

CREATE OR ALTER PROCEDURE dbo.Integration_Search @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id,Provider,IntegrationType,Enabled,EndpointBaseUrl,
           CONVERT(BIT,CASE WHEN CredentialReference IS NULL THEN 0 ELSE 1 END) HasCredentialReference,
           CONVERT(BIT,CASE WHEN WebhookSigningSecretReference IS NULL THEN 0 ELSE 1 END) HasWebhookSigningSecret,
           TimeoutSeconds,RetryMaxAttempts,RetryBaseDelaySeconds,CreatedUtc,UpdatedUtc,RowVersion
    FROM dbo.IntegrationConfigurations WHERE TenantId=@TenantId ORDER BY Provider,IntegrationType;
END;
GO

CREATE OR ALTER PROCEDURE dbo.IntegrationOutbox_Claim @BatchSize INT=50,@UtcNow DATETIME2(7)=NULL
AS
BEGIN
    SET NOCOUNT ON;SET XACT_ABORT ON;IF @UtcNow IS NULL SET @UtcNow=SYSUTCDATETIME();IF @BatchSize<1 SET @BatchSize=1;IF @BatchSize>200 SET @BatchSize=200;
    ;WITH Due AS(SELECT TOP(@BatchSize)* FROM dbo.IntegrationOutbox WITH(UPDLOCK,READPAST,ROWLOCK) WHERE Status IN(1,2,4,5) AND NextAttemptUtc<=@UtcNow ORDER BY NextAttemptUtc,Id)
    UPDATE Due SET Status=2,AttemptCount=AttemptCount+1,NextAttemptUtc=DATEADD(MINUTE,2,@UtcNow),LastError=NULL
    OUTPUT inserted.Id,inserted.TenantId,inserted.IntegrationConfigurationId,inserted.Provider,inserted.Destination,inserted.EventType,inserted.ContractVersion,inserted.PayloadJson,inserted.AttemptCount,inserted.MaxAttempts,inserted.RetryBaseDelaySeconds,inserted.CorrelationId,inserted.IdempotencyKey;
END;
GO

CREATE OR ALTER PROCEDURE dbo.IntegrationOutbox_ManualRetry @TenantId BIGINT,@DeliveryId BIGINT,@UtcNow DATETIME2(7)=NULL
AS
BEGIN
    SET NOCOUNT ON;IF @UtcNow IS NULL SET @UtcNow=SYSUTCDATETIME();
    UPDATE dbo.IntegrationOutbox SET Status=5,AttemptCount=0,NextAttemptUtc=@UtcNow,LastResponseCode=NULL,LastError=NULL,DeliveredUtc=NULL,CompletedUtc=NULL WHERE TenantId=@TenantId AND Id=@DeliveryId AND Status IN(4,6);
    IF @@ROWCOUNT<>1 THROW 54020,'Failed/dead-letter integration delivery was not found.',1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.IntegrationDeliveryLog_Search @TenantId BIGINT,@IntegrationConfigurationId BIGINT=NULL,@Take INT=100
AS
BEGIN
    SET NOCOUNT ON;IF @Take<1 SET @Take=1;IF @Take>500 SET @Take=500;
    SELECT TOP(@Take) Id,IntegrationConfigurationId,InboundEventId,OutboxMessageId,CorrelationId,Provider,Direction,Status,DurationMilliseconds,HttpStatusCode,ErrorCategory,CreatedUtc FROM dbo.IntegrationDeliveryLogs WHERE TenantId=@TenantId AND (@IntegrationConfigurationId IS NULL OR IntegrationConfigurationId=@IntegrationConfigurationId) ORDER BY CreatedUtc DESC,Id DESC;
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.11.0') INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.11.0',N'Phase 12 secure tenant integrations, HMAC inbound receipts, outbound outbox and delivery audit',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.11.0')<>1 THROW 54090,'V1.11.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.IntegrationConfigurations',N'U') IS NULL OR OBJECT_ID(N'dbo.IntegrationInboundEvents',N'U') IS NULL OR OBJECT_ID(N'dbo.IntegrationOutbox',N'U') IS NULL OR OBJECT_ID(N'dbo.IntegrationDeliveryLogs',N'U') IS NULL THROW 54091,'Phase 12 tables are incomplete.',1;
IF OBJECT_ID(N'dbo.Integration_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.IntegrationOutbox_Claim',N'P') IS NULL OR OBJECT_ID(N'dbo.IntegrationOutbox_ManualRetry',N'P') IS NULL OR OBJECT_ID(N'dbo.IntegrationDeliveryLog_Search',N'P') IS NULL THROW 54092,'Phase 12 procedures are incomplete.',1;
GO
