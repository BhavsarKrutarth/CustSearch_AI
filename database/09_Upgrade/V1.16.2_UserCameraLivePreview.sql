:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON; SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Cameras',N'U') IS NULL OR OBJECT_ID(N'dbo.Users',N'U') IS NULL OR OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL
    THROW 55210,'Camera preview prerequisites are missing.',1;
GO

IF OBJECT_ID(N'dbo.CameraUserPreviewGrants',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CameraUserPreviewGrants
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CameraUserPreviewGrants PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        CameraId BIGINT NOT NULL,
        UserId BIGINT NOT NULL,
        CanViewLive BIT NOT NULL CONSTRAINT DF_CameraUserPreviewGrants_Live DEFAULT(1),
        CanViewTracking BIT NOT NULL CONSTRAINT DF_CameraUserPreviewGrants_Tracking DEFAULT(0),
        CanControl BIT NOT NULL CONSTRAINT DF_CameraUserPreviewGrants_Control DEFAULT(0),
        ValidUntilUtc DATETIME2(7) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_CameraUserPreviewGrants_Active DEFAULT(1),
        AssignedByUserId BIGINT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        UpdatedUtc DATETIME2(7) NOT NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_CameraUserPreviewGrants_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_CameraUserPreviewGrants_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
        CONSTRAINT FK_CameraUserPreviewGrants_Cameras FOREIGN KEY(CameraId) REFERENCES dbo.Cameras(Id),
        CONSTRAINT FK_CameraUserPreviewGrants_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_CameraUserPreviewGrants_AssignedBy FOREIGN KEY(AssignedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_CameraUserPreviewGrants_Access CHECK(CanViewLive=1 OR (CanViewTracking=0 AND CanControl=0))
    );
    CREATE UNIQUE INDEX UX_CameraUserPreviewGrants_CameraUser ON dbo.CameraUserPreviewGrants(TenantId,CameraId,UserId);
    CREATE INDEX IX_CameraUserPreviewGrants_UserAccess ON dbo.CameraUserPreviewGrants(TenantId,UserId,IsActive,ValidUntilUtc) INCLUDE(StoreId,CameraId,CanViewLive,CanViewTracking,CanControl);
END;
GO

IF OBJECT_ID(N'dbo.CameraPreviewSessions',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CameraPreviewSessions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CameraPreviewSessions PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        CameraId BIGINT NOT NULL,
        UserId BIGINT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        ExpiresUtc DATETIME2(7) NOT NULL,
        LastAccessedUtc DATETIME2(7) NOT NULL,
        EndedUtc DATETIME2(7) NULL,
        Status TINYINT NOT NULL,
        CONSTRAINT FK_CameraPreviewSessions_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_CameraPreviewSessions_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
        CONSTRAINT FK_CameraPreviewSessions_Cameras FOREIGN KEY(CameraId) REFERENCES dbo.Cameras(Id),
        CONSTRAINT FK_CameraPreviewSessions_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_CameraPreviewSessions_Status CHECK(Status BETWEEN 1 AND 3),
        CONSTRAINT CK_CameraPreviewSessions_Expiry CHECK(ExpiresUtc>CreatedUtc)
    );
    CREATE INDEX IX_CameraPreviewSessions_UserStatus ON dbo.CameraPreviewSessions(TenantId,UserId,Status,ExpiresUtc) INCLUDE(CameraId,StoreId);
    CREATE INDEX IX_CameraPreviewSessions_CameraStatus ON dbo.CameraPreviewSessions(CameraId,Status,ExpiresUtc) INCLUDE(UserId);
END;
GO

DECLARE @Permissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
INSERT @Permissions VALUES
    (N'Cameras.Preview',N'Open a short-lived live preview for an explicitly assigned tenant/store camera.'),
    (N'Cameras.TrackingView',N'View anonymous live tracking overlays for an explicitly assigned camera.');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 2,p.Name,p.Description,1,SYSUTCDATETIME() FROM @Permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions existing WHERE existing.Name=p.Name);

INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT role.Id,permission.Id
FROM dbo.Roles role CROSS JOIN dbo.Permissions permission
WHERE role.Scope=2 AND role.IsActive=1 AND permission.Name IN(N'Cameras.Preview',N'Cameras.TrackingView')
  AND role.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER',N'CAMERAOPERATOR')
  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions existing WHERE existing.RoleId=role.Id AND existing.PermissionId=permission.Id);
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.16.2')
    INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
    VALUES(N'V1.16.2',N'User-camera live preview grants and short-lived audited preview sessions',SYSUTCDATETIME(),SUSER_SNAME());
GO

IF OBJECT_ID(N'dbo.CameraUserPreviewGrants',N'U') IS NULL OR OBJECT_ID(N'dbo.CameraPreviewSessions',N'U') IS NULL
    THROW 55211,'Camera preview tables were not created.',1;
IF (SELECT COUNT(*) FROM dbo.Permissions WHERE Name IN(N'Cameras.Preview',N'Cameras.TrackingView'))<>2
    THROW 55212,'Camera preview permissions are incomplete.',1;
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.16.2')<>1
    THROW 55213,'V1.16.2 version row must exist exactly once.',1;
GO
