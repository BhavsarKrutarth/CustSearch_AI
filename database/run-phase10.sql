/*
 CustSearch AI — Phase 10 standalone/manual SQL Server 2022 installer
 Version: V1.9.0
 Prerequisite: validated Phase 9 V1.8.0 CustSearch_AI database.
 Safe/repeatable: run directly in SSMS/Azure Data Studio; no PowerShell or :r include is required.

 Phase details:
  10A factual CustomerPreferenceSignals
  10B explicit HouseholdPreferenceTags; verified HouseholdMembers only are aggregated
  10C/10D ProductCategoryAliases + existing Phase 5 dynamic store trigger/aliases
  10E VoiceCommandSessions with server-side category resolution/confirmation
  10F PreferenceWeightVersions + CustomerPreferenceScores
  10G permissions and tenant/store-safe read procedures

 Privacy: VisitParty/co-visit is never Household truth. Voice text never auto-creates a category.
*/
USE [CustSearch_AI];
GO
SET NOCOUNT ON; SET XACT_ABORT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET CONCAT_NULL_YIELDS_NULL ON; SET ARITHABORT ON; SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.8.0') THROW 52100,'Phase 10 requires Phase 9 V1.8.0.',1;
IF OBJECT_ID(N'dbo.Customers',N'U') IS NULL OR OBJECT_ID(N'dbo.Households',N'U') IS NULL OR OBJECT_ID(N'dbo.HouseholdMembers',N'U') IS NULL OR OBJECT_ID(N'dbo.ProductCategories',N'U') IS NULL OR OBJECT_ID(N'dbo.StoreVoiceCommandSettings',N'U') IS NULL THROW 52101,'Required Phase 5-9 baseline objects are missing.',1;
GO

