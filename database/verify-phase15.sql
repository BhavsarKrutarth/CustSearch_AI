USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0')<>1 THROW 55000,'V1.14.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.ReportExportJobs',N'U') IS NULL OR OBJECT_ID(N'dbo.ReportExportEvents',N'U') IS NULL THROW 55001,'Phase 15 report export tables are missing.',1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.ReportExportJobs') AND name IN(N'TenantId',N'RequestedByUserId',N'ReportType',N'FilterJson',N'Format',N'Status',N'ProgressPercent',N'StorageReference',N'Sha256',N'ExpiresUtc',N'LeaseToken',N'RowVersion'))<>12 THROW 55002,'ReportExportJobs columns are incomplete.',1;
IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ReportExportJobs') AND name IN(N'IX_ReportExportJobs_Queue',N'IX_ReportExportJobs_Requester',N'IX_ReportExportJobs_Tenant_Requester',N'IX_ReportExportJobs_Expiry'))<>4 THROW 55003,'ReportExportJobs indexes are incomplete.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.ReportExportJobs') AND is_disabled=0)<>2 THROW 55004,'ReportExportJobs foreign keys are incomplete or disabled.',1;
IF (SELECT COUNT(*) FROM sys.procedures WHERE name IN(N'ReportExportJob_Create',N'ReportExportJob_Get',N'ReportExportJob_List',N'ReportExportJob_Claim',N'ReportExportJob_Progress',N'ReportExportJob_Complete',N'ReportExportJob_Fail',N'ReportExportJob_Expire',N'ReportExportRequesterScope_Get',N'ReportAudit_Write',N'ReportExportEvent_Claim',N'ReportExportEvent_Complete',N'ReportExportEvent_Fail',N'TenantReport_Get',N'PlatformReport_Get'))<>15 THROW 55005,'Phase 15 procedures are incomplete.',1;
IF EXISTS(SELECT 1 FROM sys.sql_modules WHERE object_id IN(OBJECT_ID(N'dbo.TenantReport_Get'),OBJECT_ID(N'dbo.PlatformReport_Get')) AND definition LIKE N'%EXEC(%') THROW 55006,'Report procedures must not use caller-controlled dynamic SQL.',1;
IF (SELECT COUNT(*) FROM dbo.ReportExportJobs)<>0 THROW 55007,'The Phase 15 verifier does not expect persisted test export jobs.',1;
IF (SELECT COUNT(*) FROM dbo.ReportExportEvents)<>0 THROW 55008,'The Phase 15 verifier does not expect persisted export events.',1;

EXEC dbo.TenantReport_Get @TenantId=9223372036854775807,@AllowedStoreIdsCsv=N'',@ReportType=N'Tenant.DailyVisitors',@Take=1;
EXEC dbo.PlatformReport_Get @ReportType=N'Platform.TenantOperationalSummary',@TenantId=9223372036854775807,@Take=1;
EXEC dbo.ReportExportJob_Claim @LeaseSeconds=300;

SELECT VersionNumber,Description,AppliedUtc,AppliedBy FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0';
SELECT N'Phase 15 report/export database verification passed.' Result;
GO
