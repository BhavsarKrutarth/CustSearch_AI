/* Camera Motion / Tenant Storage — Phase C: camera motion rule engine configuration. */
:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON;SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.0') THROW 56220,'Motion rules require V1.18.0.',1;
GO
IF COL_LENGTH(N'dbo.Cameras',N'MotionRulesEnabled') IS NULL ALTER TABLE dbo.Cameras ADD MotionRulesEnabled BIT NOT NULL CONSTRAINT DF_Cameras_MotionRulesEnabled DEFAULT(0) WITH VALUES;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CameraZoneConfigurations') AND name=N'UX_CameraZones_Tenant_Store_Id') CREATE UNIQUE INDEX UX_CameraZones_Tenant_Store_Id ON dbo.CameraZoneConfigurations(TenantId,StoreId,Id);
GO
IF OBJECT_ID(N'dbo.CameraMotionRules',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CameraMotionRules(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CameraMotionRules PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CameraId BIGINT NOT NULL,RuleCode NVARCHAR(100) NOT NULL,RuleName NVARCHAR(150) NOT NULL,IsEnabled BIT NOT NULL,MinimumConfidence DECIMAL(5,4) NOT NULL,Sensitivity INT NOT NULL,MinimumDurationSeconds INT NOT NULL,CooldownSeconds INT NOT NULL,StartTime TIME NULL,EndTime TIME NULL,DaysOfWeek NVARCHAR(27) NOT NULL,EvidenceSnapshotEnabled BIT NOT NULL,EvidenceClipEnabled BIT NOT NULL,EvidencePreEventSeconds INT NOT NULL,EvidencePostEventSeconds INT NOT NULL,Severity TINYINT NOT NULL,CreateAlert BIT NOT NULL,RealtimeNotificationEnabled BIT NOT NULL,ZoneRequired BIT NOT NULL,ZoneId BIGINT NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_CameraMotionRules_Camera FOREIGN KEY(TenantId,StoreId,CameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),CONSTRAINT FK_CameraMotionRules_Zone FOREIGN KEY(TenantId,StoreId,ZoneId) REFERENCES dbo.CameraZoneConfigurations(TenantId,StoreId,Id),CONSTRAINT CK_CameraMotionRules_Confidence CHECK(MinimumConfidence BETWEEN 0 AND 1),CONSTRAINT CK_CameraMotionRules_Sensitivity CHECK(Sensitivity BETWEEN 1 AND 100),CONSTRAINT CK_CameraMotionRules_Durations CHECK(MinimumDurationSeconds BETWEEN 0 AND 86400 AND CooldownSeconds BETWEEN 0 AND 86400 AND EvidencePreEventSeconds BETWEEN 0 AND 300 AND EvidencePostEventSeconds BETWEEN 0 AND 300),CONSTRAINT CK_CameraMotionRules_Schedule CHECK((StartTime IS NULL AND EndTime IS NULL) OR (StartTime IS NOT NULL AND EndTime IS NOT NULL)),CONSTRAINT CK_CameraMotionRules_Severity CHECK(Severity BETWEEN 1 AND 3),CONSTRAINT CK_CameraMotionRules_Zone CHECK(ZoneRequired=0 OR ZoneId IS NOT NULL)
 );
 CREATE UNIQUE INDEX UX_CameraMotionRules_Tenant_Camera_Code ON dbo.CameraMotionRules(TenantId,CameraId,RuleCode);
 CREATE INDEX IX_CameraMotionRules_Runtime ON dbo.CameraMotionRules(TenantId,StoreId,CameraId,IsEnabled) INCLUDE(RuleCode,MinimumConfidence,Sensitivity,MinimumDurationSeconds,CooldownSeconds,ZoneId);
END;
GO
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'Cameras.ManageRules') INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) VALUES(2,N'Cameras.ManageRules',N'Manage tenant camera motion and health rules.',1,SYSUTCDATETIME());
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p WHERE p.Name=N'Cameras.ManageRules' AND r.Scope=2 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.1') INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.18.1',N'Camera Motion Phase C rule engine configuration',SYSUTCDATETIME(),SUSER_SNAME());
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.1')<>1 THROW 56229,'V1.18.1 must exist exactly once.',1;
GO
