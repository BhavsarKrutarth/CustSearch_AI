USE [CustSearch_AI];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

/* Expected validation failures execute without an ambient transaction. */
BEGIN TRY
    EXEC dbo.ReportExportJob_Create @TenantId=NULL,@RequestedByUserId=1,@ReportType=N'Platform.TenantOperationalSummary',@FilterJson=N'not-json',@Format=1;
    THROW 55110,'Invalid JSON was accepted.',1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER()=55110 THROW;
END CATCH;

BEGIN TRY
    EXEC dbo.PlatformReport_Get @ReportType=N'Platform.NotAllowed',@Take=10;
    THROW 55111,'Unsupported report type was accepted.',1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER()=55111 THROW;
END CATCH;

BEGIN TRANSACTION;
DECLARE @Token NVARCHAR(32)=REPLACE(CONVERT(NVARCHAR(36),NEWID()),N'-',N'');
DECLARE @UserName NVARCHAR(100)=N'phase15-sql-'+@Token;
INSERT dbo.Users(TenantId,Scope,UserName,NormalizedUserName,Email,NormalizedEmail,DisplayName,PasswordHash,SecurityStamp,IsActive,CreatedUtc)
VALUES(NULL,1,@UserName,UPPER(@UserName),@UserName+N'@invalid.test',UPPER(@UserName+N'@invalid.test'),N'Phase 15 SQL Test',N'not-a-real-password-hash',CONVERT(NVARCHAR(36),NEWID()),1,SYSUTCDATETIME());
DECLARE @UserId BIGINT=SCOPE_IDENTITY();
INSERT dbo.UserRoles(UserId,RoleId) SELECT @UserId,Id FROM dbo.Roles WHERE Scope=1 AND TenantId IS NULL AND NormalizedName=N'PLATFORMSUPERADMIN' AND IsActive=1;
IF @@ROWCOUNT<>1 THROW 55118,'PlatformSuperAdmin seed role is unavailable.',1;
DECLARE @OtherUserName NVARCHAR(100)=N'phase15-other-'+@Token;
INSERT dbo.Users(TenantId,Scope,UserName,NormalizedUserName,Email,NormalizedEmail,DisplayName,PasswordHash,SecurityStamp,IsActive,CreatedUtc)
VALUES(NULL,1,@OtherUserName,UPPER(@OtherUserName),@OtherUserName+N'@invalid.test',UPPER(@OtherUserName+N'@invalid.test'),N'Phase 15 Other Test',N'not-a-real-password-hash',CONVERT(NVARCHAR(36),NEWID()),1,SYSUTCDATETIME());
DECLARE @OtherUserId BIGINT=SCOPE_IDENTITY();
INSERT dbo.UserRoles(UserId,RoleId) SELECT @OtherUserId,Id FROM dbo.Roles WHERE Scope=1 AND TenantId IS NULL AND NormalizedName=N'PLATFORMSUPERADMIN' AND IsActive=1;

