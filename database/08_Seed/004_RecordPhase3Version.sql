/*
==============================================================
Script        : 004_RecordPhase3Version.sql
Purpose       : Records successful Phase 3 authorization schema deployment.
Safety        : Inserts the version ledger row only once.
==============================================================
*/
USE [CustSearch_AI];
GO

-- The version ledger makes repeat deployments observable without duplicating history.
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber = N'V1.2.0')
BEGIN
    INSERT INTO dbo.DatabaseVersions (VersionNumber, Description, AppliedUtc, AppliedBy)
    VALUES (N'V1.2.0', N'Phase 3 authorization roles and permissions', SYSUTCDATETIME(), SUSER_SNAME());
END;
GO
