/* Camera Motion / Tenant Storage — Phase A: authoritative tenant-wide active camera quota support. */
:on error exit
USE [CustSearch_AI];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL
 OR OBJECT_ID(N'dbo.Tenants',N'U') IS NULL
 OR OBJECT_ID(N'dbo.Cameras',N'U') IS NULL
 OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.17.1')
 THROW 56200,'Camera quota Phase A requires the V1.17.1 baseline.',1;
GO

/* Supports tenant-wide IsActive counts while offline cameras continue to consume a slot. */
IF NOT EXISTS(
 SELECT 1 FROM sys.indexes
 WHERE object_id=OBJECT_ID(N'dbo.Cameras') AND name=N'IX_Cameras_Tenant_Active')
 CREATE INDEX IX_Cameras_Tenant_Active ON dbo.Cameras(TenantId,IsActive) INCLUDE(Id,StoreId,Status);
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.0')
 INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
 VALUES(N'V1.18.0',N'Camera Motion Phase A tenant-wide active camera quota support',SYSUTCDATETIME(),SUSER_SNAME());

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.0')<>1
 THROW 56209,'V1.18.0 must exist exactly once.',1;
GO
