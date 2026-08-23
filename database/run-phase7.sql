/*
==============================================================
CustSearch AI - Direct SQL Server Phase 7 Runner
File          : database/run-phase7.sql
Database      : CustSearch_AI
SQL Server    : 2022+
Purpose       : Plain T-SQL Phase 7 households/visits deployment + validation.
Usage         : Open directly in SSMS/Azure Data Studio and Execute.
Dependency    : Phase 6 / V1.5.0 must already be installed.
Safety        : Idempotent; no DROP/TRUNCATE, no EF migrations, no SQLCMD :r.
==============================================================
*/
USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
SET ANSI_WARNINGS ON;
GO
SET CONCAT_NULL_YIELDS_NULL ON;
GO
SET ARITHABORT ON;
GO
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL
    THROW 51300,'DatabaseVersions is missing. Install earlier phases first.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.5.0')
    THROW 51301,'Phase 6 V1.5.0 is not installed. Run database/run-phase6.sql first.',1;
IF OBJECT_ID(N'dbo.Tenants',N'U') IS NULL OR OBJECT_ID(N'dbo.Users',N'U') IS NULL OR OBJECT_ID(N'dbo.Stores',N'U') IS NULL
    THROW 51302,'Tenant/User/Store foundation is missing.',1;
IF OBJECT_ID(N'dbo.Customers',N'U') IS NULL OR OBJECT_ID(N'dbo.AnonymousVisitors',N'U') IS NULL OR OBJECT_ID(N'dbo.CustomerStoreAssignments',N'U') IS NULL
    THROW 51303,'Phase 6 customer/visitor foundation is missing.',1;
GO

