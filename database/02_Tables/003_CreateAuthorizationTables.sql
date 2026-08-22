/*
==============================================================
Script        : 003_CreateAuthorizationTables.sql
Purpose       : Creates Phase 3 role and permission storage with tenant-safe ownership.
Safety        : Repeat-safe; never drops or truncates existing objects.
==============================================================
*/
USE [CustSearch_AI];
GO

SET XACT_ABORT ON;
GO

-- Roles stores either platform-wide roles or roles owned by exactly one tenant.
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
        TenantId BIGINT NULL,
        Scope TINYINT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        NormalizedName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NOT NULL,
        IsSystem BIT NOT NULL CONSTRAINT DF_Roles_IsSystem DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Roles_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Roles_Tenants_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT CK_Roles_ScopeTenant CHECK
            ((Scope = 1 AND TenantId IS NULL) OR (Scope = 2 AND TenantId IS NOT NULL))
    );
END;
GO

-- Permissions is the stable capability catalog shared by server policies and the UI.
IF OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Permissions PRIMARY KEY,
        Scope TINYINT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(300) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Permissions_IsActive DEFAULT (1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Permissions_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Permissions_Scope CHECK (Scope IN (1, 2))
    );
END;
GO

-- UserRoles records role assignments without duplicating the same user-role pair.
IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles
    (
        UserId BIGINT NOT NULL,
        RoleId BIGINT NOT NULL,
        AssignedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_UserRoles_AssignedUtc DEFAULT SYSUTCDATETIME(),
        AssignedByUserId BIGINT NULL,
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Users_AssignedByUserId FOREIGN KEY (AssignedByUserId) REFERENCES dbo.Users (Id)
    );
END;
GO

-- RolePermissions records each capability granted to a role once.
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        RoleId BIGINT NOT NULL,
        PermissionId BIGINT NOT NULL,
        CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
        CONSTRAINT FK_RolePermissions_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RolePermissions_Permissions_PermissionId
            FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions (Id) ON DELETE CASCADE
    );
END;
GO

-- This trigger blocks direct SQL assignments between different tenants or authorization scopes.
CREATE OR ALTER TRIGGER dbo.TR_UserRoles_ValidateOwnership
ON dbo.UserRoles
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS assignment
        INNER JOIN dbo.Users AS appUser ON appUser.Id = assignment.UserId
        INNER JOIN dbo.Roles AS role ON role.Id = assignment.RoleId
        WHERE appUser.Scope <> role.Scope
           OR ISNULL(appUser.TenantId, 0) <> ISNULL(role.TenantId, 0)
    )
    BEGIN
        THROW 51001, 'A user can only receive a role from the same platform or tenant scope.', 1;
    END;
END;
GO

-- This trigger prevents platform roles from receiving tenant permissions and vice versa.
CREATE OR ALTER TRIGGER dbo.TR_RolePermissions_ValidateScope
ON dbo.RolePermissions
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS grantRow
        INNER JOIN dbo.Roles AS role ON role.Id = grantRow.RoleId
        INNER JOIN dbo.Permissions AS permission ON permission.Id = grantRow.PermissionId
        WHERE role.Scope <> permission.Scope
    )
    BEGIN
        THROW 51002, 'A role can only receive permissions from the same scope.', 1;
    END;
END;
GO
