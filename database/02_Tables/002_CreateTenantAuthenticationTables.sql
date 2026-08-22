/*
==============================================================
Script        : 002_CreateTenantAuthenticationTables.sql
Purpose       : Creates the Phase 2 tenant and authentication schema.
Safety        : Repeat-safe; never drops or truncates existing objects.
==============================================================
*/
USE [CustSearch_AI];
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Tenants PRIMARY KEY,
        TenantCode NVARCHAR(30) NOT NULL,
        LegalName NVARCHAR(200) NOT NULL,
        DisplayName NVARCHAR(150) NOT NULL,
        TimeZone NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_Tenants_IsActive DEFAULT (1),
        IsSuspended BIT NOT NULL
            CONSTRAINT DF_Tenants_IsSuspended DEFAULT (0),
        CreatedUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_Tenants_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Tenants_ActiveSuspended
            CHECK (NOT (IsActive = 0 AND IsSuspended = 1))
    );
END;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Users PRIMARY KEY,
        TenantId BIGINT NULL,
        Scope TINYINT NOT NULL,
        UserName NVARCHAR(100) NOT NULL,
        NormalizedUserName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(254) NOT NULL,
        NormalizedEmail NVARCHAR(254) NOT NULL,
        DisplayName NVARCHAR(150) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        SecurityStamp NVARCHAR(64) NOT NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_Users_CreatedUtc DEFAULT SYSUTCDATETIME(),
        LastLoginUtc DATETIME2(7) NULL,
        CONSTRAINT FK_Users_Tenants_TenantId
            FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT CK_Users_ScopeTenant
            CHECK ((Scope = 1 AND TenantId IS NULL) OR (Scope = 2 AND TenantId IS NOT NULL))
    );
END;
GO

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_RefreshTokens PRIMARY KEY,
        UserId BIGINT NOT NULL,
        TokenHash CHAR(64) NOT NULL,
        FamilyId UNIQUEIDENTIFIER NOT NULL,
        IssuedSecurityStamp NVARCHAR(64) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL,
        ExpiresUtc DATETIME2(7) NOT NULL,
        RevokedUtc DATETIME2(7) NULL,
        RevokedReason NVARCHAR(100) NULL,
        ReplacedByTokenHash CHAR(64) NULL,
        CreatedByIp VARCHAR(64) NULL,
        RevokedByIp VARCHAR(64) NULL,
        CONSTRAINT FK_RefreshTokens_Users_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT CK_RefreshTokens_Expiry
            CHECK (ExpiresUtc > CreatedUtc),
        CONSTRAINT CK_RefreshTokens_Revocation
            CHECK ((RevokedUtc IS NULL AND RevokedReason IS NULL) OR
                   (RevokedUtc IS NOT NULL AND RevokedReason IS NOT NULL))
    );
END;
GO

IF OBJECT_ID(N'dbo.AuthenticationEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuthenticationEvents
    (
        Id BIGINT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_AuthenticationEvents PRIMARY KEY,
        UserId BIGINT NULL,
        TenantId BIGINT NULL,
        EventType NVARCHAR(60) NOT NULL,
        IsSuccess BIT NOT NULL,
        FailureCode NVARCHAR(60) NULL,
        OccurredUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_AuthenticationEvents_OccurredUtc DEFAULT SYSUTCDATETIME(),
        IpAddress VARCHAR(64) NULL,
        CorrelationId VARCHAR(64) NOT NULL
    );
END;
GO