/* Small factual tenant dataset validates aggregation and tenant/store pre-filtering. */
INSERT dbo.Tenants(TenantCode,LegalName,DisplayName,TimeZone,MaxStaff) VALUES(N'P15'+LEFT(@Token,12),N'Phase 15 Test Tenant',N'Phase 15 Test',N'Asia/Kolkata',5);
DECLARE @TenantId BIGINT=SCOPE_IDENTITY();
INSERT dbo.Users(TenantId,Scope,UserName,NormalizedUserName,Email,NormalizedEmail,DisplayName,PasswordHash,SecurityStamp,IsActive,CreatedUtc)
VALUES(@TenantId,2,N'phase15-tenant-'+@Token,UPPER(N'phase15-tenant-'+@Token),N'phase15-tenant-'+@Token+N'@invalid.test',UPPER(N'phase15-tenant-'+@Token+N'@invalid.test'),N'Phase 15 Tenant Test',N'not-a-real-password-hash',CONVERT(NVARCHAR(36),NEWID()),1,SYSUTCDATETIME());
DECLARE @TenantUserId BIGINT=SCOPE_IDENTITY();
INSERT dbo.Stores(TenantId,StoreCode,StoreName,AddressLine1,City,StateOrProvince,PostalCode,CountryCode,TimeZone) VALUES(@TenantId,N'MAIN',N'Main Test Store',N'Test Address',N'Ahmedabad',N'Gujarat',N'380001','IN',N'Asia/Kolkata');
DECLARE @StoreId BIGINT=SCOPE_IDENTITY();
INSERT dbo.Customers(TenantId,CustomerCode,FirstName) VALUES(@TenantId,N'CUST-15',N'Asha');DECLARE @CustomerId BIGINT=SCOPE_IDENTITY();
INSERT dbo.CustomerStoreAssignments(TenantId,CustomerId,StoreId,IsPrimary,AssignedUtc,AssignedByUserId) VALUES(@TenantId,@CustomerId,@StoreId,1,'2026-08-19T00:00:00Z',@TenantUserId);
INSERT dbo.CustomerVisits(TenantId,StoreId,CustomerId,VisitCode,EnteredUtc,ExitedUtc,Source) VALUES
(@TenantId,@StoreId,@CustomerId,N'VISIT-1','2026-08-20T10:00:00Z','2026-08-20T11:00:00Z',1),
(@TenantId,@StoreId,@CustomerId,N'VISIT-2','2026-08-21T10:00:00Z','2026-08-21T11:00:00Z',1);
INSERT dbo.RetailInvoices(TenantId,StoreId,InvoiceNumber,CustomerId,InvoiceUtc,Subtotal,DiscountAmount,TaxAmount,GrandTotal,PaidAmount,BalanceAmount,Status,CreatedByUserId)
VALUES(@TenantId,@StoreId,N'INV-15',@CustomerId,'2026-08-21T12:00:00Z',100,0,23,123,100,23,2,@TenantUserId);DECLARE @InvoiceId BIGINT=SCOPE_IDENTITY();
INSERT dbo.RetailInvoiceItems(TenantId,InvoiceId,ProductCodeSnapshot,ProductNameSnapshot,CategoryNameSnapshot,Quantity,UnitPrice,DiscountAmount,TaxPercent,TaxAmount,LineSubtotal,LineTotal)
VALUES(@TenantId,@InvoiceId,N'SKU-15',N'Test Product',N'Test Category',1,100,0,23,23,100,123);
INSERT dbo.RetailInvoicePayments(TenantId,StoreId,InvoiceId,PaymentReference,PaymentMethod,Amount,PaymentUtc,Status,ReceivedByUserId)
VALUES(@TenantId,@StoreId,@InvoiceId,N'PAY-15',1,100,'2026-08-21T12:05:00Z',2,@TenantUserId);

