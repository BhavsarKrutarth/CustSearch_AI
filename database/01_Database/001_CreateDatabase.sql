/*
==============================================================
Script        : 001_CreateDatabase.sql
Purpose       : Creates the CustSearch_AI database when absent.
Safety        : Repeat-safe. Never drops or recreates a database.
==============================================================
*/
USE [master];
GO

IF DB_ID(N'CustSearch_AI') IS NULL
BEGIN
    PRINT N'Creating database CustSearch_AI.';
    CREATE DATABASE [CustSearch_AI];
END
ELSE
BEGIN
    PRINT N'Database CustSearch_AI already exists.';
END;
GO

/* Keep behavior aligned with the project's SQL Server 2022 production target,
   including when development runs on a newer SQL Server instance. */
IF (SELECT compatibility_level FROM sys.databases WHERE name = N'CustSearch_AI') <> 160
BEGIN
    PRINT N'Setting CustSearch_AI compatibility level to SQL Server 2022 (160).';
    ALTER DATABASE [CustSearch_AI] SET COMPATIBILITY_LEVEL = 160;
END;
GO