-- ============================================================
-- PHASE 7 - HOUSEHOLDS / VISITS
-- 7A Household Management
-- 7B Household Members & Verified Relationships
-- 7C Visit Parties / Co-Visit Evidence
-- 7D Customer Visits
-- 7G Tenant/Store Isolation & Privacy
-- 7H Database Validation / Version
-- VERSION V1.6.0
-- ============================================================
IF OBJECT_ID(N'dbo.Households',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Households
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Households PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        HouseholdCode NVARCHAR(50) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Notes NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Households_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Households_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Households_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Households_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Households') AND name=N'UX_Households_Tenant_Code') CREATE UNIQUE INDEX UX_Households_Tenant_Code ON dbo.Households(TenantId,HouseholdCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Households') AND name=N'UX_Households_Tenant_Id') CREATE UNIQUE INDEX UX_Households_Tenant_Id ON dbo.Households(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Households') AND name=N'IX_Households_Tenant_Active_Updated') CREATE INDEX IX_Households_Tenant_Active_Updated ON dbo.Households(TenantId,IsActive,UpdatedUtc DESC);
GO

/* 7B: RelationshipSource values are only explicit/human-verifiable sources: 1 CustomerProvided, 2 StaffVerified, 3 AdminVerified, 4 ImportedVerified. */
IF OBJECT_ID(N'dbo.HouseholdMembers',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HouseholdMembers
    (
        TenantId BIGINT NOT NULL,
        HouseholdId BIGINT NOT NULL,
        CustomerId BIGINT NOT NULL,
        RelationshipType NVARCHAR(50) NOT NULL,
        RelationshipSource TINYINT NOT NULL,
        IsVerified BIT NOT NULL CONSTRAINT DF_HouseholdMembers_IsVerified DEFAULT(1),
        VerifiedByUserId BIGINT NOT NULL,
        VerifiedUtc DATETIME2(7) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_HouseholdMembers_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_HouseholdMembers_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_HouseholdMembers_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT PK_HouseholdMembers PRIMARY KEY(HouseholdId,CustomerId),
        CONSTRAINT FK_HouseholdMembers_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_HouseholdMembers_Households_TenantHousehold FOREIGN KEY(TenantId,HouseholdId) REFERENCES dbo.Households(TenantId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_HouseholdMembers_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_HouseholdMembers_Users_VerifiedBy FOREIGN KEY(VerifiedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_HouseholdMembers_RelationshipSource CHECK(RelationshipSource BETWEEN 1 AND 4),
        CONSTRAINT CK_HouseholdMembers_Verified CHECK(IsVerified=1 AND VerifiedByUserId>0)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HouseholdMembers') AND name=N'IX_HouseholdMembers_Tenant_Customer_Active') CREATE INDEX IX_HouseholdMembers_Tenant_Customer_Active ON dbo.HouseholdMembers(TenantId,CustomerId,IsActive);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HouseholdMembers') AND name=N'IX_HouseholdMembers_Tenant_Household_Active') CREATE INDEX IX_HouseholdMembers_Tenant_Household_Active ON dbo.HouseholdMembers(TenantId,HouseholdId,IsActive);
GO

/* 7C: co-visit evidence only; no household/family foreign key exists here. */
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'UX_AnonymousVisitors_Tenant_Store_Id') CREATE UNIQUE INDEX UX_AnonymousVisitors_Tenant_Store_Id ON dbo.AnonymousVisitors(TenantId,StoreId,Id);
GO
IF OBJECT_ID(N'dbo.VisitParties',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VisitParties
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VisitParties PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        PartyCode NVARCHAR(50) NOT NULL,
        StartedUtc DATETIME2(7) NOT NULL,
        EndedUtc DATETIME2(7) NULL,
        Source TINYINT NOT NULL,
        Status TINYINT NOT NULL CONSTRAINT DF_VisitParties_Status DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_VisitParties_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_VisitParties_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_VisitParties_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_VisitParties_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT CK_VisitParties_Source CHECK(Source BETWEEN 1 AND 4),
        CONSTRAINT CK_VisitParties_Status CHECK(Status BETWEEN 1 AND 3),
        CONSTRAINT CK_VisitParties_Period CHECK(EndedUtc IS NULL OR EndedUtc>=StartedUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitParties') AND name=N'UX_VisitParties_Tenant_Store_Code') CREATE UNIQUE INDEX UX_VisitParties_Tenant_Store_Code ON dbo.VisitParties(TenantId,StoreId,PartyCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitParties') AND name=N'UX_VisitParties_Tenant_Store_Id') CREATE UNIQUE INDEX UX_VisitParties_Tenant_Store_Id ON dbo.VisitParties(TenantId,StoreId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitParties') AND name=N'IX_VisitParties_Tenant_Store_Status_Start') CREATE INDEX IX_VisitParties_Tenant_Store_Status_Start ON dbo.VisitParties(TenantId,StoreId,Status,StartedUtc DESC);
GO
IF OBJECT_ID(N'dbo.VisitPartyMembers',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VisitPartyMembers
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VisitPartyMembers PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        VisitPartyId BIGINT NOT NULL,
        IdentityType TINYINT NOT NULL,
        CustomerId BIGINT NULL,
        AnonymousVisitorId BIGINT NULL,
        JoinedUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_VisitPartyMembers_Party_TenantStoreParty FOREIGN KEY(TenantId,StoreId,VisitPartyId) REFERENCES dbo.VisitParties(TenantId,StoreId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_VisitPartyMembers_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_VisitPartyMembers_Visitors_TenantStoreVisitor FOREIGN KEY(TenantId,StoreId,AnonymousVisitorId) REFERENCES dbo.AnonymousVisitors(TenantId,StoreId,Id),
        CONSTRAINT CK_VisitPartyMembers_IdentityType CHECK(IdentityType IN(1,2)),
        CONSTRAINT CK_VisitPartyMembers_IdentityXor CHECK((IdentityType=1 AND CustomerId IS NOT NULL AND AnonymousVisitorId IS NULL) OR (IdentityType=2 AND CustomerId IS NULL AND AnonymousVisitorId IS NOT NULL))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitPartyMembers') AND name=N'IX_VisitPartyMembers_Tenant_Party') CREATE INDEX IX_VisitPartyMembers_Tenant_Party ON dbo.VisitPartyMembers(TenantId,StoreId,VisitPartyId,JoinedUtc);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitPartyMembers') AND name=N'UX_VisitPartyMembers_Party_Customer') CREATE UNIQUE INDEX UX_VisitPartyMembers_Party_Customer ON dbo.VisitPartyMembers(VisitPartyId,CustomerId) WHERE CustomerId IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VisitPartyMembers') AND name=N'UX_VisitPartyMembers_Party_Visitor') CREATE UNIQUE INDEX UX_VisitPartyMembers_Party_Visitor ON dbo.VisitPartyMembers(VisitPartyId,AnonymousVisitorId) WHERE AnonymousVisitorId IS NOT NULL;
GO

/* 7D: factual visit history, deliberately no invoice/preference columns. */
IF OBJECT_ID(N'dbo.CustomerVisits',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerVisits
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerVisits PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        CustomerId BIGINT NOT NULL,
        VisitPartyId BIGINT NULL,
        VisitCode NVARCHAR(50) NOT NULL,
        EnteredUtc DATETIME2(7) NOT NULL,
        ExitedUtc DATETIME2(7) NULL,
        Source TINYINT NOT NULL,
        Status TINYINT NOT NULL CONSTRAINT DF_CustomerVisits_Status DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_CustomerVisits_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_CustomerVisits_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_CustomerVisits_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_CustomerVisits_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_CustomerVisits_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_CustomerVisits_Parties_TenantStoreParty FOREIGN KEY(TenantId,StoreId,VisitPartyId) REFERENCES dbo.VisitParties(TenantId,StoreId,Id),
        CONSTRAINT CK_CustomerVisits_Source CHECK(Source BETWEEN 1 AND 4),
        CONSTRAINT CK_CustomerVisits_Status CHECK(Status BETWEEN 1 AND 3),
        CONSTRAINT CK_CustomerVisits_Period CHECK(ExitedUtc IS NULL OR ExitedUtc>=EnteredUtc)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'UX_CustomerVisits_Tenant_Code') CREATE UNIQUE INDEX UX_CustomerVisits_Tenant_Code ON dbo.CustomerVisits(TenantId,VisitCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'IX_CustomerVisits_Tenant_Store_Entered') CREATE INDEX IX_CustomerVisits_Tenant_Store_Entered ON dbo.CustomerVisits(TenantId,StoreId,EnteredUtc DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'IX_CustomerVisits_Tenant_Customer_Entered') CREATE INDEX IX_CustomerVisits_Tenant_Customer_Entered ON dbo.CustomerVisits(TenantId,CustomerId,EnteredUtc DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'IX_CustomerVisits_Tenant_Party') CREATE INDEX IX_CustomerVisits_Tenant_Party ON dbo.CustomerVisits(TenantId,VisitPartyId) WHERE VisitPartyId IS NOT NULL;
GO

CREATE OR ALTER PROCEDURE dbo.Household_Search @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@Search NVARCHAR(200)=NULL,@ActiveOnly BIT=0,@PageNumber INT=1,@PageSize INT=25
AS
BEGIN
 SET NOCOUNT ON; IF @PageNumber<1 SET @PageNumber=1; IF @PageSize<1 SET @PageSize=25; IF @PageSize>100 SET @PageSize=100;
 ;WITH F AS(
  SELECT h.Id,h.HouseholdCode,h.Name,h.IsActive,h.UpdatedUtc,
    (SELECT COUNT(*) FROM dbo.HouseholdMembers hm WHERE hm.TenantId=@TenantId AND hm.HouseholdId=h.Id AND hm.IsActive=1 AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments csa JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId))) VisibleMemberCount
  FROM dbo.Households h WHERE h.TenantId=@TenantId AND (@ActiveOnly=0 OR h.IsActive=1)
   AND (@Search IS NULL OR h.HouseholdCode LIKE N'%'+@Search+N'%' OR h.Name LIKE N'%'+@Search+N'%')
   AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.HouseholdMembers hm JOIN dbo.CustomerStoreAssignments csa ON csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE hm.TenantId=@TenantId AND hm.HouseholdId=h.Id AND hm.IsActive=1)))
 SELECT Id,HouseholdCode,Name,VisibleMemberCount,IsActive,UpdatedUtc,COUNT_BIG(1) OVER() TotalCount FROM F ORDER BY UpdatedUtc DESC,Id DESC OFFSET(@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
CREATE OR ALTER PROCEDURE dbo.Household_GetDetail @TenantId BIGINT,@HouseholdId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 IF @AllowedStoreIdsCsv IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.HouseholdMembers hm JOIN dbo.CustomerStoreAssignments csa ON csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE hm.TenantId=@TenantId AND hm.HouseholdId=@HouseholdId AND hm.IsActive=1) RETURN;
 SELECT Id,HouseholdCode,Name,Notes,IsActive,CreatedUtc,UpdatedUtc FROM dbo.Households WHERE TenantId=@TenantId AND Id=@HouseholdId;
 SELECT hm.CustomerId,c.CustomerCode,c.FirstName,c.LastName,hm.RelationshipType,hm.RelationshipSource,hm.IsVerified,hm.VerifiedByUserId,hm.VerifiedUtc,hm.IsActive FROM dbo.HouseholdMembers hm JOIN dbo.Customers c ON c.TenantId=hm.TenantId AND c.Id=hm.CustomerId
 WHERE hm.TenantId=@TenantId AND hm.HouseholdId=@HouseholdId AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(SELECT 1 FROM dbo.CustomerStoreAssignments csa JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=csa.StoreId WHERE csa.TenantId=@TenantId AND csa.CustomerId=hm.CustomerId));
