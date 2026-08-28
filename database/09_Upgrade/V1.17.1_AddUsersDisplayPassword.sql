/*
  Adds an optional direct-display password column for local/support operations.
  Existing PasswordHash values cannot be converted back to passwords. Only future
  create/reset/change-password operations write DisplayPassword when the
  PasswordStorage:StoreDisplayPassword application setting is enabled.
*/
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH(N'dbo.Users', N'DisplayPassword') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD DisplayPassword NVARCHAR(500) NULL;
END;
GO

COMMIT TRANSACTION;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber = N'V1.17.1')
BEGIN
    INSERT dbo.DatabaseVersions(VersionNumber, Description, AppliedUtc, AppliedBy)
    VALUES(N'V1.17.1', N'Optional direct-display password column for support operations', SYSUTCDATETIME(), SUSER_SNAME());
END;
GO

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber = N'V1.17.1') <> 1
    THROW 56180, 'V1.17.1 version row must exist exactly once.', 1;
GO
