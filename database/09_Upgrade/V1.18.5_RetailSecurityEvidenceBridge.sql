/* Camera Motion / Tenant Storage - Phase G: retail-security evidence bridge. */
:on error exit
USE [CustSearch_AI];
GO
SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;
SET ARITHABORT ON;SET CONCAT_NULL_YIELDS_NULL ON;SET NUMERIC_ROUNDABORT OFF;
GO
SET NOCOUNT ON;SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL
 OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.4')
 OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.17.0')
 THROW 56260,'Retail security evidence bridge requires V1.17.0 and V1.18.4.',1;
GO
IF EXISTS(SELECT 1 FROM dbo.CameraEvidence e WHERE e.SecurityIncidentId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.SecurityIncidents i WHERE i.TenantId=e.TenantId AND i.StoreId=e.StoreId AND i.Id=e.SecurityIncidentId))
 THROW 56261,'CameraEvidence contains an invalid security incident link.',1;
GO
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.CameraEvidence') AND name=N'FK_CameraEvidence_SecurityIncident')
 ALTER TABLE dbo.CameraEvidence WITH CHECK ADD CONSTRAINT FK_CameraEvidence_SecurityIncident FOREIGN KEY(TenantId,StoreId,SecurityIncidentId) REFERENCES dbo.SecurityIncidents(TenantId,StoreId,Id);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraEvidence') AND name=N'IX_CameraEvidence_SecurityIncident')
 CREATE INDEX IX_CameraEvidence_SecurityIncident ON dbo.CameraEvidence(TenantId,StoreId,SecurityIncidentId,CapturedUtc DESC) INCLUDE(EvidenceType,CameraId,RetentionUntilUtc,DeletedUtc,IsRestricted,StorageObjectKey) WHERE SecurityIncidentId IS NOT NULL;
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.5')
 INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.18.5',N'Camera Motion Phase G retail security correlation and quota evidence bridge',SYSUTCDATETIME(),SUSER_SNAME());
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.5')<>1 THROW 56269,'V1.18.5 must exist exactly once.',1;
GO