END;
GO
CREATE OR ALTER PROCEDURE dbo.CustomerVisit_Search @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@CustomerId BIGINT=NULL,@Search NVARCHAR(200)=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@PageNumber INT=1,@PageSize INT=25
AS
BEGIN
 SET NOCOUNT ON; IF @PageNumber<1 SET @PageNumber=1; IF @PageSize<1 SET @PageSize=25; IF @PageSize>100 SET @PageSize=100;
 ;WITH F AS(SELECT v.Id,v.VisitCode,v.CustomerId,c.CustomerCode,CONCAT(c.FirstName,CASE WHEN c.LastName IS NULL THEN N'' ELSE N' '+c.LastName END) CustomerName,v.StoreId,v.VisitPartyId,v.EnteredUtc,v.ExitedUtc,v.Source,v.Status FROM dbo.CustomerVisits v JOIN dbo.Customers c ON c.TenantId=v.TenantId AND c.Id=v.CustomerId WHERE v.TenantId=@TenantId AND (@StoreId IS NULL OR v.StoreId=@StoreId) AND (@CustomerId IS NULL OR v.CustomerId=@CustomerId) AND (@FromUtc IS NULL OR v.EnteredUtc>=@FromUtc) AND (@ToUtc IS NULL OR v.EnteredUtc<@ToUtc) AND (@Search IS NULL OR v.VisitCode LIKE N'%'+@Search+N'%' OR c.CustomerCode LIKE N'%'+@Search+N'%' OR c.FirstName LIKE N'%'+@Search+N'%' OR c.LastName LIKE N'%'+@Search+N'%') AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)))
 SELECT *,COUNT_BIG(1) OVER() TotalCount FROM F ORDER BY EnteredUtc DESC,Id DESC OFFSET(@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
CREATE OR ALTER PROCEDURE dbo.VisitParty_Search @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@Search NVARCHAR(200)=NULL,@Status TINYINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@PageNumber INT=1,@PageSize INT=25
AS
BEGIN
 SET NOCOUNT ON; IF @PageNumber<1 SET @PageNumber=1; IF @PageSize<1 SET @PageSize=25; IF @PageSize>100 SET @PageSize=100;
 ;WITH F AS(SELECT p.Id,p.PartyCode,p.StoreId,p.StartedUtc,p.EndedUtc,p.Source,p.Status,(SELECT COUNT(*) FROM dbo.VisitPartyMembers m WHERE m.TenantId=@TenantId AND m.VisitPartyId=p.Id) MemberCount FROM dbo.VisitParties p WHERE p.TenantId=@TenantId AND (@StoreId IS NULL OR p.StoreId=@StoreId) AND (@Status IS NULL OR p.Status=@Status) AND (@Search IS NULL OR p.PartyCode LIKE N'%'+@Search+N'%') AND (@FromUtc IS NULL OR p.StartedUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.StartedUtc<@ToUtc) AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)))
 SELECT *,COUNT_BIG(1) OVER() TotalCount FROM F ORDER BY StartedUtc DESC,Id DESC OFFSET(@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
CREATE OR ALTER PROCEDURE dbo.VisitParty_GetDetail @TenantId BIGINT,@VisitPartyId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SELECT p.Id,p.PartyCode,p.StoreId,p.StartedUtc,p.EndedUtc,p.Source,p.Status,p.CreatedUtc,p.UpdatedUtc FROM dbo.VisitParties p WHERE p.TenantId=@TenantId AND p.Id=@VisitPartyId AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL));
 SELECT m.Id,m.IdentityType,m.CustomerId,c.CustomerCode,m.AnonymousVisitorId,av.VisitorCode,m.JoinedUtc FROM dbo.VisitPartyMembers m LEFT JOIN dbo.Customers c ON c.TenantId=m.TenantId AND c.Id=m.CustomerId LEFT JOIN dbo.AnonymousVisitors av ON av.TenantId=m.TenantId AND av.StoreId=m.StoreId AND av.Id=m.AnonymousVisitorId JOIN dbo.VisitParties p ON p.TenantId=m.TenantId AND p.StoreId=m.StoreId AND p.Id=m.VisitPartyId WHERE m.TenantId=@TenantId AND m.VisitPartyId=@VisitPartyId AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL));
