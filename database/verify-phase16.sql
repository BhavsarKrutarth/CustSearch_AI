USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')<>1 THROW 55200,'V1.15.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.SystemSettings',N'U') IS NULL OR OBJECT_ID(N'dbo.WorkerHeartbeats',N'U') IS NULL THROW 55201,'Phase 16 tables are missing.',1;
IF (SELECT COUNT(*) FROM dbo.SystemSettings WHERE TenantId IS NULL AND StoreId IS NULL)<>33 THROW 55202,'Phase 16 platform defaults are incomplete.',1;
IF EXISTS(SELECT 1 FROM dbo.SystemSettings WHERE SettingKey=N'AutoLinkHouseholdFromFaceSimilarity' AND SettingValue<>N'false') THROW 55203,'Unsafe household inference setting detected.',1;
IF OBJECT_ID(N'dbo.SystemSetting_List',N'P') IS NULL OR OBJECT_ID(N'dbo.SystemSetting_Upsert',N'P') IS NULL OR OBJECT_ID(N'dbo.WorkerHeartbeat_Upsert',N'P') IS NULL OR OBJECT_ID(N'dbo.AuditLog_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.SystemHealth_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.ReportExportJob_ArtifactDeleted',N'P') IS NULL OR OBJECT_ID(N'dbo.OperationalRetention_Run',N'P') IS NULL THROW 55204,'Phase 16 procedures are incomplete.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=1 AND Name=N'PlatformSettings.Manage' AND IsActive=1) OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=1 AND Name=N'SystemHealth.View' AND IsActive=1) THROW 55205,'Phase 16 platform permissions are incomplete.',1;
DBCC CHECKCONSTRAINTS(N'dbo.SystemSettings') WITH ALL_CONSTRAINTS;
DBCC CHECKCONSTRAINTS(N'dbo.WorkerHeartbeats') WITH ALL_CONSTRAINTS;
SELECT N'Phase 16 database verification passed.' Result;
GO
