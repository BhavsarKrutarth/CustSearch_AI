/* CustSearch AI — Phase 12 read-only verification. Run after database/run-phase12.sql. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.11.0')<>1 THROW 54200,'V1.11.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.IntegrationConfigurations',N'U') IS NULL OR OBJECT_ID(N'dbo.IntegrationInboundEvents',N'U') IS NULL OR OBJECT_ID(N'dbo.IntegrationOutbox',N'U') IS NULL OR OBJECT_ID(N'dbo.IntegrationDeliveryLogs',N'U') IS NULL THROW 54201,'Phase 12 tables missing.',1;
IF OBJECT_ID(N'dbo.Integration_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.IntegrationOutbox_Claim',N'P') IS NULL OR OBJECT_ID(N'dbo.IntegrationOutbox_ManualRetry',N'P') IS NULL OR OBJECT_ID(N'dbo.IntegrationDeliveryLog_Search',N'P') IS NULL THROW 54202,'Phase 12 procedures missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'UX_IntegrationInboundEvents_Tenant_Config_Event' AND is_unique=1) OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationInboundEvents') AND name=N'UX_IntegrationInboundEvents_Tenant_Config_Idempotency' AND is_unique=1) THROW 54203,'Inbound replay/idempotency indexes missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.IntegrationOutbox') AND name=N'UX_IntegrationOutbox_Tenant_Idempotency' AND is_unique=1) THROW 54204,'Outbound idempotency index missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.IntegrationConfigurations') AND name=N'CredentialReference') OR NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.IntegrationConfigurations') AND name=N'WebhookSigningSecretReference') THROW 54205,'Opaque secret-reference columns missing.',1;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id IN(OBJECT_ID(N'dbo.IntegrationConfigurations'),OBJECT_ID(N'dbo.IntegrationInboundEvents'),OBJECT_ID(N'dbo.IntegrationDeliveryLogs')) AND name IN(N'Credential',N'Password',N'Secret',N'AccessToken',N'RefreshToken',N'PayloadJson',N'RequestBody',N'ResponseBody')) THROW 54206,'Plain secrets or unnecessary full payload columns detected.',1;
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.IntegrationOutbox_Claim')) NOT LIKE N'%UPDLOCK%' OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.IntegrationOutbox_Claim')) NOT LIKE N'%READPAST%' THROW 54207,'Concurrent integration outbox claim locking missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Integrations.View') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Integrations.Manage') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Webhooks.View') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Webhooks.Manage') THROW 54208,'Integration/webhook permissions missing.',1;
EXEC dbo.Integration_Search @TenantId=-1;
EXEC dbo.IntegrationDeliveryLog_Search @TenantId=-1,@IntegrationConfigurationId=NULL,@Take=10;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
PRINT 'PHASE12_DATABASE_VERIFICATION_GREEN';
GO