END;
GO

DECLARE @Phase7Permissions TABLE(Name NVARCHAR(150));
INSERT @Phase7Permissions(Name) VALUES(N'Households.View'),(N'Households.Create'),(N'Households.Edit'),(N'Households.ManageMembers'),(N'Visits.View'),(N'Visits.Edit'),(N'VisitParties.View');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) SELECT 2,p.Name,N'Allows '+p.Name+N' operations.',1,SYSUTCDATETIME() FROM @Phase7Permissions p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO
IF OBJECT_ID(N'dbo.Tenant_ProvisionDefaultRoles',N'P') IS NOT NULL
BEGIN
 DECLARE @tid BIGINT; DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Tenants; OPEN c; FETCH NEXT FROM c INTO @tid; WHILE @@FETCH_STATUS=0 BEGIN EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId=@tid; FETCH NEXT FROM c INTO @tid; END; CLOSE c; DEALLOCATE c;
END;
GO
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName=N'CRMSTAFF' AND p.Name IN(N'Households.View',N'Households.Create',N'Households.Edit',N'Households.ManageMembers',N'Visits.View',N'VisitParties.View') AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.6.0') INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.6.0',N'Phase 7 verified households, co-visit parties and factual customer visits',SYSUTCDATETIME(),SUSER_SNAME());
GO

