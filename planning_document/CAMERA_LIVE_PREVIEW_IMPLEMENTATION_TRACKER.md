# Camera Live Preview Implementation Tracker

- Last updated: 2026-08-26 (Asia/Calcutta)
- Branch: `audit/all-phases-database-smoke`
- Scope: tenant/store/user-authorized office-camera preview with continuous RTSP capture
- Database target: `KRUTARTH-BHAVSA/CustSearch_AI`
- Database version applied: `V1.16.2`

## Current outcome

The first secure preview increment is implemented. Angular requests authenticated JPEG frames through
the ASP.NET API. The API revalidates tenant, store, `Cameras.Preview`, explicit user-camera grant and
short-lived session ownership on every request. Python maintains a bounded continuous RTSP capture
with reconnect/backoff and returns only an in-memory JPEG to the internal API. The browser never
receives the camera IP, RTSP URL, username or password.

This is a secure low-frame-rate preview (default refresh 750 ms), not a WebRTC high-FPS stream.
The authorization/database model can be reused by a later WebRTC media gateway.

## Completed work

### Database

- Added tenant permissions `Cameras.Preview` and `Cameras.TrackingView`.
- Granted both permissions to TenantAdmin, TenantOwner, ShopOwner and CameraOperator roles.
- Added `CameraUserPreviewGrants` for explicit user-camera live/tracking/control access.
- Added `CameraPreviewSessions` for short-lived audited sessions without tokens or media secrets.
- Added tenant/user/camera/session indexes, foreign keys and access/expiry check constraints.
- Applied the migration twice and ran the verifier after the live database upgrade to prove repeat safety.
- Applied `OfficeCameraPreviewGrant.sql` repeatedly to prove repeat-safe office-user assignment.
- Live office grant: tenant `10019`, store `11`, camera `3`, user `10038`.
- No RTSP URL or credential was written to SQL.

### Backend

- Added camera grant list/save/remove operations.
- Added preview start/frame/end endpoints.
- Added server-side tenant/store/user/grant/session revalidation.
- Revoking a grant ends the user's active sessions for that camera.
- Added no-cache frame responses and safe frame metadata headers.
- Added an internal Python frame client protected by `X-CustSearch-AI-Key`.

### Python

- Added one bounded continuous capture worker per opaque camera reference.
- Added RTSP reconnect with exponential backoff and a 60-second idle shutdown.
- Added latest-frame age validation and maximum active source limit.
- JPEGs remain in memory and are not persisted or logged.
- Added authenticated `POST /v1/cctv/cameras/frame`.

### Angular

- Added secure Start/Stop Preview controls.
- Added authenticated blob polling and safe object-URL cleanup.
- Added runtime disabled/unavailable states.
- Added user-wise preview grant management for camera managers.
- Added separate `Cameras.Preview` permission handling.

### Runtime configuration

- Generated one 48-byte random shared preview service key.
- Stored matching values only in the Windows User environment as
  `CUSTSEARCH_AI_API_KEY` and `CctvPreview__ApiKey`.
- Set `CctvPreview__Enabled=true` and the loopback Python base URL in the User environment.
- The secret value was not printed, committed or written to this document.

## Tests observed

- Isolated .NET Release build: PASS, 0 warnings, 0 errors.
- .NET unit: 104/104 PASS.
- Full .NET integration/API: 233/233 PASS; camera-focused subset 17/17 PASS.
- Angular lint: PASS.
- Angular unit: 84/84 PASS.
- Full Playwright Chromium: 50/50 PASS; camera-focused subset 3/3 PASS.
- Python: Ruff PASS and 11/11 pytest PASS.
- Angular production build: PASS (existing non-blocking 151-byte admin-shell style warning).
- SQL migration/verifier: PASS.
- Office preview-grant script executed twice: PASS both times.

Three access profiles covered:

1. Explicitly assigned office CameraOperator can start a session and receive a frame.
2. Same-tenant user assigned only to another store cannot receive the office-camera grant.
3. Other-tenant/no-camera user cannot resolve or start the office-camera preview.

## Remaining physical-camera gate

- `CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP` is not configured in Process, User or Machine scope.
- An authorized operator must set it locally without sending the value in chat or committing it.
- Restart Python and the ASP.NET API so they inherit the new environment configuration.
- Confirm Python `/health/live`, then the authenticated frame endpoint.
- Login as `office.camera.operator`, select `Office Hikvision Camera`, and press **Start preview**.
- Run a 30-minute preview/reconnect soak and confirm no credentials appear in API/Python/IIS logs.

