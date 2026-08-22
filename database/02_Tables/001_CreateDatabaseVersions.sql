/*
==============================================================
Script        : 001_CreateDatabaseVersions.sql
Purpose       : Creates the version ledger for SQL deployments.
Used By       : Deployment scripts and operational diagnostics.
Safety        : Repeat-safe; preserves all existing version rows.
==============================================================
*/
USE [CustSearch_AI];
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.DatabaseVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseVersions
    (
        VersionId BIGINT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_DatabaseVersions PRIMARY KEY,
        VersionNumber NVARCHAR(50) NOT NULL,
        Description NVARCHAR(250) NOT NULL,
        AppliedUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_DatabaseVersions_AppliedUtc DEFAULT SYSUTCDATETIME(),
        AppliedBy NVARCHAR(100) NOT NULL
            CONSTRAINT DF_DatabaseVersions_AppliedBy DEFAULT ORIGINAL_LOGIN()
    );
END;
GO
