/*
==============================================================
Script        : 006_RecordPhase4Version.sql
Purpose       : Records successful Phase 4 platform tenant management deployment.
Safety        : Inserts the version ledger row only once.
==============================================================
*/
USE [CustSearch_AI];
GO

-- The version ledger makes repeat deployments observable without duplicating history.
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber = N'V1.3.0')
BEGIN
    INSERT INTO dbo.DatabaseVersions (VersionNumber, Description, AppliedUtc, AppliedBy)
    VALUES (N'V1.3.0', N'Phase 4 platform tenant management', SYSUTCDATETIME(), SUSER_SNAME());
END;
GO
