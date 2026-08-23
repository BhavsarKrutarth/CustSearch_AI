/*
 CustSearch AI — Phase 8 production database upgrade
 Version: V1.7.0
 Rules: SQL Server 2022, idempotent, UTC, no EF migrations, tenant/store predicates before paging/aggregation.
*/
USE [CustSearch_AI];
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
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

IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.6.0')
    THROW 51300,'Phase 7 V1.6.0 must be installed before Phase 8.',1;
GO

-- Supporting tenant-safe alternate keys from earlier phases.
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategories') AND name=N'UX_ProductCategories_Tenant_Id')
    CREATE UNIQUE INDEX UX_ProductCategories_Tenant_Id ON dbo.ProductCategories(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerVisits') AND name=N'UX_CustomerVisits_Tenant_Store_Id')
    CREATE UNIQUE INDEX UX_CustomerVisits_Tenant_Store_Id ON dbo.CustomerVisits(TenantId,StoreId,Id);
GO

-- ============================================================
-- 8A - Product catalog
-- ============================================================
IF OBJECT_ID(N'dbo.Products',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        ProductCode NVARCHAR(50) NOT NULL,
        Barcode NVARCHAR(100) NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        CategoryId BIGINT NOT NULL,
        Brand NVARCHAR(150) NULL,
        UnitName NVARCHAR(50) NOT NULL,
        SalePrice DECIMAL(18,2) NOT NULL,
        CostPrice DECIMAL(18,2) NULL,
        TaxPercent DECIMAL(9,4) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Products_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Products_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Products_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_Products_Categories_TenantCategory FOREIGN KEY(TenantId,CategoryId) REFERENCES dbo.ProductCategories(TenantId,Id),
        CONSTRAINT CK_Products_SalePrice CHECK(SalePrice>=0),
        CONSTRAINT CK_Products_CostPrice CHECK(CostPrice IS NULL OR CostPrice>=0),
        CONSTRAINT CK_Products_TaxPercent CHECK(TaxPercent IS NULL OR TaxPercent BETWEEN 0 AND 100)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Products') AND name=N'UX_Products_Tenant_Code')
    CREATE UNIQUE INDEX UX_Products_Tenant_Code ON dbo.Products(TenantId,ProductCode);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Products') AND name=N'UX_Products_Tenant_Id')
    CREATE UNIQUE INDEX UX_Products_Tenant_Id ON dbo.Products(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Products') AND name=N'UX_Products_Tenant_Barcode')
    CREATE UNIQUE INDEX UX_Products_Tenant_Barcode ON dbo.Products(TenantId,Barcode) WHERE Barcode IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Products') AND name=N'IX_Products_Tenant_Category_Active')
    CREATE INDEX IX_Products_Tenant_Category_Active ON dbo.Products(TenantId,CategoryId,IsActive) INCLUDE(ProductCode,Name,SalePrice,TaxPercent);
GO

IF OBJECT_ID(N'dbo.ProductStoreAvailabilities',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductStoreAvailabilities
    (
        TenantId BIGINT NOT NULL,
        ProductId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ProductStoreAvailabilities_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ProductStoreAvailabilities_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ProductStoreAvailabilities_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT PK_ProductStoreAvailabilities PRIMARY KEY(ProductId,StoreId),
        CONSTRAINT FK_ProductStoreAvailabilities_Products_TenantProduct FOREIGN KEY(TenantId,ProductId) REFERENCES dbo.Products(TenantId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_ProductStoreAvailabilities_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id) ON DELETE CASCADE
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductStoreAvailabilities') AND name=N'IX_ProductStoreAvailabilities_Tenant_Store_Active')
    CREATE INDEX IX_ProductStoreAvailabilities_Tenant_Store_Active ON dbo.ProductStoreAvailabilities(TenantId,StoreId,IsActive,ProductId);
GO

-- ============================================================
-- 8B - Retail invoice header
-- ============================================================
IF OBJECT_ID(N'dbo.RetailInvoices',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RetailInvoices
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetailInvoices PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        InvoiceNumber NVARCHAR(50) NOT NULL,
        CustomerId BIGINT NULL,
        HouseholdId BIGINT NULL,
        CustomerVisitId BIGINT NULL,
        VisitPartyId BIGINT NULL,
        InvoiceUtc DATETIME2(7) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Subtotal DEFAULT(0),
        DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Discount DEFAULT(0),
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Tax DEFAULT(0),
        GrandTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_GrandTotal DEFAULT(0),
        PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Paid DEFAULT(0),
        BalanceAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_RetailInvoices_Balance DEFAULT(0),
        Status TINYINT NOT NULL CONSTRAINT DF_RetailInvoices_Status DEFAULT(1),
        Notes NVARCHAR(1000) NULL,
        CreatedByUserId BIGINT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailInvoices_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailInvoices_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CancelledUtc DATETIME2(7) NULL,
        CancelledByUserId BIGINT NULL,
        CancellationReason NVARCHAR(500) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_RetailInvoices_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_RetailInvoices_Stores_TenantStore FOREIGN KEY(TenantId,StoreId) REFERENCES dbo.Stores(TenantId,Id),
        CONSTRAINT FK_RetailInvoices_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_RetailInvoices_Households_TenantHousehold FOREIGN KEY(TenantId,HouseholdId) REFERENCES dbo.Households(TenantId,Id),
        CONSTRAINT FK_RetailInvoices_Visits_TenantStoreVisit FOREIGN KEY(TenantId,StoreId,CustomerVisitId) REFERENCES dbo.CustomerVisits(TenantId,StoreId,Id),
        CONSTRAINT FK_RetailInvoices_Parties_TenantStoreParty FOREIGN KEY(TenantId,StoreId,VisitPartyId) REFERENCES dbo.VisitParties(TenantId,StoreId,Id),
        CONSTRAINT FK_RetailInvoices_Users_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_RetailInvoices_Users_CancelledBy FOREIGN KEY(CancelledByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_RetailInvoices_Status CHECK(Status BETWEEN 1 AND 5),
        CONSTRAINT CK_RetailInvoices_Amounts CHECK(Subtotal>=0 AND DiscountAmount>=0 AND TaxAmount>=0 AND GrandTotal>=0 AND PaidAmount>=0 AND BalanceAmount>=0 AND DiscountAmount<=Subtotal AND GrandTotal=Subtotal-DiscountAmount+TaxAmount AND PaidAmount<=GrandTotal AND BalanceAmount=GrandTotal-PaidAmount),
        CONSTRAINT CK_RetailInvoices_Cancellation CHECK((Status=5 AND CancelledUtc IS NOT NULL AND CancelledByUserId IS NOT NULL AND CancellationReason IS NOT NULL) OR (Status<>5 AND CancelledUtc IS NULL AND CancelledByUserId IS NULL))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'UX_RetailInvoices_Tenant_Store_Number')
    CREATE UNIQUE INDEX UX_RetailInvoices_Tenant_Store_Number ON dbo.RetailInvoices(TenantId,StoreId,InvoiceNumber);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'UX_RetailInvoices_Tenant_Id')
    CREATE UNIQUE INDEX UX_RetailInvoices_Tenant_Id ON dbo.RetailInvoices(TenantId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'UX_RetailInvoices_Tenant_Store_Id')
    CREATE UNIQUE INDEX UX_RetailInvoices_Tenant_Store_Id ON dbo.RetailInvoices(TenantId,StoreId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'IX_RetailInvoices_Tenant_Store_Date')
    CREATE INDEX IX_RetailInvoices_Tenant_Store_Date ON dbo.RetailInvoices(TenantId,StoreId,InvoiceUtc DESC) INCLUDE(InvoiceNumber,CustomerId,GrandTotal,PaidAmount,BalanceAmount,Status);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'IX_RetailInvoices_Tenant_Customer_Date')
    CREATE INDEX IX_RetailInvoices_Tenant_Customer_Date ON dbo.RetailInvoices(TenantId,CustomerId,InvoiceUtc DESC) WHERE CustomerId IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoices') AND name=N'IX_RetailInvoices_Tenant_Status_Date')
    CREATE INDEX IX_RetailInvoices_Tenant_Status_Date ON dbo.RetailInvoices(TenantId,Status,InvoiceUtc DESC);
GO

-- ============================================================
-- 8C - Invoice item snapshots
-- ============================================================
IF OBJECT_ID(N'dbo.RetailInvoiceItems',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RetailInvoiceItems
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetailInvoiceItems PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        InvoiceId BIGINT NOT NULL,
        ProductId BIGINT NULL,
        CategoryId BIGINT NULL,
        ProductCodeSnapshot NVARCHAR(50) NOT NULL,
        ProductNameSnapshot NVARCHAR(200) NOT NULL,
        CategoryNameSnapshot NVARCHAR(150) NULL,
        Quantity DECIMAL(18,4) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        DiscountAmount DECIMAL(18,2) NOT NULL,
        TaxPercent DECIMAL(9,4) NOT NULL,
        TaxAmount DECIMAL(18,2) NOT NULL,
        LineSubtotal DECIMAL(18,2) NOT NULL,
        LineTotal DECIMAL(18,2) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailInvoiceItems_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_RetailInvoiceItems_Invoices_TenantInvoice FOREIGN KEY(TenantId,InvoiceId) REFERENCES dbo.RetailInvoices(TenantId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_RetailInvoiceItems_Products_TenantProduct FOREIGN KEY(TenantId,ProductId) REFERENCES dbo.Products(TenantId,Id),
        CONSTRAINT FK_RetailInvoiceItems_Categories_TenantCategory FOREIGN KEY(TenantId,CategoryId) REFERENCES dbo.ProductCategories(TenantId,Id),
        CONSTRAINT CK_RetailInvoiceItems_Quantity CHECK(Quantity>0),
        CONSTRAINT CK_RetailInvoiceItems_Amounts CHECK(UnitPrice>=0 AND DiscountAmount>=0 AND TaxPercent BETWEEN 0 AND 100 AND TaxAmount>=0 AND LineSubtotal>=0 AND LineTotal>=0 AND DiscountAmount<=LineSubtotal AND LineTotal=LineSubtotal-DiscountAmount+TaxAmount)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItems') AND name=N'UX_RetailInvoiceItems_Tenant_Invoice_Id')
    CREATE UNIQUE INDEX UX_RetailInvoiceItems_Tenant_Invoice_Id ON dbo.RetailInvoiceItems(TenantId,InvoiceId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItems') AND name=N'IX_RetailInvoiceItems_Tenant_Product')
    CREATE INDEX IX_RetailInvoiceItems_Tenant_Product ON dbo.RetailInvoiceItems(TenantId,ProductId) INCLUDE(InvoiceId,Quantity,LineTotal);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItems') AND name=N'IX_RetailInvoiceItems_Tenant_Category')
    CREATE INDEX IX_RetailInvoiceItems_Tenant_Category ON dbo.RetailInvoiceItems(TenantId,CategoryId) INCLUDE(InvoiceId,LineTotal);
GO

-- ============================================================
-- 8D - Payments
-- ============================================================
IF OBJECT_ID(N'dbo.RetailInvoicePayments',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RetailInvoicePayments
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetailInvoicePayments PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        InvoiceId BIGINT NOT NULL,
        PaymentReference NVARCHAR(100) NOT NULL,
        PaymentMethod TINYINT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentUtc DATETIME2(7) NOT NULL,
        Status TINYINT NOT NULL,
        ExternalTransactionId NVARCHAR(150) NULL,
        Notes NVARCHAR(500) NULL,
        ReceivedByUserId BIGINT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailInvoicePayments_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_RetailInvoicePayments_Invoices_TenantStoreInvoice FOREIGN KEY(TenantId,StoreId,InvoiceId) REFERENCES dbo.RetailInvoices(TenantId,StoreId,Id),
        CONSTRAINT FK_RetailInvoicePayments_Users_ReceivedBy FOREIGN KEY(ReceivedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_RetailInvoicePayments_Method CHECK(PaymentMethod BETWEEN 1 AND 5),
        CONSTRAINT CK_RetailInvoicePayments_Status CHECK(Status BETWEEN 1 AND 4),
        CONSTRAINT CK_RetailInvoicePayments_Amount CHECK(Amount>0)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoicePayments') AND name=N'UX_RetailInvoicePayments_Tenant_Reference')
    CREATE UNIQUE INDEX UX_RetailInvoicePayments_Tenant_Reference ON dbo.RetailInvoicePayments(TenantId,PaymentReference);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoicePayments') AND name=N'IX_RetailInvoicePayments_Tenant_Store_Date')
    CREATE INDEX IX_RetailInvoicePayments_Tenant_Store_Date ON dbo.RetailInvoicePayments(TenantId,StoreId,PaymentUtc DESC) INCLUDE(InvoiceId,PaymentMethod,Amount,Status);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoicePayments') AND name=N'IX_RetailInvoicePayments_Tenant_Invoice_Status')
    CREATE INDEX IX_RetailInvoicePayments_Tenant_Invoice_Status ON dbo.RetailInvoicePayments(TenantId,InvoiceId,Status) INCLUDE(Amount);
GO

-- ============================================================
-- 8E - Explicit invoice participants
-- ============================================================
IF OBJECT_ID(N'dbo.RetailInvoiceParticipants',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RetailInvoiceParticipants
    (
        TenantId BIGINT NOT NULL,
        InvoiceId BIGINT NOT NULL,
        CustomerId BIGINT NOT NULL,
        ParticipationType TINYINT NOT NULL,
        IsPayer BIT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailInvoiceParticipants_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT PK_RetailInvoiceParticipants PRIMARY KEY(InvoiceId,CustomerId),
        CONSTRAINT FK_RetailInvoiceParticipants_Invoices_TenantInvoice FOREIGN KEY(TenantId,InvoiceId) REFERENCES dbo.RetailInvoices(TenantId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_RetailInvoiceParticipants_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT CK_RetailInvoiceParticipants_Type CHECK(ParticipationType BETWEEN 1 AND 4),
        CONSTRAINT CK_RetailInvoiceParticipants_Payer CHECK((ParticipationType=1 AND IsPayer=1) OR (ParticipationType<>1 AND IsPayer=0))
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceParticipants') AND name=N'UX_RetailInvoiceParticipants_OnePayer')
    CREATE UNIQUE INDEX UX_RetailInvoiceParticipants_OnePayer ON dbo.RetailInvoiceParticipants(InvoiceId) WHERE IsPayer=1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceParticipants') AND name=N'IX_RetailInvoiceParticipants_Tenant_Customer')
    CREATE INDEX IX_RetailInvoiceParticipants_Tenant_Customer ON dbo.RetailInvoiceParticipants(TenantId,CustomerId,InvoiceId);
GO

-- ============================================================
-- 8F - Explicit spend attribution
-- ============================================================
IF OBJECT_ID(N'dbo.RetailInvoiceItemAttributions',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RetailInvoiceItemAttributions
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RetailInvoiceItemAttributions PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        InvoiceId BIGINT NOT NULL,
        InvoiceItemId BIGINT NOT NULL,
        CustomerId BIGINT NOT NULL,
        AttributionType TINYINT NOT NULL,
        QuantityAttributed DECIMAL(18,4) NULL,
        AmountAttributed DECIMAL(18,2) NOT NULL,
        Source TINYINT NOT NULL,
        CreatedByUserId BIGINT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_RetailInvoiceItemAttributions_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_RetailAttributions_Invoices_TenantInvoice FOREIGN KEY(TenantId,InvoiceId) REFERENCES dbo.RetailInvoices(TenantId,Id) ON DELETE CASCADE,
        CONSTRAINT FK_RetailAttributions_Items_TenantInvoiceItem FOREIGN KEY(TenantId,InvoiceId,InvoiceItemId) REFERENCES dbo.RetailInvoiceItems(TenantId,InvoiceId,Id),
        CONSTRAINT FK_RetailAttributions_Customers_TenantCustomer FOREIGN KEY(TenantId,CustomerId) REFERENCES dbo.Customers(TenantId,Id),
        CONSTRAINT FK_RetailAttributions_Users_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_RetailAttributions_Type CHECK(AttributionType BETWEEN 1 AND 3),
        CONSTRAINT CK_RetailAttributions_Source CHECK(Source BETWEEN 1 AND 4),
        CONSTRAINT CK_RetailAttributions_Amount CHECK(AmountAttributed>0),
        CONSTRAINT CK_RetailAttributions_Quantity CHECK(QuantityAttributed IS NULL OR QuantityAttributed>0)
    );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItemAttributions') AND name=N'UX_RetailAttributions_Item_Customer')
    CREATE UNIQUE INDEX UX_RetailAttributions_Item_Customer ON dbo.RetailInvoiceItemAttributions(TenantId,InvoiceItemId,CustomerId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RetailInvoiceItemAttributions') AND name=N'IX_RetailAttributions_Tenant_Customer')
    CREATE INDEX IX_RetailAttributions_Tenant_Customer ON dbo.RetailInvoiceItemAttributions(TenantId,CustomerId,InvoiceId) INCLUDE(AmountAttributed);
GO

-- ============================================================
-- 8H - Dapper search/report procedures. Tenant/store scope occurs before paging/aggregation.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.Product_Search
    @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@CategoryId BIGINT=NULL,@Search NVARCHAR(200)=NULL,@ActiveOnly BIT=0,@PageNumber INT=1,@PageSize INT=25
AS
BEGIN
    SET NOCOUNT ON;IF @PageNumber<1 SET @PageNumber=1;IF @PageSize<1 SET @PageSize=25;IF @PageSize>100 SET @PageSize=100;
    ;WITH Filtered AS
    (
        SELECT p.Id,p.ProductCode,p.Barcode,p.Name,p.CategoryId,c.Name CategoryName,p.Brand,p.UnitName,p.SalePrice,p.TaxPercent,p.IsActive
        FROM dbo.Products p JOIN dbo.ProductCategories c ON c.TenantId=p.TenantId AND c.Id=p.CategoryId
        WHERE p.TenantId=@TenantId
          AND (@ActiveOnly=0 OR p.IsActive=1)
          AND (@CategoryId IS NULL OR p.CategoryId=@CategoryId)
          AND (@Search IS NULL OR p.ProductCode LIKE N'%'+@Search+N'%' OR p.Barcode LIKE N'%'+@Search+N'%' OR p.Name LIKE N'%'+@Search+N'%' OR p.Brand LIKE N'%'+@Search+N'%')
          AND (@StoreId IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities a WHERE a.TenantId=@TenantId AND a.ProductId=p.Id AND a.IsActive=1) OR EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities a WHERE a.TenantId=@TenantId AND a.ProductId=p.Id AND a.StoreId=@StoreId AND a.IsActive=1))
          AND (@AllowedStoreIdsCsv IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities a WHERE a.TenantId=@TenantId AND a.ProductId=p.Id AND a.IsActive=1) OR EXISTS(SELECT 1 FROM dbo.ProductStoreAvailabilities a JOIN STRING_SPLIT(@AllowedStoreIdsCsv,N',') s ON TRY_CONVERT(BIGINT,s.value)=a.StoreId WHERE a.TenantId=@TenantId AND a.ProductId=p.Id AND a.IsActive=1))
    )
    SELECT *,COUNT_BIG(1) OVER() TotalCount FROM Filtered ORDER BY Name,Id OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.RetailInvoice_Search
    @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@CustomerId BIGINT=NULL,@Status TINYINT=NULL,@Search NVARCHAR(200)=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@PageNumber INT=1,@PageSize INT=25
AS
BEGIN
    SET NOCOUNT ON;IF @PageNumber<1 SET @PageNumber=1;IF @PageSize<1 SET @PageSize=25;IF @PageSize>100 SET @PageSize=100;
    ;WITH Filtered AS
    (
        SELECT i.Id,i.InvoiceNumber,i.StoreId,i.CustomerId,c.CustomerCode,CASE WHEN c.Id IS NULL THEN NULL ELSE CONCAT(c.FirstName,CASE WHEN c.LastName IS NULL THEN N'' ELSE N' '+c.LastName END) END CustomerName,i.InvoiceUtc,i.GrandTotal,i.PaidAmount,i.BalanceAmount,i.Status
        FROM dbo.RetailInvoices i LEFT JOIN dbo.Customers c ON c.TenantId=i.TenantId AND c.Id=i.CustomerId
        WHERE i.TenantId=@TenantId AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@CustomerId IS NULL OR i.CustomerId=@CustomerId) AND (@Status IS NULL OR i.Status=@Status)
          AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc)
          AND (@Search IS NULL OR i.InvoiceNumber LIKE N'%'+@Search+N'%' OR c.CustomerCode LIKE N'%'+@Search+N'%' OR c.FirstName LIKE N'%'+@Search+N'%' OR c.LastName LIKE N'%'+@Search+N'%')
          AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    )
    SELECT *,COUNT_BIG(1) OVER() TotalCount FROM Filtered ORDER BY InvoiceUtc DESC,Id DESC OFFSET (@PageNumber-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

CREATE OR ALTER PROCEDURE dbo.RetailInvoice_GetDetail @TenantId BIGINT,@InvoiceId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.* FROM dbo.RetailInvoices i WHERE i.TenantId=@TenantId AND i.Id=@InvoiceId AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL));
    SELECT x.* FROM dbo.RetailInvoiceItems x JOIN dbo.RetailInvoices i ON i.TenantId=x.TenantId AND i.Id=x.InvoiceId WHERE x.TenantId=@TenantId AND x.InvoiceId=@InvoiceId AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)) ORDER BY x.Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.CustomerPurchaseHistory_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@CustomerId BIGINT,@RecentCount INT=10
AS
BEGIN
    SET NOCOUNT ON;IF @RecentCount<1 SET @RecentCount=10;IF @RecentCount>100 SET @RecentCount=100;
    ;WITH VisibleInvoices AS
    (
      SELECT DISTINCT i.Id,i.StoreId,i.InvoiceUtc,i.InvoiceNumber,i.Status,i.GrandTotal
      FROM dbo.RetailInvoices i
      LEFT JOIN dbo.RetailInvoiceParticipants p ON p.TenantId=i.TenantId AND p.InvoiceId=i.Id AND p.CustomerId=@CustomerId
      LEFT JOIN dbo.RetailInvoiceItemAttributions a ON a.TenantId=i.TenantId AND a.InvoiceId=i.Id AND a.CustomerId=@CustomerId
      WHERE i.TenantId=@TenantId AND i.Status IN(2,3,4) AND (i.CustomerId=@CustomerId OR p.CustomerId IS NOT NULL OR a.CustomerId IS NOT NULL)
        AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    )
    SELECT @CustomerId CustomerId,COUNT_BIG(*) InvoiceCount,
      COALESCE((SELECT SUM(i.GrandTotal) FROM dbo.RetailInvoiceParticipants p JOIN dbo.RetailInvoices i ON i.TenantId=p.TenantId AND i.Id=p.InvoiceId WHERE p.TenantId=@TenantId AND p.CustomerId=@CustomerId AND p.IsPayer=1 AND i.Status IN(2,3,4) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))),0) PayerSpend,
      COALESCE((SELECT SUM(a.AmountAttributed) FROM dbo.RetailInvoiceItemAttributions a JOIN dbo.RetailInvoices i ON i.TenantId=a.TenantId AND i.Id=a.InvoiceId WHERE a.TenantId=@TenantId AND a.CustomerId=@CustomerId AND i.Status IN(2,3,4) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))),0) ExplicitAttributedSpend,
      MAX(InvoiceUtc) LastPurchaseUtc,(SELECT TOP(1) StoreId FROM VisibleInvoices ORDER BY InvoiceUtc DESC,Id DESC) LastPurchaseStoreId
    FROM VisibleInvoices;

    SELECT TOP(@RecentCount) v.Id InvoiceId,v.InvoiceNumber,v.StoreId,v.InvoiceUtc,v.Status,v.GrandTotal,
      CASE WHEN EXISTS(SELECT 1 FROM dbo.RetailInvoiceParticipants p WHERE p.TenantId=@TenantId AND p.InvoiceId=v.Id AND p.CustomerId=@CustomerId AND p.IsPayer=1) THEN v.GrandTotal ELSE CAST(0 AS DECIMAL(18,2)) END PayerAmount,
      COALESCE((SELECT SUM(a.AmountAttributed) FROM dbo.RetailInvoiceItemAttributions a WHERE a.TenantId=@TenantId AND a.InvoiceId=v.Id AND a.CustomerId=@CustomerId),0) AttributedAmount
    FROM VisibleInvoices v ORDER BY v.InvoiceUtc DESC,v.Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.HouseholdPurchaseSummary_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@HouseholdId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    ;WITH VerifiedMembers AS(SELECT CustomerId FROM dbo.HouseholdMembers WHERE TenantId=@TenantId AND HouseholdId=@HouseholdId AND IsActive=1 AND IsVerified=1),
    H AS(SELECT DISTINCT i.Id,i.InvoiceUtc FROM dbo.RetailInvoiceItemAttributions a JOIN VerifiedMembers m ON m.CustomerId=a.CustomerId JOIN dbo.RetailInvoices i ON i.TenantId=a.TenantId AND i.Id=a.InvoiceId WHERE a.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)))
    SELECT @HouseholdId HouseholdId,(SELECT COUNT_BIG(*) FROM H) InvoiceCount,
      COALESCE((SELECT SUM(a.AmountAttributed) FROM dbo.RetailInvoiceItemAttributions a JOIN VerifiedMembers m ON m.CustomerId=a.CustomerId JOIN dbo.RetailInvoices i ON i.TenantId=a.TenantId AND i.Id=a.InvoiceId WHERE a.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))),0) VerifiedMemberAttributedSpend,
      (SELECT MAX(InvoiceUtc) FROM H) LastPurchaseUtc;
