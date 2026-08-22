/*
==============================================================
Script        : 002_TenantAuthentication_Indexes.sql
Purpose       : Adds Phase 2 tenant/auth uniqueness and lookup indexes.
Safety        : Repeat-safe.
==============================================================
*/
USE [CustSearch_AI];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Tenants') AND name = N'UX_Tenants_TenantCode')
    CREATE UNIQUE INDEX UX_Tenants_TenantCode ON dbo.Tenants (TenantCode);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'UX_Users_TenantId_NormalizedUserName')
    CREATE UNIQUE INDEX UX_Users_TenantId_NormalizedUserName ON dbo.Users (TenantId, NormalizedUserName);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'UX_Users_TenantId_NormalizedEmail')
    CREATE UNIQUE INDEX UX_Users_TenantId_NormalizedEmail ON dbo.Users (TenantId, NormalizedEmail);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'IX_Users_Scope')
    CREATE INDEX IX_Users_Scope ON dbo.Users (Scope);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RefreshTokens') AND name = N'UX_RefreshTokens_TokenHash')
    CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens (TokenHash);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RefreshTokens') AND name = N'IX_RefreshTokens_UserId_FamilyId')
    CREATE INDEX IX_RefreshTokens_UserId_FamilyId ON dbo.RefreshTokens (UserId, FamilyId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RefreshTokens') AND name = N'IX_RefreshTokens_ExpiresUtc')
    CREATE INDEX IX_RefreshTokens_ExpiresUtc ON dbo.RefreshTokens (ExpiresUtc);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuthenticationEvents') AND name = N'IX_AuthenticationEvents_TenantId_OccurredUtc')
    CREATE INDEX IX_AuthenticationEvents_TenantId_OccurredUtc ON dbo.AuthenticationEvents (TenantId, OccurredUtc);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuthenticationEvents') AND name = N'IX_AuthenticationEvents_UserId_OccurredUtc')
    CREATE INDEX IX_AuthenticationEvents_UserId_OccurredUtc ON dbo.AuthenticationEvents (UserId, OccurredUtc);
GO