/* ====================== PHASE 7 STRUCTURAL VALIDATION ====================== */
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.6.0')<>1 THROW 51350,'Expected exactly one V1.6.0 row.',1;
IF OBJECT_ID(N'dbo.Households',N'U') IS NULL OR OBJECT_ID(N'dbo.HouseholdMembers',N'U') IS NULL OR OBJECT_ID(N'dbo.VisitParties',N'U') IS NULL OR OBJECT_ID(N'dbo.VisitPartyMembers',N'U') IS NULL OR OBJECT_ID(N'dbo.CustomerVisits',N'U') IS NULL THROW 51351,'One or more Phase 7 tables are missing.',1;
IF OBJECT_ID(N'dbo.Household_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.Household_GetDetail',N'P') IS NULL OR OBJECT_ID(N'dbo.CustomerVisit_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.VisitParty_Search',N'P') IS NULL OR OBJECT_ID(N'dbo.VisitParty_GetDetail',N'P') IS NULL THROW 51352,'One or more Phase 7 stored procedures are missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.VisitPartyMembers') AND name=N'CK_VisitPartyMembers_IdentityXor') THROW 51353,'VisitPartyMember identity XOR constraint is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.HouseholdMembers') AND name=N'CK_HouseholdMembers_RelationshipSource') THROW 51354,'Household relationship source constraint is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.HouseholdMembers') AND name=N'FK_HouseholdMembers_Households_TenantHousehold') THROW 51355,'Tenant-safe Household member FK is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.VisitPartyMembers') AND name=N'FK_VisitPartyMembers_Visitors_TenantStoreVisitor') THROW 51356,'Tenant/store-safe visitor party FK is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'FK_CustomerVisits_Parties_TenantStoreParty') THROW 51357,'Tenant/store-safe visit party FK is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Households.ManageMembers' AND IsActive=1) OR NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'VisitParties.View' AND IsActive=1) THROW 51358,'Phase 7 permissions are missing.',1;
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.Household_Search')) NOT LIKE N'%@TenantId%' OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.Household_Search')) NOT LIKE N'%@AllowedStoreIdsCsv%' THROW 51359,'Household_Search tenant/store predicates are missing.',1;
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.CustomerVisit_Search')) NOT LIKE N'%@AllowedStoreIdsCsv%' OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.VisitParty_Search')) NOT LIKE N'%@AllowedStoreIdsCsv%' THROW 51360,'Visit/party search store predicates are missing.',1;
IF EXISTS(SELECT TenantId,HouseholdCode FROM dbo.Households GROUP BY TenantId,HouseholdCode HAVING COUNT_BIG(*)>1) THROW 51361,'Duplicate household codes detected.',1;
IF EXISTS(SELECT VisitPartyId,CustomerId FROM dbo.VisitPartyMembers WHERE CustomerId IS NOT NULL GROUP BY VisitPartyId,CustomerId HAVING COUNT_BIG(*)>1) THROW 51362,'Duplicate customer party members detected.',1;
IF EXISTS(SELECT VisitPartyId,AnonymousVisitorId FROM dbo.VisitPartyMembers WHERE AnonymousVisitorId IS NOT NULL GROUP BY VisitPartyId,AnonymousVisitorId HAVING COUNT_BIG(*)>1) THROW 51363,'Duplicate visitor party members detected.',1;
GO

PRINT 'Phase 7 SQL Server script completed and validated successfully.';
SELECT VersionNumber,Description,AppliedUtc,AppliedBy FROM dbo.DatabaseVersions WHERE VersionNumber IN(N'V1.5.0',N'V1.6.0') ORDER BY VersionNumber;
SELECT o.type_desc ObjectType,s.name SchemaName,o.name ObjectName FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id WHERE s.name=N'dbo' AND o.name IN(N'Households',N'HouseholdMembers',N'VisitParties',N'VisitPartyMembers',N'CustomerVisits',N'Household_Search',N'Household_GetDetail',N'CustomerVisit_Search',N'VisitParty_Search',N'VisitParty_GetDetail') ORDER BY o.type_desc,o.name;
GO