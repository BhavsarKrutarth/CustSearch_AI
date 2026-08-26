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