END;
GO

CREATE OR ALTER PROCEDURE dbo.RetailSalesSummary_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COALESCE(SUM(Subtotal),0) GrossSales,COALESCE(SUM(DiscountAmount),0) Discounts,COALESCE(SUM(TaxAmount),0) Tax,COALESCE(SUM(GrandTotal),0) NetSales,COALESCE(SUM(PaidAmount),0) PaidAmount,COALESCE(SUM(BalanceAmount),0) OutstandingAmount,COUNT_BIG(*) InvoiceCount
    FROM dbo.RetailInvoices i WHERE i.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL));
END;
GO

CREATE OR ALTER PROCEDURE dbo.RetailSalesByProduct_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@Top INT=20
AS
BEGIN
    SET NOCOUNT ON;IF @Top<1 SET @Top=20;IF @Top>100 SET @Top=100;
    SELECT TOP(@Top) COALESCE(x.ProductId,0) Id,x.ProductCodeSnapshot Code,x.ProductNameSnapshot Name,SUM(x.LineTotal) NetSales,COUNT(DISTINCT i.Id) InvoiceCount
    FROM dbo.RetailInvoiceItems x JOIN dbo.RetailInvoices i ON i.TenantId=x.TenantId AND i.Id=x.InvoiceId
    WHERE i.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    GROUP BY x.ProductId,x.ProductCodeSnapshot,x.ProductNameSnapshot ORDER BY NetSales DESC,Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.RetailSalesByCategory_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL,@Top INT=20
