/*
 CustSearch AI — Phase 6 production database upgrade
 Version: V1.5.0
 Rules: idempotent, no EF migrations, SQL Server 2022, UTC timestamps, tenant/store predicates before paging.
*/
USE [CustSearch_AI];
GO
SET XACT_ABORT ON;
GO
/* Phase 6H — required deterministic SET options for filtered indexes on SQL Server 2022. */
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

-- ============================================================
-- PHASE 6 - SHOPPER CUSTOMERS / ANONYMOUS VISITORS
-- SUB-PHASE: 6A - Customer Management
-- VERSION: V1.5.0
-- ============================================================
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        CustomerCode NVARCHAR(50) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NULL,
        Mobile NVARCHAR(30) NULL,
        Email NVARCHAR(254) NULL,
        Notes NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Customers_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Customers_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Customers_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'UX_Customers_Tenant_CustomerCode')
    CREATE UNIQUE INDEX UX_Customers_Tenant_CustomerCode ON dbo.Customers(TenantId,CustomerCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'UX_Customers_Tenant_Id')
    CREATE UNIQUE INDEX UX_Customers_Tenant_Id ON dbo.Customers(TenantId,Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'IX_Customers_Tenant_Active')
    CREATE INDEX IX_Customers_Tenant_Active ON dbo.Customers(TenantId,IsActive);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'IX_Customers_Tenant_Mobile')
    CREATE INDEX IX_Customers_Tenant_Mobile ON dbo.Customers(TenantId,Mobile) WHERE Mobile IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'IX_Customers_Tenant_Email')
    CREATE INDEX IX_Customers_Tenant_Email ON dbo.Customers(TenantId,Email) WHERE Email IS NOT NULL;
GO

-- ============================================================
-- SUB-PHASE: 6G - Tenant Isolation & Authorization
-- Customer/store visibility uses tenant-safe composite foreign keys.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'UX_Stores_Tenant_Id')
    CREATE UNIQUE INDEX UX_Stores_Tenant_Id ON dbo.Stores(TenantId,Id);
GO

