/* Camera Motion / Tenant Storage - Phase D: optional camera detection zones. */
:on error exit
USE [CustSearch_AI];
GO
SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;
SET ARITHABORT ON;SET CONCAT_NULL_YIELDS_NULL ON;SET NUMERIC_ROUNDABORT OFF;
GO
SET NOCOUNT ON;SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.1') THROW 56230,'Optional detection zones require V1.18.1.',1;
GO
IF COL_LENGTH(N'dbo.Cameras',N'UseDetectionZone') IS NULL ALTER TABLE dbo.Cameras ADD UseDetectionZone BIT NOT NULL CONSTRAINT DF_Cameras_UseDetectionZone DEFAULT(0) WITH VALUES;
GO
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.CameraZoneConfigurations') AND name=N'CK_CameraZones_Type') ALTER TABLE dbo.CameraZoneConfigurations DROP CONSTRAINT CK_CameraZones_Type;
ALTER TABLE dbo.CameraZoneConfigurations WITH CHECK ADD CONSTRAINT CK_CameraZones_Type CHECK(ZoneType BETWEEN 1 AND 10);
ALTER TABLE dbo.CameraZoneConfigurations CHECK CONSTRAINT CK_CameraZones_Type;
GO
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'Cameras.ManageZones') INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) VALUES(2,N'Cameras.ManageZones',N'Manage tenant camera detection zones and detection area settings.',1,SYSUTCDATETIME());
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p WHERE p.Name=N'Cameras.ManageZones' AND r.Scope=2 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.2') INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.18.2',N'Camera Motion Phase D optional detection zones',SYSUTCDATETIME(),SUSER_SNAME());
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.2')<>1 THROW 56239,'V1.18.2 must exist exactly once.',1;
GO