AS
BEGIN
    SET NOCOUNT ON;IF @Top<1 SET @Top=20;IF @Top>100 SET @Top=100;
    SELECT TOP(@Top) COALESCE(x.CategoryId,0) Id,COALESCE(c.CategoryCode,N'UNCATEGORIZED') Code,COALESCE(x.CategoryNameSnapshot,N'Uncategorized') Name,SUM(x.LineTotal) NetSales,COUNT(DISTINCT i.Id) InvoiceCount
    FROM dbo.RetailInvoiceItems x JOIN dbo.RetailInvoices i ON i.TenantId=x.TenantId AND i.Id=x.InvoiceId LEFT JOIN dbo.ProductCategories c ON c.TenantId=x.TenantId AND c.Id=x.CategoryId
    WHERE i.TenantId=@TenantId AND i.Status IN(2,3,4) AND (@StoreId IS NULL OR i.StoreId=@StoreId) AND (@FromUtc IS NULL OR i.InvoiceUtc>=@FromUtc) AND (@ToUtc IS NULL OR i.InvoiceUtc<@ToUtc) AND (@AllowedStoreIdsCsv IS NULL OR i.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL))
    GROUP BY x.CategoryId,c.CategoryCode,x.CategoryNameSnapshot ORDER BY NetSales DESC,Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.RetailPaymentSummary_Get @TenantId BIGINT,@AllowedStoreIdsCsv NVARCHAR(MAX)=NULL,@StoreId BIGINT=NULL,@FromUtc DATETIME2(7)=NULL,@ToUtc DATETIME2(7)=NULL
