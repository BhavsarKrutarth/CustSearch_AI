/*
Purpose       : Binds existing and future refresh sessions to the user's current security stamp.
Tenant scope  : Platform and tenant authentication infrastructure; no tenant-owned row is exposed.
Transaction   : Short schema upgrade and deterministic backfill in one transaction.
Idempotency   : Safe to run repeatedly; adds or finalizes the column only when required.
*/
USE [CustSearch_AI];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- Active, suspended and inactive are distinct lifecycle states. This corrects the
-- original constraint that accidentally rejected the valid inactive state.
IF OBJECT_ID(N'dbo.CK_Tenants_ActiveSuspended', N'C') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP CONSTRAINT CK_Tenants_ActiveSuspended;
END;

ALTER TABLE dbo.Tenants WITH CHECK
    ADD CONSTRAINT CK_Tenants_ActiveSuspended
        CHECK (NOT (IsActive = 0 AND IsSuspended = 1));

IF COL_LENGTH(N'dbo.RefreshTokens', N'IssuedSecurityStamp') IS NULL
BEGIN
    ALTER TABLE dbo.RefreshTokens
        ADD IssuedSecurityStamp NVARCHAR(64) NULL;
END;

-- Existing sessions inherit the stamp that is current during deployment. Later stamp changes
-- are detected because newly issued and backfilled tokens retain this issuance-time value.
-- Dynamic SQL compiles after the conditional ALTER, including on databases upgraded from Phase 2.
EXEC sys.sp_executesql N'
    UPDATE refreshToken
    SET IssuedSecurityStamp = users.SecurityStamp
    FROM dbo.RefreshTokens AS refreshToken
    INNER JOIN dbo.Users AS users ON users.Id = refreshToken.UserId
    WHERE refreshToken.IssuedSecurityStamp IS NULL;';

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.RefreshTokens')
      AND name = N'IssuedSecurityStamp'
      AND is_nullable = 1
)
BEGIN
    EXEC sys.sp_executesql N'
        ALTER TABLE dbo.RefreshTokens
            ALTER COLUMN IssuedSecurityStamp NVARCHAR(64) NOT NULL;';
END;

COMMIT TRANSACTION;
GO