IF OBJECT_ID(N'dbo.CustomerStoreAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerStoreAssignments
    (
        TenantId BIGINT NOT NULL,
        CustomerId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        IsPrimary BIT NOT NULL CONSTRAINT DF_CustomerStoreAssignments_IsPrimary DEFAULT(0),
        AssignedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_CustomerStoreAssignments_AssignedUtc DEFAULT(SYSUTCDATETIME()),
        AssignedByUserId BIGINT NOT NULL,
        CONSTRAINT PK_CustomerStoreAssignments PRIMARY KEY(CustomerId,StoreId),
        CONSTRAINT FK_CustomerStoreAssignments_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_CustomerStoreAssignments_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_CustomerStoreAssignments_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_CustomerStoreAssignments_Users_AssignedBy FOREIGN KEY(AssignedByUserId) REFERENCES dbo.Users(Id)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments') AND name=N'IX_CustomerStoreAssignments_Tenant_Store')
    CREATE INDEX IX_CustomerStoreAssignments_Tenant_Store ON dbo.CustomerStoreAssignments(TenantId,StoreId,CustomerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments') AND name=N'IX_CustomerStoreAssignments_Customer_Primary')
    CREATE INDEX IX_CustomerStoreAssignments_Customer_Primary ON dbo.CustomerStoreAssignments(CustomerId,IsPrimary);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments') AND name=N'UX_CustomerStoreAssignments_Primary')
    CREATE UNIQUE INDEX UX_CustomerStoreAssignments_Primary ON dbo.CustomerStoreAssignments(CustomerId) WHERE IsPrimary=1;
GO

-- ============================================================
-- SUB-PHASE: 6B - Anonymous Visitors
-- No biometric/external identity fields are stored here; conversion is explicit and audited.
-- ============================================================
IF OBJECT_ID(N'dbo.AnonymousVisitors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnonymousVisitors
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AnonymousVisitors PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        VisitorCode NVARCHAR(50) NOT NULL,
        FirstSeenUtc DATETIME2(7) NOT NULL,
        LastSeenUtc DATETIME2(7) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AnonymousVisitors_IsActive DEFAULT(1),
        ConvertedCustomerId BIGINT NULL,
        ConvertedUtc DATETIME2(7) NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_AnonymousVisitors_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_AnonymousVisitors_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_AnonymousVisitors_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_AnonymousVisitors_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_AnonymousVisitors_Customers_TenantCustomer FOREIGN KEY(TenantId,ConvertedCustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT CK_AnonymousVisitors_LastSeen CHECK(LastSeenUtc >= FirstSeenUtc),
        CONSTRAINT CK_AnonymousVisitors_Conversion CHECK((ConvertedCustomerId IS NULL AND ConvertedUtc IS NULL) OR (ConvertedCustomerId IS NOT NULL AND ConvertedUtc IS NOT NULL))
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'UX_AnonymousVisitors_Tenant_Store_Code')
    CREATE UNIQUE INDEX UX_AnonymousVisitors_Tenant_Store_Code ON dbo.AnonymousVisitors(TenantId,StoreId,VisitorCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'IX_AnonymousVisitors_Tenant_Store_Active_LastSeen')
    CREATE INDEX IX_AnonymousVisitors_Tenant_Store_Active_LastSeen ON dbo.AnonymousVisitors(TenantId,StoreId,IsActive,LastSeenUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'IX_AnonymousVisitors_Tenant_ConvertedCustomer')
    CREATE INDEX IX_AnonymousVisitors_Tenant_ConvertedCustomer ON dbo.AnonymousVisitors(TenantId,ConvertedCustomerId) WHERE ConvertedCustomerId IS NOT NULL;
GO

-- ============================================================
-- SUB-PHASE: 6C - Customer Search
-- Tenant/store authorization is applied before paging. NULL AllowedStoreIdsCsv means tenant-wide role.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Customer_Search
    @TenantId BIGINT,
    @AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,
    @StoreId BIGINT=NULL,
    @Search NVARCHAR(200)=NULL,
    @ActiveOnly BIT=0,
    @PageNumber INT=1,
    @PageSize INT=25
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber < 1 SET @PageNumber=1;
    IF @PageSize < 1 SET @PageSize=25;
    IF @PageSize > 100 SET @PageSize=100;

    ;WITH Filtered AS
    (
        SELECT c.Id,c.CustomerCode,c.FirstName,c.LastName,c.Mobile,c.Email,c.IsActive,c.UpdatedUtc
        FROM dbo.Customers c
        WHERE c.TenantId=@TenantId
          AND (@ActiveOnly=0 OR c.IsActive=1)
          AND (@Search IS NULL OR c.CustomerCode LIKE N'%'+@Search+N'%' OR c.FirstName LIKE N'%'+@Search+N'%'
               OR c.LastName LIKE N'%'+@Search+N'%' OR c.Mobile LIKE N'%'+@Search+N'%' OR c.Email LIKE N'%'+@Search+N'%')
          AND (@StoreId IS NULL OR EXISTS(
                SELECT 1 FROM dbo.CustomerStoreAssignments cs
                WHERE cs.TenantId=@TenantId AND cs.CustomerId=c.Id AND cs.StoreId=@StoreId))
          AND (@AllowedStoreIdsCsv IS NULL OR EXISTS(
                SELECT 1
                FROM dbo.CustomerStoreAssignments cs
                INNER JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=cs.StoreId
                WHERE cs.TenantId=@TenantId AND cs.CustomerId=c.Id))
    )
    SELECT Id,CustomerCode,FirstName,LastName,Mobile,Email,IsActive,UpdatedUtc,COUNT_BIG(1) OVER() TotalCount
    FROM Filtered
    ORDER BY UpdatedUtc DESC,Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.AnonymousVisitor_Search
    @TenantId BIGINT,
    @AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,
    @StoreId BIGINT=NULL,
    @Search NVARCHAR(200)=NULL,
    @ActiveOnly BIT=0,
    @PageNumber INT=1,
    @PageSize INT=25
AS
BEGIN
    SET NOCOUNT ON;
    IF @PageNumber < 1 SET @PageNumber=1;
    IF @PageSize < 1 SET @PageSize=25;
    IF @PageSize > 100 SET @PageSize=100;

    ;WITH Filtered AS
    (
        SELECT v.Id,v.VisitorCode,v.StoreId,v.FirstSeenUtc,v.LastSeenUtc,v.IsActive,v.ConvertedCustomerId,v.ConvertedUtc
        FROM dbo.AnonymousVisitors v
        WHERE v.TenantId=@TenantId
          AND (@ActiveOnly=0 OR v.IsActive=1)
          AND (@StoreId IS NULL OR v.StoreId=@StoreId)
          AND (@Search IS NULL OR v.VisitorCode LIKE N'%'+@Search+N'%')
          AND (@AllowedStoreIdsCsv IS NULL OR v.StoreId IN(
                SELECT DISTINCT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    )
    SELECT Id,VisitorCode,StoreId,FirstSeenUtc,LastSeenUtc,IsActive,ConvertedCustomerId,ConvertedUtc,COUNT_BIG(1) OVER() TotalCount
    FROM Filtered
    ORDER BY LastSeenUtc DESC,Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- ============================================================
-- SUB-PHASE: 6G - Tenant Isolation & Authorization
-- Reuse the stable Customers.* and Visitors.* permission names already defined by the application catalog.
-- ============================================================
DECLARE @Phase6Permissions TABLE(Name NVARCHAR(150));
INSERT INTO @Phase6Permissions(Name) VALUES
(N'Customers.View'),(N'Customers.Create'),(N'Customers.Edit'),(N'Visitors.View'),(N'Visitors.Convert');
INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 2,p.Name,N'Allows '+p.Name+N' operations.',1,SYSUTCDATETIME()
FROM @Phase6Permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO

-- Re-run the existing Phase 5 role provisioner so TenantAdmin/TenantOwner/ShopOwner/StoreManager/SalesStaff receive
-- newly materialized stable permissions according to the already-established role policy.
IF OBJECT_ID(N'dbo.Tenant_ProvisionDefaultRoles',N'P') IS NOT NULL
BEGIN
    DECLARE @Phase6TenantId BIGINT;
    DECLARE Phase6TenantCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Tenants;
    OPEN Phase6TenantCursor; FETCH NEXT FROM Phase6TenantCursor INTO @Phase6TenantId;
    WHILE @@FETCH_STATUS=0
    BEGIN
        EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId=@Phase6TenantId;
        FETCH NEXT FROM Phase6TenantCursor INTO @Phase6TenantId;
    END
    CLOSE Phase6TenantCursor; DEALLOCATE Phase6TenantCursor;
END;
GO

-- CRMStaff receives the customer/visitor permissions required for Phase 6 CRM operations.
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id
FROM dbo.Roles r
JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1 AND p.Name IN(N'Customers.View',N'Customers.Create',N'Customers.Edit',N'Visitors.View',N'Visitors.Convert')
WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName=N'CRMSTAFF'
  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO

-- ============================================================
-- SUB-PHASE: 6H - E2E, Database & Documentation
-- Version ledger is idempotent and records Phase 6 only once.
-- ============================================================
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.5.0')
    INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
    VALUES(N'V1.5.0',N'Phase 6 shopper customers, anonymous visitors, smart profile foundation and tenant-safe search',SYSUTCDATETIME(),SUSER_SNAME());
GO