AS
BEGIN
    SET NOCOUNT ON;SELECT PaymentMethod,SUM(Amount) Amount,COUNT_BIG(*) PaymentCount FROM dbo.RetailInvoicePayments p WHERE p.TenantId=@TenantId AND p.Status=2 AND (@StoreId IS NULL OR p.StoreId=@StoreId) AND (@FromUtc IS NULL OR p.PaymentUtc>=@FromUtc) AND (@ToUtc IS NULL OR p.PaymentUtc<@ToUtc) AND (@AllowedStoreIdsCsv IS NULL OR p.StoreId IN(SELECT TRY_CONVERT(BIGINT,value) FROM STRING_SPLIT(@AllowedStoreIdsCsv,N',') WHERE TRY_CONVERT(BIGINT,value) IS NOT NULL)) GROUP BY PaymentMethod ORDER BY PaymentMethod;
END;
GO

-- Phase 8 stable permissions. Retail billing remains separate from Phase 9 platform billing.
DECLARE @Phase8Permissions TABLE(Name NVARCHAR(150));
INSERT @Phase8Permissions(Name) VALUES
(N'Products.View'),(N'Products.Create'),(N'Products.Edit'),(N'Products.ManageStores'),
(N'RetailInvoices.View'),(N'RetailInvoices.Create'),(N'RetailInvoices.Edit'),(N'RetailInvoices.Finalize'),(N'RetailInvoices.Cancel'),
(N'RetailPayments.View'),(N'RetailPayments.Create'),(N'RetailSpendAttribution.View'),(N'RetailSpendAttribution.Manage'),(N'RetailReports.View');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 2,p.Name,N'Allows '+p.Name+N' operations.',1,SYSUTCDATETIME() FROM @Phase8Permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO

-- Tenant owners/admins/shop owners get Phase 8 management. StoreManager gets store operations but not cancellation; SalesStaff gets checkout basics only.
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1
WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER')
  AND p.Name IN(N'Products.View',N'Products.Create',N'Products.Edit',N'Products.ManageStores',N'RetailInvoices.View',N'RetailInvoices.Create',N'RetailInvoices.Edit',N'RetailInvoices.Finalize',N'RetailInvoices.Cancel',N'RetailPayments.View',N'RetailPayments.Create',N'RetailSpendAttribution.View',N'RetailSpendAttribution.Manage',N'RetailReports.View')
  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1
WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName=N'STOREMANAGER'
  AND p.Name IN(N'Products.View',N'Products.Create',N'Products.Edit',N'Products.ManageStores',N'RetailInvoices.View',N'RetailInvoices.Create',N'RetailInvoices.Edit',N'RetailInvoices.Finalize',N'RetailPayments.View',N'RetailPayments.Create',N'RetailSpendAttribution.View',N'RetailSpendAttribution.Manage',N'RetailReports.View')
  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=2 AND p.IsActive=1
WHERE r.Scope=2 AND r.IsActive=1 AND r.NormalizedName IN(N'SALESSTAFF',N'CRMSTAFF')
  AND p.Name IN(N'Products.View',N'RetailInvoices.View',N'RetailInvoices.Create',N'RetailInvoices.Edit',N'RetailInvoices.Finalize',N'RetailPayments.View',N'RetailPayments.Create',N'RetailSpendAttribution.View')
  AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO

IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.7.0')
    INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.7.0',N'Phase 8 products, retail billing, participants, spend attribution and tenant retail reports',SYSUTCDATETIME(),SUSER_SNAME());
GO