-- 10A factual preference signals.
IF OBJECT_ID(N'dbo.CustomerPreferenceSignals',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CustomerPreferenceSignals(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerPreferenceSignals PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NULL,CustomerId BIGINT NOT NULL,PreferenceType TINYINT NOT NULL,ReferenceId BIGINT NULL,Value NVARCHAR(200) NULL,SignalScore DECIMAL(6,2) NULL,Source TINYINT NOT NULL,Confidence DECIMAL(6,2) NULL,FirstObservedUtc DATETIME2(7) NOT NULL,LastObservedUtc DATETIME2(7) NOT NULL,IsActive BIT NOT NULL CONSTRAINT DF_CustomerPreferenceSignals_IsActive DEFAULT(1),CreatedByUserId BIGINT NULL,Reason NVARCHAR(500) NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_CustomerPreferenceSignals_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_CustomerPreferenceSignals_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),CONSTRAINT FK_CustomerPreferenceSignals_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),CONSTRAINT FK_CustomerPreferenceSignals_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_CustomerPreferenceSignals_Type CHECK(PreferenceType BETWEEN 1 AND 5),CONSTRAINT CK_CustomerPreferenceSignals_Source CHECK(Source BETWEEN 1 AND 4),CONSTRAINT CK_CustomerPreferenceSignals_Identity CHECK(ReferenceId IS NOT NULL OR NULLIF(LTRIM(RTRIM(Value)),N'') IS NOT NULL),CONSTRAINT CK_CustomerPreferenceSignals_Score CHECK(SignalScore IS NULL OR SignalScore BETWEEN 0 AND 100),CONSTRAINT CK_CustomerPreferenceSignals_Confidence CHECK(Confidence IS NULL OR Confidence BETWEEN 0 AND 100),CONSTRAINT CK_CustomerPreferenceSignals_Period CHECK(LastObservedUtc>=FirstObservedUtc));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerPreferenceSignals') AND name=N'UX_CustomerPreferenceSignals_Tenant_Id') CREATE UNIQUE INDEX UX_CustomerPreferenceSignals_Tenant_Id ON dbo.CustomerPreferenceSignals(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerPreferenceSignals') AND name=N'IX_CustomerPreferenceSignals_Tenant_Customer_Active') CREATE INDEX IX_CustomerPreferenceSignals_Tenant_Customer_Active ON dbo.CustomerPreferenceSignals(TenantId,CustomerId,IsActive,LastObservedUtc DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerPreferenceSignals') AND name=N'IX_CustomerPreferenceSignals_Tenant_Store_Customer') CREATE INDEX IX_CustomerPreferenceSignals_Tenant_Store_Customer ON dbo.CustomerPreferenceSignals(TenantId,StoreId,CustomerId);
GO

-- 10B explicit shared household tags. No VisitParty/CCTV inference source exists.
IF OBJECT_ID(N'dbo.HouseholdPreferenceTags',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.HouseholdPreferenceTags(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HouseholdPreferenceTags PRIMARY KEY,TenantId BIGINT NOT NULL,HouseholdId BIGINT NOT NULL,PreferenceType TINYINT NOT NULL,ReferenceId BIGINT NULL,Value NVARCHAR(200) NOT NULL,Source TINYINT NOT NULL,CreatedByUserId BIGINT NOT NULL,Reason NVARCHAR(500) NULL,IsActive BIT NOT NULL CONSTRAINT DF_HouseholdPreferenceTags_IsActive DEFAULT(1),CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_HouseholdPreferenceTags_Households_TenantHousehold FOREIGN KEY(TenantId,HouseholdId) REFERENCES dbo.Households(TenantId,Id),CONSTRAINT FK_HouseholdPreferenceTags_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_HouseholdPreferenceTags_Type CHECK(PreferenceType BETWEEN 1 AND 5),CONSTRAINT CK_HouseholdPreferenceTags_Source CHECK(Source BETWEEN 1 AND 3),CONSTRAINT CK_HouseholdPreferenceTags_Value CHECK(NULLIF(LTRIM(RTRIM(Value)),N'') IS NOT NULL));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HouseholdPreferenceTags') AND name=N'IX_HouseholdPreferenceTags_Tenant_Household_Active') CREATE INDEX IX_HouseholdPreferenceTags_Tenant_Household_Active ON dbo.HouseholdPreferenceTags(TenantId,HouseholdId,IsActive,CreatedUtc DESC);
GO

-- 10C/10D category aliases map speech only to existing ProductCategories. Duplicate phrase -> different categories is allowed so ambiguity is explicit.
IF OBJECT_ID(N'dbo.ProductCategoryAliases',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.ProductCategoryAliases(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductCategoryAliases PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NULL,ProductCategoryId BIGINT NOT NULL,AliasText NVARCHAR(150) NOT NULL,NormalizedAliasText NVARCHAR(150) NOT NULL,LanguageCode NVARCHAR(20) NOT NULL,IsActive BIT NOT NULL CONSTRAINT DF_ProductCategoryAliases_IsActive DEFAULT(1),CreatedByUserId BIGINT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_ProductCategoryAliases_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_ProductCategoryAliases_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),CONSTRAINT FK_ProductCategoryAliases_Categories_TenantCategory FOREIGN KEY(TenantId,ProductCategoryId) REFERENCES dbo.ProductCategories(TenantId,Id),CONSTRAINT FK_ProductCategoryAliases_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_ProductCategoryAliases_Text CHECK(NULLIF(LTRIM(RTRIM(AliasText)),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM(NormalizedAliasText)),N'') IS NOT NULL));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategoryAliases') AND name=N'UX_ProductCategoryAliases_Scope_Phrase_Category') CREATE UNIQUE INDEX UX_ProductCategoryAliases_Scope_Phrase_Category ON dbo.ProductCategoryAliases(TenantId,StoreId,NormalizedAliasText,ProductCategoryId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategoryAliases') AND name=N'IX_ProductCategoryAliases_Tenant_Category_Active') CREATE INDEX IX_ProductCategoryAliases_Tenant_Category_Active ON dbo.ProductCategoryAliases(TenantId,ProductCategoryId,IsActive);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategoryAliases') AND name=N'IX_ProductCategoryAliases_Tenant_Store_Phrase') CREATE INDEX IX_ProductCategoryAliases_Tenant_Store_Phrase ON dbo.ProductCategoryAliases(TenantId,StoreId,NormalizedAliasText,IsActive);
GO

