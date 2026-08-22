/*
==============================================================
Script        : 001_RecordFoundationVersion.sql
Purpose       : Records successful installation of foundation schema V1.0.0.
Safety        : Repeat-safe.
==============================================================
*/
USE [CustSearch_AI];
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.DatabaseVersions
    WHERE VersionNumber = N'V1.0.0'
)
BEGIN
    INSERT dbo.DatabaseVersions (VersionNumber, Description, AppliedUtc, AppliedBy)
    VALUES (N'V1.0.0', N'CustSearch AI foundation database and version ledger', SYSUTCDATETIME(), ORIGINAL_LOGIN());
END;
GO
