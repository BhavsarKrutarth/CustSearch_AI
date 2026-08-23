/*
==============================================================
CustSearch AI - Direct SQL Server Phase 6 Runner
File          : database/run-phase6.sql
Database      : CustSearch_AI
SQL Server    : 2022+
Purpose       : Plain T-SQL equivalent of database/run-phase6.ps1.
Usage         : Open this file in SSMS/Azure Data Studio and Execute.
Dependency    : Phase 5 / V1.4.0 must already be installed.
Safety        : Idempotent; does not drop/truncate Phase 6 objects.
==============================================================
*/

USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ------------------------------------------------------------
   PRECHECK - Phase 5 foundation must exist before Phase 6.
------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.DatabaseVersions', N'U') IS NULL
    THROW 51100, 'DatabaseVersions is missing. Complete earlier database phases first.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.4.0')
    THROW 51101, 'Phase 5 V1.4.0 is not installed. Run database/run-phase5.sql first.', 1;
IF OBJECT_ID(N'dbo.Tenants',N'U') IS NULL OR OBJECT_ID(N'dbo.Users',N'U') IS NULL
    THROW 51102, 'Tenants/Users foundation is missing.', 1;
IF OBJECT_ID(N'dbo.Stores',N'U') IS NULL
    THROW 51103, 'dbo.Stores is missing. Run Phase 5 first.', 1;
IF OBJECT_ID(N'dbo.Permissions',N'U') IS NULL OR OBJECT_ID(N'dbo.Roles',N'U') IS NULL OR OBJECT_ID(N'dbo.RolePermissions',N'U') IS NULL
    THROW 51104, 'Authorization foundation is missing.', 1;
GO

/* Phase 6H - deterministic SET options required for filtered indexes. */
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
-- Customer/store visibility uses tenant-safe composite keys.
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
-- Unknown visitors remain anonymous until an explicit conversion.
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
-- Tenant/store predicates are applied before paging.
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
                SELECT DISTINCT TRY_CONVERT(BIGINT,value)
                FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',')
                WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    )
    SELECT Id,VisitorCode,StoreId,FirstSeenUtc,LastSeenUtc,IsActive,ConvertedCustomerId,ConvertedUtc,COUNT_BIG(1) OVER() TotalCount
    FROM Filtered
    ORDER BY LastSeenUtc DESC,Id DESC
    OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- ============================================================
-- SUB-PHASE: 6D - Smart Customer Profile
-- Database foundation only: identity/contact/store visibility +
-- explicit anonymous visitor conversions. Later household/visit/
-- purchase/preference phases are intentionally not fabricated.
-- ============================================================
-- No extra table is required in Phase 6D. Smart Profile reads the
-- factual Phase 6A/6B data created above.
GO

-- ============================================================
-- SUB-PHASE: 6G - Permissions / tenant authorization catalog
-- ============================================================
DECLARE @Phase6Permissions TABLE(Name NVARCHAR(150));
INSERT INTO @Phase6Permissions(Name) VALUES
(N'Customers.View'),(N'Customers.Create'),(N'Customers.Edit'),(N'Visitors.View'),(N'Visitors.Convert');
INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 2,p.Name,N'Allows '+p.Name+N' operations.',1,SYSUTCDATETIME()
FROM @Phase6Permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO

/* Re-provision existing tenant default roles with newly available Phase 6 permissions. */
IF OBJECT_ID(N'dbo.Tenant_ProvisionDefaultRoles',N'P') IS NOT NULL
BEGIN
    DECLARE @Phase6TenantId BIGINT;
    DECLARE Phase6TenantCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Tenants;
    OPEN Phase6TenantCursor;
    FETCH NEXT FROM Phase6TenantCursor INTO @Phase6TenantId;
    WHILE @@FETCH_STATUS=0
    BEGIN
        EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId=@Phase6TenantId;
        FETCH NEXT FROM Phase6TenantCursor INTO @Phase6TenantId;
    END;
    CLOSE Phase6TenantCursor;
    DEALLOCATE Phase6TenantCursor;
END;
GO

/* CRMStaff receives customer/visitor permissions. */
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id
FROM dbo.Roles r
JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1
    AND p.Name IN(N'Customers.View',N'Customers.Create',N'Customers.Edit',N'Visitors.View',N'Visitors.Convert')
WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName=N'CRMSTAFF'
  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO

-- ============================================================
-- SUB-PHASE: 6H - Version ledger
-- ============================================================
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.5.0')
    INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
    VALUES(N'V1.5.0',N'Phase 6 shopper customers, anonymous visitors, smart profile foundation and tenant-safe search',SYSUTCDATETIME(),SUSER_SNAME());
GO

/* ------------------------------------------------------------
   PHASE 6 VALIDATION - same intent as run-phase6.ps1 validation.
------------------------------------------------------------ */
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.5.0') <> 1
    THROW 51150, 'Expected exactly one V1.5.0 Phase 6 version row.', 1;
IF OBJECT_ID(N'dbo.Customers',N'U') IS NULL
    THROW 51151, 'Customers table is missing.', 1;
IF OBJECT_ID(N'dbo.CustomerStoreAssignments',N'U') IS NULL
    THROW 51152, 'CustomerStoreAssignments table is missing.', 1;
IF OBJECT_ID(N'dbo.AnonymousVisitors',N'U') IS NULL
    THROW 51153, 'AnonymousVisitors table is missing.', 1;
IF OBJECT_ID(N'dbo.Customer_Search',N'P') IS NULL
    THROW 51154, 'Customer_Search procedure is missing.', 1;
IF OBJECT_ID(N'dbo.AnonymousVisitor_Search',N'P') IS NULL
    THROW 51155, 'AnonymousVisitor_Search procedure is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.Customer_Search') AND name=N'@TenantId')
   OR NOT EXISTS(SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.Customer_Search') AND name=N'@AllowedStoreIdsCsv')
    THROW 51156, 'Customer_Search tenant/store parameters are missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitor_Search') AND name=N'@TenantId')
   OR NOT EXISTS(SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitor_Search') AND name=N'@AllowedStoreIdsCsv')
    THROW 51157, 'AnonymousVisitor_Search tenant/store parameters are missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_CustomerStoreAssignments_Customers_TenantCustomer')
    THROW 51158, 'Tenant-safe customer assignment/customer FK is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_CustomerStoreAssignments_Stores_TenantStore')
    THROW 51159, 'Tenant-safe customer assignment/store FK is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AnonymousVisitors_Stores_TenantStore')
    THROW 51160, 'Tenant-safe visitor/store FK is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AnonymousVisitors_Customers_TenantCustomer')
    THROW 51161, 'Tenant-safe visitor/customer FK is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'UX_Customers_Tenant_CustomerCode' AND is_unique=1)
    THROW 51162, 'UX_Customers_Tenant_CustomerCode is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'UX_Stores_Tenant_Id' AND is_unique=1)
    THROW 51163, 'UX_Stores_Tenant_Id is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments') AND name=N'UX_CustomerStoreAssignments_Primary' AND is_unique=1)
    THROW 51164, 'UX_CustomerStoreAssignments_Primary is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Customers.View' AND IsActive=1)
    THROW 51165, 'Customers.View permission is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Visitors.Convert' AND IsActive=1)
    THROW 51166, 'Visitors.Convert permission is missing.', 1;
IF EXISTS(
    SELECT TenantId,CustomerCode FROM dbo.Customers
    GROUP BY TenantId,CustomerCode HAVING COUNT_BIG(*)>1
)
    THROW 51167, 'Duplicate customer codes exist inside a tenant.', 1;
IF EXISTS(
    SELECT TenantId,StoreId,VisitorCode FROM dbo.AnonymousVisitors
    GROUP BY TenantId,StoreId,VisitorCode HAVING COUNT_BIG(*)>1
)
    THROW 51168, 'Duplicate visitor codes exist inside tenant/store scope.', 1;
GO

PRINT 'Phase 6 SQL Server script completed and validated successfully.';

/* Useful verification result sets. */
SELECT VersionNumber,Description,AppliedUtc,AppliedBy
FROM dbo.DatabaseVersions
WHERE VersionNumber IN(N'V1.4.0',N'V1.5.0')
ORDER BY VersionNumber;

SELECT o.type_desc AS ObjectType, s.name AS SchemaName, o.name AS ObjectName
FROM sys.objects o
JOIN sys.schemas s ON s.schema_id=o.schema_id
WHERE s.name=N'dbo'
  AND o.name IN(N'Customers',N'CustomerStoreAssignments',N'AnonymousVisitors',N'Customer_Search',N'AnonymousVisitor_Search')
ORDER BY o.type_desc,o.name;
GO