-- 10D runtime controls extend Phase 5 StoreVoiceCommandSettings/StoreVoiceCommandAliases.
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StoreVoiceCommandSettings') AND name=N'UX_StoreVoiceCommandSettings_Tenant_Store') CREATE UNIQUE INDEX UX_StoreVoiceCommandSettings_Tenant_Store ON dbo.StoreVoiceCommandSettings(TenantId,StoreId);
GO
IF OBJECT_ID(N'dbo.StoreVoiceCommandRuntimeSettings',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.StoreVoiceCommandRuntimeSettings(StoreId BIGINT NOT NULL CONSTRAINT PK_StoreVoiceCommandRuntimeSettings PRIMARY KEY,TenantId BIGINT NOT NULL,LanguageCode NVARCHAR(20) NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Language DEFAULT(N'en-IN'),RequireConfirmation BIT NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Confirmation DEFAULT(1),ListeningTimeoutSeconds INT NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Timeout DEFAULT(30),MinimumRecognitionConfidence DECIMAL(6,2) NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Confidence DEFAULT(70),CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Created DEFAULT(SYSUTCDATETIME()),UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceRuntime_Updated DEFAULT(SYSUTCDATETIME()),CONSTRAINT FK_StoreVoiceRuntime_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),CONSTRAINT CK_StoreVoiceRuntime_Timeout CHECK(ListeningTimeoutSeconds BETWEEN 3 AND 120),CONSTRAINT CK_StoreVoiceRuntime_Confidence CHECK(MinimumRecognitionConfidence BETWEEN 0 AND 100));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StoreVoiceCommandRuntimeSettings') AND name=N'UX_StoreVoiceRuntime_Tenant_Store') CREATE UNIQUE INDEX UX_StoreVoiceRuntime_Tenant_Store ON dbo.StoreVoiceCommandRuntimeSettings(TenantId,StoreId);
GO

-- 10E confirmation-controlled voice session.
IF OBJECT_ID(N'dbo.VoiceCommandSessions',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.VoiceCommandSessions(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VoiceCommandSessions PRIMARY KEY,TenantId BIGINT NOT NULL,StoreId BIGINT NOT NULL,StaffUserId BIGINT NOT NULL,CustomerId BIGINT NOT NULL,MatchedTrigger NVARCHAR(100) NOT NULL,RecognizedText NVARCHAR(250) NULL,RecognitionConfidence DECIMAL(6,2) NULL,ProposedPreferenceType TINYINT NULL,ProposedReferenceId BIGINT NULL,ProposedValue NVARCHAR(200) NULL,ConfirmationRequired BIT NOT NULL,Status TINYINT NOT NULL,ExpiresUtc DATETIME2(7) NOT NULL,ResolvedUtc DATETIME2(7) NULL,CreatedUtc DATETIME2(7) NOT NULL,UpdatedUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_VoiceCommandSessions_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),CONSTRAINT FK_VoiceCommandSessions_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),CONSTRAINT FK_VoiceCommandSessions_Users FOREIGN KEY(StaffUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_VoiceCommandSessions_Status CHECK(Status BETWEEN 1 AND 5),CONSTRAINT CK_VoiceCommandSessions_Confidence CHECK(RecognitionConfidence IS NULL OR RecognitionConfidence BETWEEN 0 AND 100),CONSTRAINT CK_VoiceCommandSessions_Type CHECK(ProposedPreferenceType IS NULL OR ProposedPreferenceType BETWEEN 1 AND 5),CONSTRAINT CK_VoiceCommandSessions_Period CHECK(ExpiresUtc>CreatedUtc AND (ResolvedUtc IS NULL OR ResolvedUtc>=CreatedUtc)));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VoiceCommandSessions') AND name=N'IX_VoiceCommandSessions_Tenant_Store_Customer_Status') CREATE INDEX IX_VoiceCommandSessions_Tenant_Store_Customer_Status ON dbo.VoiceCommandSessions(TenantId,StoreId,CustomerId,Status,CreatedUtc DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VoiceCommandSessions') AND name=N'IX_VoiceCommandSessions_Tenant_Staff_Created') CREATE INDEX IX_VoiceCommandSessions_Tenant_Staff_Created ON dbo.VoiceCommandSessions(TenantId,StaffUserId,CreatedUtc DESC);
GO