The actual physical frame remains `BLOCKED`, not failed, until the authorized RTSP URL is configured.

## Runtime commands

Set the RTSP value only in a private local PowerShell session or approved secret store:

```powershell
[Environment]::SetEnvironmentVariable(
  'CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP',
  'rtsp://<authorized-user>:<password>@<camera-ip>:554/Streaming/Channels/102',
  'User')
```

Never paste the real value into source, SQL, logs, screenshots or this document. Restart API/Python
afterward. The canonical database commands are:

```powershell
Set-Location 'D:\Project\AdminCore\CustSearch_AI\CustSearch_AI\database'
sqlcmd -S KRUTARTH-BHAVSA -d CustSearch_AI -E -N -C -b -i .\run-phase-camera-preview.sql
sqlcmd -S KRUTARTH-BHAVSA -d CustSearch_AI -E -N -C -b -i .\verify-camera-preview.sql
sqlcmd -S KRUTARTH-BHAVSA -d CustSearch_AI -E -N -C -b -i .\10_TestData\OfficeCameraPreviewGrant.sql
```

## Full canonical database upgrade script

Canonical file: `database/09_Upgrade/V1.16.2_UserCameraLivePreview.sql`

```sql
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
```

## Later WebRTC upgrade

After the current office-camera preview passes physical soak testing, replace JPEG polling with an
internal WebRTC media gateway for lower latency and higher FPS. Keep the same explicit grants,
short-lived session ownership checks, tenant/store isolation and no-secret browser boundary.

## Full office user-camera assignment script

Canonical file: `database/10_TestData/OfficeCameraPreviewGrant.sql`

```sql
:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @UserId BIGINT=(SELECT TOP(1)Id FROM dbo.Users WHERE NormalizedUserName=N'OFFICE.CAMERA.OPERATOR' AND IsActive=1);
DECLARE @CameraId BIGINT=(SELECT TOP(1)Id FROM dbo.Cameras WHERE RtspConfigurationReference=N'env:CUSTSEARCH_CAMERA_OFFICE_ENTRY01_RTSP' AND IsActive=1);
IF @UserId IS NULL THROW 55230,'Active office.camera.operator was not found.',1;
IF @CameraId IS NULL THROW 55231,'Active office-entry camera reference was not found.',1;
DECLARE @TenantId BIGINT,@StoreId BIGINT;SELECT @TenantId=TenantId,@StoreId=StoreId FROM dbo.Cameras WHERE Id=@CameraId;
DECLARE @AssignedByUserId BIGINT=(SELECT TOP(1)userRole.UserId FROM dbo.UserRoles userRole JOIN dbo.Roles role ON role.Id=userRole.RoleId JOIN dbo.Users userRow ON userRow.Id=userRole.UserId WHERE role.TenantId=@TenantId AND role.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER') AND role.IsActive=1 AND userRow.IsActive=1 ORDER BY userRole.UserId);
IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Id=@UserId AND TenantId=@TenantId) THROW 55232,'Office user and camera tenant mismatch.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.UserStoreAssignments WHERE UserId=@UserId AND TenantId=@TenantId AND StoreId=@StoreId) THROW 55233,'Office user is not assigned to the camera store.',1;
IF @AssignedByUserId IS NULL THROW 55234,'Active tenant administrator was not found.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.CameraUserPreviewGrants WHERE TenantId=@TenantId AND CameraId=@CameraId AND UserId=@UserId)
    INSERT dbo.CameraUserPreviewGrants(TenantId,StoreId,CameraId,UserId,CanViewLive,CanViewTracking,CanControl,ValidUntilUtc,IsActive,AssignedByUserId,CreatedUtc,UpdatedUtc)
    VALUES(@TenantId,@StoreId,@CameraId,@UserId,1,1,0,NULL,1,@AssignedByUserId,SYSUTCDATETIME(),SYSUTCDATETIME());
ELSE
    UPDATE dbo.CameraUserPreviewGrants SET CanViewLive=1,CanViewTracking=1,CanControl=0,ValidUntilUtc=NULL,IsActive=1,AssignedByUserId=@AssignedByUserId,UpdatedUtc=SYSUTCDATETIME() WHERE TenantId=@TenantId AND CameraId=@CameraId AND UserId=@UserId;
SELECT N'PASS' AS Result,@TenantId AS TenantId,@StoreId AS StoreId,@CameraId AS CameraId,@UserId AS UserId;
GO
```
