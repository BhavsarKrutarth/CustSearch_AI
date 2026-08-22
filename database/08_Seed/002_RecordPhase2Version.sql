/*
==============================================================
Script        : 002_RecordPhase2Version.sql
Purpose       : Records successful installation of Phase 2 schema V1.1.0.
Safety        : Repeat-safe.
==============================================================
*/
USE [CustSearch_AI];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber = N'V1.1.0')
BEGIN
    INSERT dbo.DatabaseVersions (VersionNumber, Description, AppliedUtc, AppliedBy)
    VALUES (N'V1.1.0', N'Multi-tenant identity and rotating refresh-token authentication schema', SYSUTCDATETIME(), ORIGINAL_LOGIN());
END;
GO