-- 10F reproducible scoring weights and derived scores.
IF OBJECT_ID(N'dbo.PreferenceWeightVersions',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.PreferenceWeightVersions(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PreferenceWeightVersions PRIMARY KEY,TenantId BIGINT NOT NULL,VersionCode NVARCHAR(50) NOT NULL,ManualStaffWeight DECIMAL(6,3) NOT NULL,PurchaseWeight DECIMAL(6,3) NOT NULL,CategoryInteractionWeight DECIMAL(6,3) NOT NULL,VoiceConfirmedWeight DECIMAL(6,3) NOT NULL,IsActive BIT NOT NULL CONSTRAINT DF_PreferenceWeightVersions_IsActive DEFAULT(1),CreatedByUserId BIGINT NOT NULL,CreatedUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_PreferenceWeightVersions_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),CONSTRAINT FK_PreferenceWeightVersions_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_PreferenceWeightVersions_Weights CHECK(ManualStaffWeight BETWEEN 0 AND 10 AND PurchaseWeight BETWEEN 0 AND 10 AND CategoryInteractionWeight BETWEEN 0 AND 10 AND VoiceConfirmedWeight BETWEEN 0 AND 10));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PreferenceWeightVersions') AND name=N'UX_PreferenceWeightVersions_Tenant_Code') CREATE UNIQUE INDEX UX_PreferenceWeightVersions_Tenant_Code ON dbo.PreferenceWeightVersions(TenantId,VersionCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PreferenceWeightVersions') AND name=N'UX_PreferenceWeightVersions_Tenant_Id') CREATE UNIQUE INDEX UX_PreferenceWeightVersions_Tenant_Id ON dbo.PreferenceWeightVersions(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PreferenceWeightVersions') AND name=N'UX_PreferenceWeightVersions_OneActive') CREATE UNIQUE INDEX UX_PreferenceWeightVersions_OneActive ON dbo.PreferenceWeightVersions(TenantId) WHERE IsActive=1;
GO
IF OBJECT_ID(N'dbo.CustomerPreferenceScores',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.CustomerPreferenceScores(Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerPreferenceScores PRIMARY KEY,TenantId BIGINT NOT NULL,CustomerId BIGINT NOT NULL,PreferenceType TINYINT NOT NULL,ReferenceId BIGINT NULL,Value NVARCHAR(200) NULL,Score DECIMAL(6,2) NOT NULL,WeightVersionId BIGINT NOT NULL,CalculatedUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_CustomerPreferenceScores_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),CONSTRAINT FK_CustomerPreferenceScores_Weight_TenantVersion FOREIGN KEY(TenantId,WeightVersionId) REFERENCES dbo.PreferenceWeightVersions(TenantId,Id),CONSTRAINT CK_CustomerPreferenceScores_Type CHECK(PreferenceType BETWEEN 1 AND 5),CONSTRAINT CK_CustomerPreferenceScores_Identity CHECK(ReferenceId IS NOT NULL OR NULLIF(LTRIM(RTRIM(Value)),N'') IS NOT NULL),CONSTRAINT CK_CustomerPreferenceScores_Value CHECK(Score BETWEEN 0 AND 100));
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerPreferenceScores') AND name=N'IX_CustomerPreferenceScores_Tenant_Customer_Score') CREATE INDEX IX_CustomerPreferenceScores_Tenant_Customer_Score ON dbo.CustomerPreferenceScores(TenantId,CustomerId,Score DESC);
GO

-- 10G stable permission ensure.
DECLARE @Phase10Permissions TABLE(Name NVARCHAR(150),Description NVARCHAR(300));
INSERT INTO @Phase10Permissions VALUES(N'Preferences.View',N'View customer and verified-household preferences.'),(N'Preferences.Manage',N'Manage explicit preferences and run recalculation.'),(N'VoiceCommands.Use',N'Use store-configured voice commands.'),(N'VoiceCommands.View',N'View store voice-command settings.'),(N'VoiceCommands.Configure',N'Configure store voice triggers, aliases and runtime controls.'),(N'VoiceCommands.Audit',N'View preference and voice-command audit history.');
INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) SELECT 2,p.Name,p.Description,1,SYSUTCDATETIME() FROM @Phase10Permissions p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);
GO

