/*
==============================================================
Script        : 003_Authorization_Indexes.sql
Purpose       : Adds unique names and efficient authorization lookup paths.
Safety        : Creates only missing indexes.
==============================================================
*/
USE [CustSearch_AI];
GO

-- One role name may exist once at platform level and once inside each tenant.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Roles') AND name = N'UX_Roles_TenantId_NormalizedName')
    CREATE UNIQUE INDEX UX_Roles_TenantId_NormalizedName ON dbo.Roles (TenantId, NormalizedName);
GO

-- Active-role filtering uses scope and status on nearly every authorization lookup.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Roles') AND name = N'IX_Roles_Scope_IsActive')
    CREATE INDEX IX_Roles_Scope_IsActive ON dbo.Roles (Scope, IsActive) INCLUDE (TenantId, NormalizedName);
GO

-- Permission names are stable contract keys and must never be duplicated.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Permissions') AND name = N'UX_Permissions_Name')
    CREATE UNIQUE INDEX UX_Permissions_Name ON dbo.Permissions (Name);
GO

-- Active permissions are loaded by scope when building an authorization session.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Permissions') AND name = N'IX_Permissions_Scope_IsActive')
    CREATE INDEX IX_Permissions_Scope_IsActive ON dbo.Permissions (Scope, IsActive) INCLUDE (Name);
GO

-- Reverse role lookup supports administration pages that list a role's members.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.UserRoles') AND name = N'IX_UserRoles_RoleId')
    CREATE INDEX IX_UserRoles_RoleId ON dbo.UserRoles (RoleId) INCLUDE (UserId, AssignedUtc);
GO

-- Audit lookup identifies assignments made by an administrator without imposing filtered-index session requirements.
IF EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.UserRoles')
      AND name = N'IX_UserRoles_AssignedByUserId'
      AND filter_definition IS NOT NULL
)
    DROP INDEX IX_UserRoles_AssignedByUserId ON dbo.UserRoles;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.UserRoles') AND name = N'IX_UserRoles_AssignedByUserId')
    CREATE INDEX IX_UserRoles_AssignedByUserId ON dbo.UserRoles (AssignedByUserId) INCLUDE (UserId, RoleId, AssignedUtc);
GO

-- Reverse permission lookup supports finding every role that grants a capability.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RolePermissions') AND name = N'IX_RolePermissions_PermissionId')
    CREATE INDEX IX_RolePermissions_PermissionId ON dbo.RolePermissions (PermissionId, RoleId);
GO
