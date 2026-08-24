/*
 CustSearch AI — Phase 11 read-only verification script
 Run after database/run-phase11.sql. It does not create/drop business objects.
*/
USE [CustSearch_AI];
GO
SET NOCOUNT ON;

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.10.0')<>1 THROW 53200,'V1.10.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.Alerts',N'U') IS NULL THROW 53201,'Alerts missing.',1;
IF OBJECT_ID(N'dbo.RealtimeEvents',N'U') IS NULL THROW 53202,'RealtimeEvents missing.',1;
IF OBJECT_ID(N'dbo.NotificationOutbox',N'U') IS NULL THROW 53203,'NotificationOutbox missing.',1;
IF OBJECT_ID(N'dbo.Alert_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.AlertRecovery_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.NotificationOutbox_Claim',N'P') IS NULL OR OBJECT_ID(N'dbo.NotificationOutbox_Metrics',N'P') IS NULL THROW 53204,'Phase 11 procedures missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Alerts') AND name=N'UX_Alerts_Tenant_DeduplicationKey' AND is_unique=1) THROW 53205,'Authoritative alert deduplication index missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.NotificationOutbox') AND name=N'UX_NotificationOutbox_IdempotencyKey' AND is_unique=1) THROW 53206,'Outbox idempotency index missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RealtimeEvents') AND name=N'IX_RealtimeEvents_Tenant_Store_Cursor') THROW 53207,'Recovery cursor index missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.NotificationOutbox') AND name=N'RowVersion' AND system_type_id=189) THROW 53208,'Outbox optimistic claim rowversion missing.',1;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id IN(OBJECT_ID(N'dbo.Alerts'),OBJECT_ID(N'dbo.NotificationOutbox'),OBJECT_ID(N'dbo.RealtimeEvents')) AND (name LIKE N'%Password%' OR name LIKE N'%Credential%' OR name LIKE N'%Token%')) THROW 53209,'Alert/outbox schema must not store provider credentials or tokens.',1;
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.NotificationOutbox_Claim')) NOT LIKE N'%UPDLOCK%' OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.NotificationOutbox_Claim')) NOT LIKE N'%READPAST%' THROW 53210,'Concurrent outbox claim locking is missing.',1;
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.AlertRecovery_Get')) NOT LIKE N'%@AfterEventId%' OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.AlertRecovery_Get')) NOT LIKE N'%@AllowedStoreIdsCsv%' THROW 53211,'Recovery cursor/store authorization scope is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Alerts.View') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Alerts.Acknowledge') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Alerts.Configure') THROW 53212,'Alert permissions missing.',1;

EXEC dbo.Alert_Search @TenantId=-1,@AllowedStoreIdsCsv=N'',@StoreId=NULL,@Status=NULL,@Take=10;
EXEC dbo.AlertRecovery_Get @TenantId=-1,@AllowedStoreIdsCsv=N'',@AfterEventId=0,@Take=10;
EXEC dbo.NotificationOutbox_Metrics @TenantId=-1;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
PRINT 'PHASE11_DATABASE_VERIFICATION_GREEN';
GO