DECLARE @Returning TABLE(CustomerId BIGINT,CustomerCode NVARCHAR(50),FirstName NVARCHAR(100),LastName NVARCHAR(100),VisitCount BIGINT,FirstVisitUtc DATETIME2(7),LatestVisitUtc DATETIME2(7));
INSERT @Returning EXEC dbo.TenantReport_Get @TenantId=@TenantId,@AllowedStoreIdsCsv=NULL,@ReportType=N'Tenant.ReturningCustomers',@StoreId=@StoreId,@Take=10;
IF NOT EXISTS(SELECT 1 FROM @Returning WHERE CustomerId=@CustomerId AND VisitCount=2) THROW 55122,'Returning customer report is inaccurate.',1;
DECLARE @Sales TABLE(SalesDate DATE,StoreId BIGINT,StoreName NVARCHAR(150),InvoiceCount BIGINT,NetSales DECIMAL(19,4),PaidAmount DECIMAL(19,4),OutstandingAmount DECIMAL(19,4));
INSERT @Sales EXEC dbo.TenantReport_Get @TenantId=@TenantId,@AllowedStoreIdsCsv=NULL,@ReportType=N'Tenant.RetailSales',@StoreId=@StoreId,@Take=10;
IF NOT EXISTS(SELECT 1 FROM @Sales WHERE StoreId=@StoreId AND InvoiceCount=1 AND NetSales=123 AND PaidAmount=100 AND OutstandingAmount=23) THROW 55123,'Retail sales report is inaccurate.',1;
DECLARE @Products TABLE(ProductId BIGINT,ProductCode NVARCHAR(100),ProductName NVARCHAR(200),Quantity DECIMAL(18,4),SalesTotal DECIMAL(19,4));
INSERT @Products EXEC dbo.TenantReport_Get @TenantId=@TenantId,@AllowedStoreIdsCsv=NULL,@ReportType=N'Tenant.ProductSales',@StoreId=@StoreId,@Take=10;
IF NOT EXISTS(SELECT 1 FROM @Products WHERE ProductCode=N'SKU-15' AND Quantity=1 AND SalesTotal=123) THROW 55124,'Product sales report is inaccurate.',1;

EXEC dbo.ReportAudit_Write @TenantId=NULL,@StoreId=NULL,@ActorUserId=@UserId,@Action=N'PlatformReportPreviewed',@EntityType=N'ReportExport',@EntityId=NULL,@AfterJson=N'{"reportType":"Platform.TenantOperationalSummary"}',@IpAddress='127.0.0.1',@UserAgent=N'Phase15SqlTest',@CorrelationId='phase15-sql-test';
IF NOT EXISTS(SELECT 1 FROM dbo.AuditLogs WHERE UserId=@UserId AND Action=N'PlatformReportPreviewed' AND CorrelationId='phase15-sql-test') THROW 55112,'Report access audit was not persisted.',1;

DECLARE @Created TABLE(Id BIGINT,TenantId BIGINT,RequestedByUserId BIGINT,ReportType NVARCHAR(100),FilterJson NVARCHAR(4000),Format TINYINT,Status TINYINT,ProgressPercent TINYINT,StorageReference NVARCHAR(260),DownloadFileName NVARCHAR(260),ContentType NVARCHAR(150),ContentLength BIGINT,Sha256 CHAR(64),ErrorMessage NVARCHAR(1000),RequestedUtc DATETIME2(7),StartedUtc DATETIME2(7),HeartbeatUtc DATETIME2(7),CompletedUtc DATETIME2(7),ExpiresUtc DATETIME2(7),AttemptCount INT,RowVersion BINARY(8));
INSERT @Created EXEC dbo.ReportExportJob_Create @TenantId=NULL,@RequestedByUserId=@UserId,@ReportType=N'Platform.TenantOperationalSummary',@FilterJson=N'{}',@Format=1,@IpAddress='127.0.0.1',@UserAgent=N'Phase15SqlTest',@CorrelationId='phase15-sql-queue';
DECLARE @JobId BIGINT=(SELECT Id FROM @Created);
IF @JobId IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.ReportExportEvents WHERE ReportExportJobId=@JobId AND EventType=N'ReportExportQueued') THROW 55113,'Queued job/event was not persisted.',1;
DECLARE @Unauthorized TABLE(Id BIGINT,TenantId BIGINT,RequestedByUserId BIGINT,ReportType NVARCHAR(100),FilterJson NVARCHAR(4000),Format TINYINT,Status TINYINT,ProgressPercent TINYINT,StorageReference NVARCHAR(260),DownloadFileName NVARCHAR(260),ContentType NVARCHAR(150),ContentLength BIGINT,Sha256 CHAR(64),ErrorMessage NVARCHAR(1000),RequestedUtc DATETIME2(7),StartedUtc DATETIME2(7),HeartbeatUtc DATETIME2(7),CompletedUtc DATETIME2(7),ExpiresUtc DATETIME2(7),AttemptCount INT,RowVersion BINARY(8));
INSERT @Unauthorized EXEC dbo.ReportExportJob_Get @JobId=@JobId,@RequestedByUserId=@OtherUserId,@TenantId=NULL,@IsPlatform=1;
IF EXISTS(SELECT 1 FROM @Unauthorized) THROW 55121,'A different requester could read the export job.',1;

