/* Camera Motion / Tenant Storage - Phase E: encrypted evidence metadata and tenant quota. */
:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON;SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.2') THROW 56240,'Tenant evidence storage requires V1.18.2.',1;
GO
IF OBJECT_ID(N'dbo.TenantStoragePolicies',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.TenantStoragePolicies(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TenantStoragePolicies PRIMARY KEY,TenantId BIGINT NOT NULL,StorageEnabled BIT NOT NULL,StorageQuotaBytes BIGINT NOT NULL,DefaultRetentionDays INT NOT NULL,MotionSnapshotRetentionDays INT NOT NULL,MotionClipRetentionDays INT NOT NULL,FalsePositiveRetentionDays INT NOT NULL,UnreviewedEvidenceRetentionDays INT NOT NULL,ConfirmedIncidentRetentionDays INT NOT NULL,WarningPercent INT NOT NULL,CriticalPercent INT NOT NULL,AllowSnapshots BIT NOT NULL,AllowMotionClips BIT NOT NULL,AutoCleanupEnabled BIT NOT NULL,QuotaPressurePolicy TINYINT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,CONSTRAINT FK_TenantStoragePolicies_Tenant FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT CK_TenantStoragePolicies_Quota CHECK(StorageQuotaBytes BETWEEN 1048576 AND 10995116277760),CONSTRAINT CK_TenantStoragePolicies_Warnings CHECK(WarningPercent BETWEEN 1 AND 99 AND CriticalPercent>WarningPercent AND CriticalPercent<=100),CONSTRAINT CK_TenantStoragePolicies_Retention CHECK(DefaultRetentionDays BETWEEN 1 AND 3650 AND MotionSnapshotRetentionDays BETWEEN 1 AND 3650 AND MotionClipRetentionDays BETWEEN 1 AND 3650 AND FalsePositiveRetentionDays BETWEEN 1 AND 3650 AND UnreviewedEvidenceRetentionDays BETWEEN 1 AND 3650 AND ConfirmedIncidentRetentionDays BETWEEN 1 AND 3650));
 CREATE UNIQUE INDEX UX_TenantStoragePolicies_Tenant ON dbo.TenantStoragePolicies(TenantId);
END;
GO
IF OBJECT_ID(N'dbo.TenantStorageUsage',N'U') IS NULL
 CREATE TABLE dbo.TenantStorageUsage(TenantId BIGINT NOT NULL CONSTRAINT PK_TenantStorageUsage PRIMARY KEY,QuotaBytes BIGINT NOT NULL,UsedBytes BIGINT NOT NULL,SnapshotBytes BIGINT NOT NULL,MotionClipBytes BIGINT NOT NULL,SecurityEvidenceBytes BIGINT NOT NULL,OtherBytes BIGINT NOT NULL,LastCalculatedUtc DATETIME2(7) NOT NULL,LastCleanupUtc DATETIME2(7) NULL,RowVersion ROWVERSION NOT NULL,CONSTRAINT FK_TenantStorageUsage_Tenant FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT CK_TenantStorageUsage_Bytes CHECK(QuotaBytes>=0 AND UsedBytes>=0 AND SnapshotBytes>=0 AND MotionClipBytes>=0 AND SecurityEvidenceBytes>=0 AND OtherBytes>=0 AND UsedBytes=SnapshotBytes+MotionClipBytes+SecurityEvidenceBytes+OtherBytes AND UsedBytes<=QuotaBytes));
GO
IF OBJECT_ID(N'dbo.CameraEvidence',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CameraEvidence(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CameraEvidence PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,CameraId BIGINT NOT NULL,MotionEventId BIGINT NULL,SecurityIncidentId BIGINT NULL,EvidenceType TINYINT NOT NULL,StorageObjectKey NVARCHAR(500) NOT NULL,FileSizeBytes BIGINT NOT NULL,ContentType NVARCHAR(100) NOT NULL,ContentHash BINARY(32) NOT NULL,CapturedUtc DATETIME2(7) NOT NULL,RetentionUntilUtc DATETIME2(7) NOT NULL,IsRestricted BIT NOT NULL,IsPinned BIT NOT NULL,DeletedUtc DATETIME2(7) NULL,DeleteReason NVARCHAR(200) NULL,ServiceId NVARCHAR(100) NOT NULL,SourceEventId NVARCHAR(150) NOT NULL,IdempotencyKey NVARCHAR(150) NOT NULL,IngestionHash CHAR(64) NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,RowVersion ROWVERSION NOT NULL,CONSTRAINT FK_CameraEvidence_Camera FOREIGN KEY(TenantId,StoreId,CameraId) REFERENCES dbo.Cameras(TenantId,StoreId,Id),CONSTRAINT CK_CameraEvidence_Type CHECK(EvidenceType BETWEEN 1 AND 6),CONSTRAINT CK_CameraEvidence_Size CHECK(FileSizeBytes>0),CONSTRAINT CK_CameraEvidence_Retention CHECK(RetentionUntilUtc>=CapturedUtc),CONSTRAINT CK_CameraEvidence_Key CHECK(StorageObjectKey<>N'' AND StorageObjectKey NOT LIKE N'%..%' AND StorageObjectKey NOT LIKE N'%:%' AND LEFT(StorageObjectKey,1) NOT IN(N'/',N'\')));
 CREATE UNIQUE INDEX UX_CameraEvidence_Service_Idempotency ON dbo.CameraEvidence(ServiceId,IdempotencyKey);
 CREATE INDEX IX_CameraEvidence_Tenant_Camera_Captured ON dbo.CameraEvidence(TenantId,StoreId,CameraId,CapturedUtc DESC);
 CREATE INDEX IX_CameraEvidence_Retention ON dbo.CameraEvidence(RetentionUntilUtc,DeletedUtc,IsPinned,Id) INCLUDE(TenantId,StoreId,CameraId,StorageObjectKey,FileSizeBytes,EvidenceType);
END;
GO
IF COL_LENGTH(N'dbo.SecurityIncidentEvidence',N'FileSizeBytes') IS NULL ALTER TABLE dbo.SecurityIncidentEvidence ADD FileSizeBytes BIGINT NOT NULL CONSTRAINT DF_SecurityEvidence_FileSize DEFAULT(0) WITH VALUES;
GO
INSERT dbo.TenantStoragePolicies(TenantId,StorageEnabled,StorageQuotaBytes,DefaultRetentionDays,MotionSnapshotRetentionDays,MotionClipRetentionDays,FalsePositiveRetentionDays,UnreviewedEvidenceRetentionDays,ConfirmedIncidentRetentionDays,WarningPercent,CriticalPercent,AllowSnapshots,AllowMotionClips,AutoCleanupEnabled,QuotaPressurePolicy,CreatedUtc,UpdatedUtc)
SELECT t.Id,1,2147483648,15,15,15,3,15,30,80,90,1,1,1,1,SYSUTCDATETIME(),SYSUTCDATETIME() FROM dbo.Tenants t WHERE NOT EXISTS(SELECT 1 FROM dbo.TenantStoragePolicies p WHERE p.TenantId=t.Id);
INSERT dbo.TenantStorageUsage(TenantId,QuotaBytes,UsedBytes,SnapshotBytes,MotionClipBytes,SecurityEvidenceBytes,OtherBytes,LastCalculatedUtc)
SELECT p.TenantId,p.StorageQuotaBytes,0,0,0,0,0,SYSUTCDATETIME() FROM dbo.TenantStoragePolicies p WHERE NOT EXISTS(SELECT 1 FROM dbo.TenantStorageUsage u WHERE u.TenantId=p.TenantId);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'Storage.ViewUsage') INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) VALUES(2,N'Storage.ViewUsage',N'View tenant evidence storage policy and aggregate usage.',1,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'Cameras.ViewEvents') INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) VALUES(2,N'Cameras.ViewEvents',N'View store-scoped camera event evidence metadata.',1,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Name=N'Platform.TenantStorage.Manage') INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) VALUES(1,N'Platform.TenantStorage.Manage',N'Manage licensed tenant evidence storage policy and quota.',1,SYSUTCDATETIME());
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p WHERE p.Name IN(N'Storage.ViewUsage',N'Cameras.ViewEvents') AND r.Scope=2 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p WHERE p.Name=N'Platform.TenantStorage.Manage' AND r.Scope=1 AND r.NormalizedName IN(N'PLATFORMSUPERADMIN',N'PLATFORMOPERATIONSADMIN') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.3') INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.18.3',N'Camera Motion Phase E tenant evidence storage',SYSUTCDATETIME(),SUSER_SNAME());
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.18.3')<>1 THROW 56249,'V1.18.3 must exist exactly once.',1;
GO
