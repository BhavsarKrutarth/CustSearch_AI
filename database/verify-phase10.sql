/*
 CustSearch AI — Phase 10 read-only verification script
 Run after database/run-phase10.sql. It does not create/drop business objects.
 Verifies V1.9 versioning, required tables/indexes/FKs/procedures, dynamic voice/category-alias boundaries and constraint consistency.
*/
USE [CustSearch_AI];
GO
SET NOCOUNT ON;

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.9.0')<>1 THROW 52200,'V1.9.0 must exist exactly once.',1;
IF OBJECT_ID(N'dbo.CustomerPreferenceSignals',N'U') IS NULL THROW 52201,'CustomerPreferenceSignals missing.',1;
IF OBJECT_ID(N'dbo.CustomerPreferenceScores',N'U') IS NULL THROW 52202,'CustomerPreferenceScores missing.',1;
IF OBJECT_ID(N'dbo.HouseholdPreferenceTags',N'U') IS NULL THROW 52203,'HouseholdPreferenceTags missing.',1;
IF OBJECT_ID(N'dbo.ProductCategoryAliases',N'U') IS NULL THROW 52204,'ProductCategoryAliases missing.',1;
IF OBJECT_ID(N'dbo.StoreVoiceCommandRuntimeSettings',N'U') IS NULL THROW 52205,'StoreVoiceCommandRuntimeSettings missing.',1;
IF OBJECT_ID(N'dbo.VoiceCommandSessions',N'U') IS NULL THROW 52206,'VoiceCommandSessions missing.',1;
IF OBJECT_ID(N'dbo.PreferenceWeightVersions',N'U') IS NULL THROW 52207,'PreferenceWeightVersions missing.',1;
IF OBJECT_ID(N'dbo.CustomerPreference_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.HouseholdPreference_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.ProductCategoryAlias_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.PreferenceWeight_GetActive',N'P') IS NULL OR OBJECT_ID(N'dbo.PreferenceAudit_Search',N'P') IS NULL THROW 52208,'Phase 10 procedures missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategoryAliases') AND name=N'UX_ProductCategoryAliases_Scope_Phrase_Category') THROW 52209,'Category alias uniqueness index missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.ProductCategoryAliases') AND referenced_object_id=OBJECT_ID(N'dbo.ProductCategories')) THROW 52210,'Category alias FK to ProductCategories missing.',1;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.HouseholdPreferenceTags') AND name IN(N'VisitPartyId',N'AnonymousVisitorId',N'FaceId')) THROW 52211,'Household preferences must not depend on VisitParty/anonymous/face identity.',1;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.ProductCategoryAliases') AND name IN(N'AutoCreateCategory',N'GeneratedCategoryId')) THROW 52212,'Voice alias schema must not support automatic category creation.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Preferences.View') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'VoiceCommands.Use') OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'VoiceCommands.Audit') THROW 52213,'Phase 10 permissions missing.',1;
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.HouseholdPreference_Get')) NOT LIKE N'%IsVerified=1%' THROW 52214,'Household preference procedure must require verified members.',1;
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.CustomerPreference_Get')) NOT LIKE N'%@AllowedStoreIdsCsv%' THROW 52215,'Customer preference store scope missing.',1;

EXEC dbo.ProductCategoryAlias_Search @TenantId=-1,@StoreId=NULL,@ProductCategoryId=NULL;
EXEC dbo.CustomerPreference_Get @TenantId=-1,@CustomerId=-1,@AllowedStoreIdsCsv=NULL;
EXEC dbo.HouseholdPreference_Get @TenantId=-1,@HouseholdId=-1,@AllowedStoreIdsCsv=NULL;
EXEC dbo.PreferenceWeight_GetActive @TenantId=-1;
EXEC dbo.PreferenceAudit_Search @TenantId=-1,@AllowedStoreIdsCsv=NULL,@PageNumber=1,@PageSize=10;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
PRINT 'PHASE10_DATABASE_VERIFICATION_GREEN';
GO
