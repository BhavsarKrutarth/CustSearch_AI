/* Standalone repeat-safe Phase 15 runner. Requires the fully installed V1.13.0 baseline. */
/* CustSearch AI Phase 15 — server-scoped reports and asynchronous export jobs. */
USE [CustSearch_AI];
GO
SET NOCOUNT ON;SET XACT_ABORT ON;SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;SET CONCAT_NULL_YIELDS_NULL ON;SET ARITHABORT ON;SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'dbo.DatabaseVersions',N'U') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.13.0') THROW 54900,'Phase 14 V1.13.0 must be installed before Phase 15.',1;
GO
IF OBJECT_ID(N'dbo.ExportJobs',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.ExportJobs(
  Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExportJobs PRIMARY KEY,
  RequestedByUserId BIGINT NOT NULL,TenantId BIGINT NULL,AlertId BIGINT NULL,ReportType TINYINT NOT NULL,Format TINYINT NOT NULL,
  FilterJson NVARCHAR(4000) NOT NULL,AuthorizedStoreIdsJson NVARCHAR(4000) NOT NULL,Status TINYINT NOT NULL,Progress TINYINT NOT NULL,
  CreatedUtc DATETIME2(7) NOT NULL,StartedUtc DATETIME2(7) NULL,CompletedUtc DATETIME2(7) NULL,ExpiresUtc DATETIME2(7) NOT NULL,
  Error NVARCHAR(2000) NULL,FilePath NVARCHAR(1000) NULL,FileName NVARCHAR(260) NULL,ContentType NVARCHAR(150) NULL,
  AttemptCount INT NOT NULL,LeaseId UNIQUEIDENTIFIER NULL,LeaseExpiresUtc DATETIME2(7) NULL,RowVersion ROWVERSION NOT NULL,
  CONSTRAINT FK_ExportJobs_Users FOREIGN KEY(RequestedByUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT FK_ExportJobs_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
  CONSTRAINT FK_ExportJobs_Alerts FOREIGN KEY(AlertId) REFERENCES dbo.Alerts(Id),
  CONSTRAINT CK_ExportJobs_Scope CHECK((TenantId IS NULL AND ReportType=20) OR (TenantId IS NOT NULL AND ReportType BETWEEN 1 AND 10)),
  CONSTRAINT CK_ExportJobs_Format CHECK(Format BETWEEN 1 AND 3),CONSTRAINT CK_ExportJobs_Status CHECK(Status BETWEEN 1 AND 5),
  CONSTRAINT CK_ExportJobs_Progress CHECK(Progress BETWEEN 0 AND 100),CONSTRAINT CK_ExportJobs_Attempts CHECK(AttemptCount>=0),
  CONSTRAINT CK_ExportJobs_Json CHECK(ISJSON(FilterJson)=1 AND ISJSON(AuthorizedStoreIdsJson)=1),
  CONSTRAINT CK_ExportJobs_Retention CHECK(ExpiresUtc>CreatedUtc),
  CONSTRAINT CK_ExportJobs_File CHECK((Status=3 AND FilePath IS NOT NULL AND FileName IS NOT NULL AND ContentType IS NOT NULL AND Progress=100) OR Status<>3)
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ExportJobs') AND name=N'IX_ExportJobs_Status_Created') CREATE INDEX IX_ExportJobs_Status_Created ON dbo.ExportJobs(Status,CreatedUtc,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ExportJobs') AND name=N'IX_ExportJobs_Tenant_User_Created') CREATE INDEX IX_ExportJobs_Tenant_User_Created ON dbo.ExportJobs(TenantId,RequestedByUserId,CreatedUtc DESC,Id DESC);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ExportJobs') AND name=N'UX_ExportJobs_Alert') CREATE UNIQUE INDEX UX_ExportJobs_Alert ON dbo.ExportJobs(AlertId) WHERE AlertId IS NOT NULL;
GO

CREATE OR ALTER PROCEDURE dbo.Report_TenantOperationalSummary
 @TenantId BIGINT,@ReportType TINYINT,@StoreIdsJson NVARCHAR(4000),@FromUtc DATETIME2(7),@ToUtc DATETIME2(7),@PageNumber INT=1,@PageSize INT=100
AS
BEGIN
 SET NOCOUNT ON;
 IF @TenantId<=0 OR @ReportType NOT BETWEEN 1 AND 10 OR @FromUtc>=@ToUtc THROW 54910,'Invalid tenant report filter.',1;
 IF @PageNumber<1 OR @PageSize<1 OR @PageSize>500 THROW 54911,'Invalid tenant report paging.',1;
 IF ISJSON(@StoreIdsJson)<>1 THROW 54912,'Authorized store scope must be JSON.',1;
 DECLARE @Stores TABLE(StoreId BIGINT PRIMARY KEY);
 INSERT @Stores(StoreId) SELECT DISTINCT TRY_CONVERT(BIGINT,[value]) FROM OPENJSON(@StoreIdsJson) WHERE TRY_CONVERT(BIGINT,[value])>0;
 IF EXISTS(SELECT 1 FROM @Stores a LEFT JOIN dbo.Stores s ON s.Id=a.StoreId AND s.TenantId=@TenantId AND s.IsActive=1 WHERE s.Id IS NULL) THROW 54913,'Authorized store scope is invalid.',1;
 DECLARE @Rows TABLE(Domain NVARCHAR(80),StoreId BIGINT NULL,Metric NVARCHAR(120),Value DECIMAL(19,4),Label NVARCHAR(250) NULL,OccurredUtc DATETIME2(7) NULL);
 IF @ReportType IN(1,2) INSERT @Rows SELECT N'Customers',a.StoreId,N'Active customers',COUNT_BIG(DISTINCT a.CustomerId),NULL,NULL FROM dbo.CustomerStoreAssignments a JOIN @Stores s ON s.StoreId=a.StoreId JOIN dbo.Customers c ON c.Id=a.CustomerId AND c.TenantId=@TenantId AND c.IsActive=1 WHERE a.TenantId=@TenantId GROUP BY a.StoreId;
 IF @ReportType IN(1,3) INSERT @Rows SELECT N'Households',NULL,N'Households',COUNT_BIG(*),N'Tenant-wide factual households',NULL FROM dbo.Households h WHERE h.TenantId=@TenantId AND h.CreatedUtc>=@FromUtc AND h.CreatedUtc<@ToUtc;
 IF @ReportType IN(1,4) INSERT @Rows SELECT N'Visits',v.StoreId,N'Customer visits',COUNT_BIG(*),NULL,MAX(v.EnteredUtc) FROM dbo.CustomerVisits v JOIN @Stores s ON s.StoreId=v.StoreId WHERE v.TenantId=@TenantId AND v.EnteredUtc>=@FromUtc AND v.EnteredUtc<@ToUtc GROUP BY v.StoreId;
 IF @ReportType IN(1,5) INSERT @Rows SELECT N'Retail billing',i.StoreId,N'Invoice total',SUM(i.GrandTotal),CONCAT(COUNT_BIG(*),N' invoices'),MAX(i.InvoiceUtc) FROM dbo.RetailInvoices i JOIN @Stores s ON s.StoreId=i.StoreId WHERE i.TenantId=@TenantId AND i.InvoiceUtc>=@FromUtc AND i.InvoiceUtc<@ToUtc GROUP BY i.StoreId;
 IF @ReportType IN(1,6) INSERT @Rows SELECT N'Preferences',p.StoreId,N'Preference signals',COUNT_BIG(*),N'Factual and derived signals remain distinguishable',MAX(p.LastObservedUtc) FROM dbo.CustomerPreferenceSignals p LEFT JOIN @Stores s ON s.StoreId=p.StoreId WHERE p.TenantId=@TenantId AND (p.StoreId IS NULL OR s.StoreId IS NOT NULL) AND p.LastObservedUtc>=@FromUtc AND p.LastObservedUtc<@ToUtc GROUP BY p.StoreId;
 IF @ReportType IN(1,7) INSERT @Rows SELECT N'Alerts',a.StoreId,N'Alerts',COUNT_BIG(*),CONCAT(N'Open: ',SUM(CASE WHEN a.Status IN(1,2,3) THEN 1 ELSE 0 END)),MAX(a.CreatedUtc) FROM dbo.Alerts a LEFT JOIN @Stores s ON s.StoreId=a.StoreId WHERE a.TenantId=@TenantId AND (a.StoreId IS NULL OR s.StoreId IS NOT NULL) AND a.CreatedUtc>=@FromUtc AND a.CreatedUtc<@ToUtc GROUP BY a.StoreId;
 IF @ReportType IN(1,8) INSERT @Rows SELECT N'Integrations',NULL,N'Delivery attempts',COUNT_BIG(*),CONCAT(N'Failures: ',SUM(CASE WHEN l.Status=2 THEN 1 ELSE 0 END)),MAX(l.CreatedUtc) FROM dbo.IntegrationDeliveryLogs l WHERE l.TenantId=@TenantId AND l.CreatedUtc>=@FromUtc AND l.CreatedUtc<@ToUtc;
 IF @ReportType IN(1,9) INSERT @Rows SELECT N'Cameras',c.StoreId,N'Active cameras',COUNT_BIG(*),CONCAT(N'Online: ',SUM(CASE WHEN c.Status=2 THEN 1 ELSE 0 END)),MAX(c.LastHeartbeatUtc) FROM dbo.Cameras c JOIN @Stores s ON s.StoreId=c.StoreId WHERE c.TenantId=@TenantId AND c.IsActive=1 GROUP BY c.StoreId;
 IF @ReportType IN(1,10) INSERT @Rows SELECT N'Staff operations',p.StoreId,N'Presence sessions',COUNT_BIG(*),N'Operational only; not payroll or discipline truth',MAX(p.EnteredUtc) FROM dbo.StaffPresenceSessions p JOIN @Stores s ON s.StoreId=p.StoreId WHERE p.TenantId=@TenantId AND p.EnteredUtc>=@FromUtc AND p.EnteredUtc<@ToUtc GROUP BY p.StoreId;
 ;WITH Ordered AS(SELECT COUNT_BIG(*) OVER()TotalRows,Domain,StoreId,Metric,Value,Label,OccurredUtc,ROW_NUMBER()OVER(ORDER BY Domain,StoreId,Metric)rn FROM @Rows)
 SELECT TotalRows,Domain,StoreId,Metric,Value,Label,OccurredUtc FROM Ordered WHERE rn BETWEEN ((@PageNumber-1)*@PageSize)+1 AND @PageNumber*@PageSize ORDER BY rn;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Report_PlatformTenantSummary @FromUtc DATETIME2(7),@ToUtc DATETIME2(7),@PageNumber INT=1,@PageSize INT=100
AS
BEGIN
 SET NOCOUNT ON;
 IF @FromUtc>=@ToUtc OR @PageNumber<1 OR @PageSize<1 OR @PageSize>500 THROW 54920,'Invalid platform report filter.',1;
 ;WITH SourceRows AS(
  SELECT N'Platform tenants' Domain,CAST(NULL AS BIGINT)StoreId,N'Tenant' Metric,CAST(t.Id AS DECIMAL(19,4))Value,CONCAT(t.TenantCode,N' · ',t.DisplayName,N' · active=',t.IsActive,N' · suspended=',t.IsSuspended)Label,t.CreatedUtc OccurredUtc
  FROM dbo.Tenants t WHERE t.CreatedUtc>=@FromUtc AND t.CreatedUtc<@ToUtc
 ),Ordered AS(SELECT COUNT_BIG(*)OVER()TotalRows,*,ROW_NUMBER()OVER(ORDER BY OccurredUtc,Value)rn FROM SourceRows)
 SELECT TotalRows,Domain,StoreId,Metric,Value,Label,OccurredUtc FROM Ordered WHERE rn BETWEEN ((@PageNumber-1)*@PageSize)+1 AND @PageNumber*@PageSize ORDER BY rn;
END;
GO

DECLARE @Permissions TABLE(Scope TINYINT,Name NVARCHAR(150),Description NVARCHAR(300));
INSERT @Permissions VALUES(1,N'PlatformReports.View',N'View platform operational reports.'),(1,N'PlatformReports.Export',N'Create and download platform exports.'),(2,N'Reports.View',N'View tenant/store-scoped reports.'),(2,N'Reports.Export',N'Create and download tenant/store-scoped exports.');
INSERT dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc) SELECT p.Scope,p.Name,p.Description,1,SYSUTCDATETIME() FROM @Permissions p WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Name=p.Name);
GO
INSERT dbo.RolePermissions(RoleId,PermissionId) SELECT r.Id,p.Id FROM dbo.Roles r JOIN dbo.Permissions p ON p.Scope=r.Scope AND p.IsActive=1 WHERE r.IsActive=1 AND ((r.Scope=1 AND r.NormalizedName IN(N'PLATFORMSUPERADMIN',N'PLATFORMOPERATIONSADMIN',N'PLATFORMAUDITOR') AND p.Name IN(N'PlatformReports.View',N'PlatformReports.Export')) OR (r.Scope=2 AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER',N'STOREADMIN',N'STOREMANAGER') AND p.Name IN(N'Reports.View',N'Reports.Export'))) AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0') INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy) VALUES(N'V1.14.0',N'Phase 15 server-scoped report catalog and authorized asynchronous CSV Excel PDF exports',SYSUTCDATETIME(),SUSER_SNAME());
GO
IF(SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.14.0')<>1 THROW 54990,'V1.14.0 DatabaseVersions row must exist exactly once.',1;
IF OBJECT_ID(N'dbo.ExportJobs',N'U') IS NULL OR OBJECT_ID(N'dbo.Report_TenantOperationalSummary',N'P') IS NULL OR OBJECT_ID(N'dbo.Report_PlatformTenantSummary',N'P') IS NULL THROW 54991,'Phase 15 database objects are incomplete.',1;
GO
