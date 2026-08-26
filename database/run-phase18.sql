:setvar DatabaseName "CustSearch_AI"
:on error exit
USE [$(DatabaseName)];
GO
:r .\09_Upgrade\V1.16.0_Phase18_RetailSecurity.sql
:r .\09_Upgrade\V1.17.0_Phase18_RetailSecurityApplication.sql
:r .\verify-phase18.sql
