/* Camera Motion / Tenant Storage - Phase F: evidence retention and reconciliation worker. */
:on error exit
USE [CustSearch_AI];
GO
SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;
SET ARITHABORT ON;SET CONCAT_NULL_YIELDS_NULL ON;SET NUMERIC_ROUNDABORT OFF;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL
   OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.3')
    THROW 56250,'Evidence retention requires V1.18.3.',1;
GO
IF COL_LENGTH(N'dbo.TenantStorageUsage',N'LastReconciledUtc') IS NULL
    ALTER TABLE dbo.TenantStorageUsage ADD LastReconciledUtc DATETIME2(7) NULL;
GO
IF NOT EXISTS(
    SELECT 1 FROM sys.indexes
    WHERE object_id=OBJECT_ID(N'dbo.TenantStorageUsage')
      AND name=N'IX_TenantStorageUsage_Reconciliation')
    CREATE INDEX IX_TenantStorageUsage_Reconciliation
        ON dbo.TenantStorageUsage(LastReconciledUtc,TenantId)
        INCLUDE(UsedBytes,QuotaBytes);
GO
IF OBJECT_ID(N'dbo.WorkerControls',N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM dbo.WorkerControls WHERE WorkerType=N'evidence-retention')
    INSERT dbo.WorkerControls(WorkerType,IsPaused,Reason,UpdatedByUserId,UpdatedUtc)
    VALUES(N'evidence-retention',0,NULL,NULL,SYSUTCDATETIME());
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.4')
    INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
    VALUES(N'V1.18.4',N'Camera Motion Phase F evidence retention worker',SYSUTCDATETIME(),SUSER_SNAME());
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.4')<>1
    THROW 56259,'V1.18.4 must exist exactly once.',1;
GO
