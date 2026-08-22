/*
==============================================================
Script        : 001_User_GetByIdForTenant.sql
Purpose       : Reads a tenant user only inside the supplied tenant boundary.
Safety        : Repeat-safe through CREATE OR ALTER.
==============================================================
*/
USE [CustSearch_AI];
GO

CREATE OR ALTER PROCEDURE dbo.User_GetByIdForTenant
    @TenantId BIGINT,
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TenantId IS NULL OR @TenantId <= 0
        THROW 50001, 'A valid tenant identifier is required.', 1;

    SELECT
        u.Id,
        u.TenantId,
        u.Scope,
        u.UserName,
        u.NormalizedUserName,
        u.Email,
        u.NormalizedEmail,
        u.DisplayName,
        u.IsActive,
        u.CreatedUtc,
        u.LastLoginUtc
    FROM dbo.Users AS u
    WHERE u.Id = @UserId
      AND u.TenantId = @TenantId
      AND u.Scope = 2;
END;
GO
