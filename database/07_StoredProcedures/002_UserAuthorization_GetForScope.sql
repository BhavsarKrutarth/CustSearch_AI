/*
==============================================================
Script        : 002_UserAuthorization_GetForScope.sql
Purpose       : Returns active roles and permissions only for the user's validated scope.
Safety        : Repeat-safe through CREATE OR ALTER.
==============================================================
*/
USE [CustSearch_AI];
GO

-- The tenant predicate is mandatory for tenant sessions and prevents cross-tenant permission reads.
CREATE OR ALTER PROCEDURE dbo.UserAuthorization_GetForScope
    @UserId BIGINT,
    @TenantId BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        role.Name AS RoleName,
        permission.Name AS PermissionName
    FROM dbo.Users AS appUser
    INNER JOIN dbo.UserRoles AS userRole ON userRole.UserId = appUser.Id
    INNER JOIN dbo.Roles AS role ON role.Id = userRole.RoleId AND role.IsActive = 1
    INNER JOIN dbo.RolePermissions AS rolePermission ON rolePermission.RoleId = role.Id
    INNER JOIN dbo.Permissions AS permission ON permission.Id = rolePermission.PermissionId AND permission.IsActive = 1
    WHERE appUser.Id = @UserId
      AND appUser.IsActive = 1
      AND
      (
          (appUser.Scope = 1 AND appUser.TenantId IS NULL AND @TenantId IS NULL)
          OR (appUser.Scope = 2 AND appUser.TenantId = @TenantId AND role.TenantId = @TenantId)
      )
    ORDER BY role.Name, permission.Name;
END;
GO
