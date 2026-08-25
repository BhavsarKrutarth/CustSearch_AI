USE [CustSearch_AI];
GO
SET NOCOUNT ON;
IF(SELECT COUNT(*)FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')<>1 THROW 55200,'V1.15.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.OperationalSettings',N'U')IS NULL OR OBJECT_ID(N'dbo.OperationalSecretReferences',N'U')IS NULL OR OBJECT_ID(N'dbo.WorkerControls',N'U')IS NULL OR OBJECT_ID(N'dbo.WorkerLeases',N'U')IS NULL OR OBJECT_ID(N'dbo.WorkerHeartbeats',N'U')IS NULL OR OBJECT_ID(N'dbo.RetentionPolicies',N'U')IS NULL OR OBJECT_ID(N'dbo.RetentionRuns',N'U')IS NULL THROW 55201,'Phase 16 tables are incomplete.',1;
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'WorkerType') IS NULL OR COL_LENGTH(N'dbo.WorkerHeartbeats',N'IsReady') IS NULL THROW 55204,'Worker heartbeat compatibility columns are missing.',1;
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'WorkerName') IS NOT NULL AND EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.WorkerHeartbeats') AND name=N'WorkerName' AND default_object_id=0) THROW 55205,'Legacy WorkerName must have an insert-compatible default.',1;
IF COL_LENGTH(N'dbo.WorkerHeartbeats',N'Status') IS NOT NULL AND EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.WorkerHeartbeats') AND name=N'Status' AND default_object_id=0) THROW 55206,'Legacy Status must have an insert-compatible default.',1;
IF OBJECT_ID(N'dbo.OperationalRetention_Run',N'P')IS NULL OR OBJECT_ID(N'dbo.TR_AuditLogs_ImmutableUpdate',N'TR')IS NULL THROW 55202,'Phase 16 retention or audit hardening is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'PlatformOperations.View')OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'PlatformOperations.Manage')THROW 55203,'Phase 16 permissions are missing.',1;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
PRINT 'Phase 16 operational platform verification passed.';
GO
