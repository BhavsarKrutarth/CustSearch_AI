/*
==============================================================
Script        : 001_DatabaseVersions_Indexes.sql
Purpose       : Prevents a database version from being recorded twice.
Safety        : Repeat-safe.
==============================================================
*/
USE [CustSearch_AI];
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_DatabaseVersions_VersionNumber'
      AND object_id = OBJECT_ID(N'dbo.DatabaseVersions')
)
BEGIN
    CREATE UNIQUE INDEX UX_DatabaseVersions_VersionNumber
        ON dbo.DatabaseVersions (VersionNumber);
END;
GO