DECLARE @Claimed TABLE(Id BIGINT,TenantId BIGINT,RequestedByUserId BIGINT,ReportType NVARCHAR(100),FilterJson NVARCHAR(4000),Format TINYINT,AttemptCount INT,LeaseToken UNIQUEIDENTIFIER,RequestedUtc DATETIME2(7));
INSERT @Claimed EXEC dbo.ReportExportJob_Claim @LeaseSeconds=300;
DECLARE @Lease UNIQUEIDENTIFIER=(SELECT LeaseToken FROM @Claimed WHERE Id=@JobId);
IF @Lease IS NULL THROW 55114,'Queued job was not claimed.',1;
EXEC dbo.ReportExportJob_Progress @JobId=@JobId,@LeaseToken=@Lease,@ProgressPercent=60;
EXEC dbo.ReportExportJob_Complete @JobId=@JobId,@LeaseToken=@Lease,@StorageReference=N'test-safe.csv',@DownloadFileName=N'report.csv',@ContentType=N'text/csv',@ContentLength=5,@Sha256='f01a374e9c81e3db89b3a42940c4d6a5447684986a1296e42bf13f196eed6295',@RetentionHours=24;
IF NOT EXISTS(SELECT 1 FROM dbo.ReportExportJobs WHERE Id=@JobId AND Status=3 AND ProgressPercent=100) THROW 55115,'Job lifecycle did not complete.',1;
IF (SELECT COUNT(*) FROM dbo.ReportExportEvents WHERE ReportExportJobId=@JobId AND EventType IN(N'ReportExportQueued',N'ReportExportProgress',N'ReportExportReady'))<>3 THROW 55116,'Expected lifecycle events are missing.',1;

DECLARE @DueEvents TABLE(Id BIGINT,ReportExportJobId BIGINT,TenantId BIGINT,RequestedByUserId BIGINT,EventType NVARCHAR(100),JobStatus TINYINT,ProgressPercent TINYINT,CreatedUtc DATETIME2(7),LeaseToken UNIQUEIDENTIFIER);
INSERT @DueEvents EXEC dbo.ReportExportEvent_Claim @LeaseSeconds=60,@Take=50;
IF (SELECT COUNT(*) FROM @DueEvents WHERE ReportExportJobId=@JobId)<>3 THROW 55119,'Durable export events were not claimable.',1;
DECLARE @EventId BIGINT,@EventLease UNIQUEIDENTIFIER;
WHILE EXISTS(SELECT 1 FROM @DueEvents WHERE ReportExportJobId=@JobId)
BEGIN
    SELECT TOP(1) @EventId=Id,@EventLease=LeaseToken FROM @DueEvents WHERE ReportExportJobId=@JobId ORDER BY Id;
    EXEC dbo.ReportExportEvent_Complete @EventId=@EventId,@LeaseToken=@EventLease;
    DELETE FROM @DueEvents WHERE Id=@EventId;
END;
IF (SELECT COUNT(*) FROM dbo.ReportExportEvents WHERE ReportExportJobId=@JobId AND DeliveredUtc IS NOT NULL)<>3 THROW 55120,'Durable export events were not completed.',1;

ROLLBACK TRANSACTION;
IF EXISTS(SELECT 1 FROM dbo.Users WHERE Id IN(@UserId,@OtherUserId)) OR EXISTS(SELECT 1 FROM dbo.ReportExportJobs WHERE Id=@JobId) THROW 55117,'Rollback-only test data leaked.',1;
SELECT N'Phase 15 rollback-only database tests passed.' AS Result;
GO