CREATE OR ALTER PROCEDURE dbo.CustomerPreference_Get @TenantId BIGINT,@CustomerId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL AS BEGIN SET NOCOUNT ON;IF NOT EXISTS(SELECT 1 FROM dbo.Customers c WHERE c.TenantId=@TenantId AND c.Id=@CustomerId AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments csa JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE csa.TenantId=@TenantId AND csa.CustomerId=c.Id))) RETURN;SELECT Id,StoreId,CustomerId,PreferenceType,ReferenceId,Value,SignalScore,Source,Confidence,FirstObservedUtc,LastObservedUtc,IsActive,Reason FROM dbo.CustomerPreferenceSignals WHERE TenantId=@TenantId AND CustomerId=@CustomerId ORDER BY LastObservedUtc DESC,Id DESC;SELECT Id,CustomerId,PreferenceType,ReferenceId,Value,Score,WeightVersionId,CalculatedUtc FROM dbo.CustomerPreferenceScores WHERE TenantId=@TenantId AND CustomerId=@CustomerId ORDER BY Score DESC,Id DESC;END;
GO
CREATE OR ALTER PROCEDURE dbo.HouseholdPreference_Get @TenantId BIGINT,@HouseholdId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL AS BEGIN SET NOCOUNT ON;IF NOT EXISTS(SELECT 1 FROM dbo.Households h WHERE h.TenantId=@TenantId AND h.Id=@HouseholdId AND h.IsActive=1) RETURN;IF @AllowedStoreIdsCsv IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.HouseholdMembers hm JOIN dbo.CustomerStoreAssignments csa ON csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE hm.TenantId=@TenantId AND hm.HouseholdId=@HouseholdId AND hm.IsActive=1 AND hm.IsVerified=1) RETURN;SELECT hm.CustomerId,c.FirstName,c.LastName,hm.RelationshipType,hm.RelationshipSource,hm.VerifiedUtc FROM dbo.HouseholdMembers hm JOIN dbo.Customers c ON c.TenantId=hm.TenantId AND c.Id=hm.CustomerId WHERE hm.TenantId=@TenantId AND hm.HouseholdId=@HouseholdId AND hm.IsActive=1 AND hm.IsVerified=1 AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments csa JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId));SELECT s.CustomerId,s.PreferenceType,s.ReferenceId,s.Value,s.Score,s.WeightVersionId,s.CalculatedUtc FROM dbo.CustomerPreferenceScores s JOIN dbo.HouseholdMembers hm ON hm.TenantId=s.TenantId AND hm.CustomerId=s.CustomerId AND hm.HouseholdId=@HouseholdId AND hm.IsActive=1 AND hm.IsVerified=1 WHERE s.TenantId=@TenantId AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments csa JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') x ON TRY_CONVERT(BIGINT,x.value)=csa.StoreId WHERE csa.TenantId=@TenantId AND csa.CustomerId=s.CustomerId));SELECT Id,PreferenceType,ReferenceId,Value,Source,Reason,CreatedUtc FROM dbo.HouseholdPreferenceTags WHERE TenantId=@TenantId AND HouseholdId=@HouseholdId AND IsActive=1 ORDER BY CreatedUtc DESC;END;
GO
CREATE OR ALTER PROCEDURE dbo.ProductCategoryAlias_Search @TenantId BIGINT,@StoreId BIGINT=NULL,@ProductCategoryId BIGINT=NULL AS BEGIN SET NOCOUNT ON;SELECT a.Id,a.StoreId,a.ProductCategoryId,c.CategoryCode,c.Name CategoryName,a.AliasText,a.NormalizedAliasText,a.LanguageCode,a.IsActive,a.CreatedUtc FROM dbo.ProductCategoryAliases a JOIN dbo.ProductCategories c ON c.TenantId=a.TenantId AND c.Id=a.ProductCategoryId WHERE a.TenantId=@TenantId AND a.IsActive=1 AND (@StoreId IS NULL OR a.StoreId IS NULL OR a.StoreId=@StoreId) AND (@ProductCategoryId IS NULL OR a.ProductCategoryId=@ProductCategoryId) ORDER BY a.AliasText,a.Id;END;
GO
CREATE OR ALTER PROCEDURE dbo.PreferenceWeight_GetActive @TenantId BIGINT AS BEGIN SET NOCOUNT ON;SELECT TOP(1) Id,VersionCode,ManualStaffWeight,PurchaseWeight,CategoryInteractionWeight,VoiceConfirmedWeight,IsActive,CreatedUtc FROM dbo.PreferenceWeightVersions WHERE TenantId=@TenantId AND IsActive=1 ORDER BY Id DESC;END;
GO
CREATE OR ALTER PROCEDURE dbo.PreferenceAudit_Search @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@PageNumber INT=1,@PageSize INT=50 AS BEGIN SET NOCOUNT ON;IF @PageNumber<1 SET @PageNumber=1;IF @PageSize<1 SET @PageSize=50;IF @PageSize>200 SET @PageSize=200;SELECT Id,StoreId,UserId,Action,EntityType,EntityId,BeforeJson,AfterJson,CorrelationId,CreatedUtc,COUNT_BIG(1) OVER() TotalCount FROM dbo.AuditLogs WHERE TenantId=@TenantId AND (Action LIKE N'CustomerPreference%' OR Action LIKE N'HouseholdPreference%' OR Action LIKE N'PreferenceWeight%' OR Action LIKE N'VoiceCommand%' OR Action LIKE N'StoreVoice%' OR Action LIKE N'ProductCategoryAlias%') AND (@AllowedStoreIdsCsv IS NULL OR StoreId IS NULL OR EXISTS(SELECT 1 FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') s WHERE TRY_CONVERT(BIGINT,s.value)=StoreId)) ORDER BY CreatedUtc DESC,Id DESC OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.9.0') INSERT INTO dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.9.0',N'Phase 10 factual preferences, category aliases, verified-household aggregation, dynamic voice confirmation and versioned recalculation',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.9.0')<>1 THROW 52190,'V1.9.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.CustomerPreferenceSignals',N'U') IS NULL OR OBJECT_ID(N'dbo.ProductCategoryAliases',N'U') IS NULL OR OBJECT_ID(N'dbo.VoiceCommandSessions',N'U') IS NULL OR OBJECT_ID(N'dbo.PreferenceWeightVersions',N'U') IS NULL THROW 52191,'Phase 10 tables are incomplete.',1;
IF OBJECT_ID(N'dbo.CustomerPreference_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.HouseholdPreference_Get',N'P') IS NULL OR OBJECT_ID(N'dbo.ProductCategoryAlias_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.PreferenceAudit_Search',N'P') IS NULL THROW 52192,'Phase 10 procedures are incomplete.',1;
PRINT 'PHASE10_DATABASE_INSTALL_GREEN';
SELECT VersionNumber,Description,AppliedUtc,AppliedBy FROM dbo.DatabaseVersions WHERE VersionNumber IN(N'V1.8.0',N'V1.9.0') ORDER BY VersionNumber;
GO
