:on error exit
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.CameraUserPreviewGrants',N'U') IS NULL THROW 55220,'CameraUserPreviewGrants missing.',1;
IF OBJECT_ID(N'dbo.CameraPreviewSessions',N'U') IS NULL THROW 55221,'CameraPreviewSessions missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.16.2') THROW 55222,'V1.16.2 missing.',1;
IF EXISTS(SELECT 1 FROM dbo.CameraUserPreviewGrants grantRow LEFT JOIN dbo.Cameras camera ON camera.Id=grantRow.CameraId AND camera.TenantId=grantRow.TenantId AND camera.StoreId=grantRow.StoreId WHERE camera.Id IS NULL) THROW 55223,'Cross-scope camera preview grant found.',1;
IF EXISTS(SELECT 1 FROM dbo.CameraUserPreviewGrants grantRow LEFT JOIN dbo.Users userRow ON userRow.Id=grantRow.UserId AND userRow.TenantId=grantRow.TenantId WHERE userRow.Id IS NULL) THROW 55224,'Cross-tenant preview user grant found.',1;
IF EXISTS(SELECT 1 FROM dbo.CameraPreviewSessions WHERE ExpiresUtc<=CreatedUtc OR Status NOT BETWEEN 1 AND 3) THROW 55225,'Invalid preview session row found.',1;
SELECT N'PASS' AS Result,N'V1.16.2 user-camera live preview schema verified' AS Detail;
GO
