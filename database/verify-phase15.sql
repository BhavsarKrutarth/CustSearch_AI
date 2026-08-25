USE [CustSearch_AI];
GO
SET NOCOUNT ON;
IF(SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0')<>1 THROW 55000,'V1.14.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.ExportJobs',N'U') IS NULL THROW 55001,'ExportJobs is missing.',1;
IF OBJECT_ID(N'dbo.Report_TenantOperationalSummary',N'P') IS NULL OR OBJECT_ID(N'dbo.Report_PlatformTenantSummary',N'P') IS NULL THROW 55002,'Report procedures are missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ExportJobs') AND name=N'IX_ExportJobs_Status_Created') THROW 55003,'Export job queue index is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'Reports.Export' AND Scope=2 AND IsActive=1) OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'PlatformReports.Export' AND Scope=1 AND IsActive=1) THROW 55004,'Report permissions are incomplete.',1;
SELECT VersionNumber,Description,AppliedUtc,AppliedBy FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0';
PRINT 'Phase 15 reports and asynchronous exports verification passed.';
GO

