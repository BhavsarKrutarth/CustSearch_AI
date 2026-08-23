/*
 CustSearch AI — Phase 5 production database upgrade
 Version: V1.4.0
 Rules: idempotent, no EF migrations, SQL Server 2022, UTC timestamps.
 Every object below is tagged with its Phase 5 sub-phase.
*/
USE [CustSearch_AI];
GO
SET XACT_ABORT ON;
GO

/* Phase 5C — Store Master & Canonical Location: tenant-owned physical stores. */
IF OBJECT_ID(N'dbo.Stores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Stores
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Stores PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreCode NVARCHAR(30) NOT NULL,
        StoreName NVARCHAR(150) NOT NULL,
        AddressLine1 NVARCHAR(250) NOT NULL,
        AddressLine2 NVARCHAR(250) NULL,
        Landmark NVARCHAR(150) NULL,
        City NVARCHAR(100) NOT NULL,
        District NVARCHAR(100) NULL,
        StateOrProvince NVARCHAR(100) NOT NULL,
        PostalCode NVARCHAR(20) NOT NULL,
        CountryCode CHAR(2) NOT NULL,
        Latitude DECIMAL(9,6) NULL,
        Longitude DECIMAL(9,6) NULL,
        GeoFenceRadiusMeters DECIMAL(10,2) NULL,
        ExternalPlaceId NVARCHAR(200) NULL,
        LocationSource TINYINT NOT NULL CONSTRAINT DF_Stores_LocationSource DEFAULT(1),
        IsLocationVerified BIT NOT NULL CONSTRAINT DF_Stores_IsLocationVerified DEFAULT(0),
        LocationVerifiedUtc DATETIME2(7) NULL,
        LocationVerifiedByUserId BIGINT NULL,
        TimeZone NVARCHAR(100) NOT NULL,
        ContactEmail NVARCHAR(254) NULL,
        ContactMobile NVARCHAR(30) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Stores_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Stores_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_Stores_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Stores_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_Stores_Users_LocationVerifiedByUserId FOREIGN KEY(LocationVerifiedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Stores_Latitude CHECK (Latitude IS NULL OR Latitude BETWEEN -90 AND 90),
        CONSTRAINT CK_Stores_Longitude CHECK (Longitude IS NULL OR Longitude BETWEEN -180 AND 180),
        CONSTRAINT CK_Stores_CoordinatesPair CHECK ((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL)),
        CONSTRAINT CK_Stores_GeoFence CHECK (GeoFenceRadiusMeters IS NULL OR GeoFenceRadiusMeters > 0)
    );
END;
GO
/* Phase 5C — Store uniqueness/query indexes. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'UX_Stores_Tenant_StoreCode')
    CREATE UNIQUE INDEX UX_Stores_Tenant_StoreCode ON dbo.Stores(TenantId, StoreCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'IX_Stores_Tenant_Active')
    CREATE INDEX IX_Stores_Tenant_Active ON dbo.Stores(TenantId, IsActive);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'IX_Stores_Tenant_City')
    CREATE INDEX IX_Stores_Tenant_City ON dbo.Stores(TenantId, City);
GO

/* Phase 5B — Store Assignment & Quotas: authoritative user-to-store authorization relation. */
IF OBJECT_ID(N'dbo.UserStoreAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserStoreAssignments
    (
        TenantId BIGINT NOT NULL,
        UserId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        IsPrimary BIT NOT NULL CONSTRAINT DF_UserStoreAssignments_IsPrimary DEFAULT(0),
        AssignedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_UserStoreAssignments_AssignedUtc DEFAULT(SYSUTCDATETIME()),
        AssignedByUserId BIGINT NOT NULL,
        CONSTRAINT PK_UserStoreAssignments PRIMARY KEY(UserId, StoreId),
        CONSTRAINT FK_UserStoreAssignments_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_UserStoreAssignments_Users_UserId FOREIGN KEY(UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserStoreAssignments_Stores_StoreId FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserStoreAssignments_Users_AssignedBy FOREIGN KEY(AssignedByUserId) REFERENCES dbo.Users(Id)
    );
END;
GO
/* Phase 5B — Store assignment access indexes. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.UserStoreAssignments') AND name=N'IX_UserStoreAssignments_Tenant_Store')
    CREATE INDEX IX_UserStoreAssignments_Tenant_Store ON dbo.UserStoreAssignments(TenantId, StoreId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.UserStoreAssignments') AND name=N'IX_UserStoreAssignments_User_Primary')
    CREATE INDEX IX_UserStoreAssignments_User_Primary ON dbo.UserStoreAssignments(UserId, IsPrimary);
GO

/* Phase 5D — Staff Profile: employee metadata linked one-to-one with a tenant user. */
IF OBJECT_ID(N'dbo.StaffProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffProfiles
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffProfiles PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        UserId BIGINT NOT NULL,
        EmployeeCode NVARCHAR(50) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Mobile NVARCHAR(30) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_StaffProfiles_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StaffProfiles_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StaffProfiles_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_StaffProfiles_Tenants_TenantId FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_StaffProfiles_Users_UserId FOREIGN KEY(UserId) REFERENCES dbo.Users(Id)
    );
END;
GO
/* Phase 5D — Staff uniqueness indexes. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffProfiles') AND name=N'UX_StaffProfiles_UserId')
    CREATE UNIQUE INDEX UX_StaffProfiles_UserId ON dbo.StaffProfiles(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffProfiles') AND name=N'UX_StaffProfiles_Tenant_EmployeeCode')
    CREATE UNIQUE INDEX UX_StaffProfiles_Tenant_EmployeeCode ON dbo.StaffProfiles(TenantId, EmployeeCode);
GO

/* Phase 5D — Staff Shifts: operational scheduling context; not CCTV-derived payroll truth. */
IF OBJECT_ID(N'dbo.StaffShifts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffShifts
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffShifts PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StaffProfileId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        StartsUtc DATETIME2(7) NOT NULL,
        ScheduledEndsUtc DATETIME2(7) NULL,
        ActualEndsUtc DATETIME2(7) NULL,
        Status TINYINT NOT NULL,
        CreatedByUserId BIGINT NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StaffShifts_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StaffShifts_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_StaffShifts_StaffProfiles FOREIGN KEY(StaffProfileId) REFERENCES dbo.StaffProfiles(Id),
        CONSTRAINT FK_StaffShifts_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
        CONSTRAINT FK_StaffShifts_Users_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_StaffShifts_Period CHECK (ScheduledEndsUtc IS NULL OR ScheduledEndsUtc > StartsUtc),
        CONSTRAINT CK_StaffShifts_Status CHECK (Status BETWEEN 1 AND 4)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffShifts') AND name=N'IX_StaffShifts_Tenant_Store_Start')
    CREATE INDEX IX_StaffShifts_Tenant_Store_Start ON dbo.StaffShifts(TenantId, StoreId, StartsUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffShifts') AND name=N'IX_StaffShifts_Staff_Status')
    CREATE INDEX IX_StaffShifts_Staff_Status ON dbo.StaffShifts(StaffProfileId, Status);
GO

/* Phase 5D — Staff Presence: optional presence signals used for operations, not authoritative attendance/payroll. */
IF OBJECT_ID(N'dbo.StaffPresenceSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffPresenceSessions
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffPresenceSessions PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StaffProfileId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        Source TINYINT NOT NULL,
        EnteredUtc DATETIME2(7) NOT NULL,
        ExitedUtc DATETIME2(7) NULL,
        Confidence DECIMAL(5,4) NOT NULL,
        CONSTRAINT FK_StaffPresence_StaffProfiles FOREIGN KEY(StaffProfileId) REFERENCES dbo.StaffProfiles(Id),
        CONSTRAINT FK_StaffPresence_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
        CONSTRAINT CK_StaffPresence_Confidence CHECK (Confidence BETWEEN 0 AND 1),
        CONSTRAINT CK_StaffPresence_Period CHECK (ExitedUtc IS NULL OR ExitedUtc > EnteredUtc)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffPresenceSessions') AND name=N'IX_StaffPresence_Tenant_Store_Entered')
    CREATE INDEX IX_StaffPresence_Tenant_Store_Entered ON dbo.StaffPresenceSessions(TenantId, StoreId, EnteredUtc DESC);
GO

/* Phase 5E — Store Category Taxonomy: category master available before Phase 8 product master. */
IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductCategories
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductCategories PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NULL,
        CategoryCode NVARCHAR(50) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        ParentCategoryId BIGINT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ProductCategories_IsActive DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ProductCategories_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ProductCategories_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_ProductCategories_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_ProductCategories_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id),
        CONSTRAINT FK_ProductCategories_Parent FOREIGN KEY(ParentCategoryId) REFERENCES dbo.ProductCategories(Id)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategories') AND name=N'UX_ProductCategories_Tenant_Store_Code')
    CREATE UNIQUE INDEX UX_ProductCategories_Tenant_Store_Code ON dbo.ProductCategories(TenantId, StoreId, CategoryCode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategories') AND name=N'IX_ProductCategories_Tenant_Active')
    CREATE INDEX IX_ProductCategories_Tenant_Active ON dbo.ProductCategories(TenantId, IsActive);
GO

/* Phase 5F — Dynamic Store Voice Configuration: store-specific trigger; “Aasha Add” is default only. */
IF OBJECT_ID(N'dbo.StoreVoiceCommandSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StoreVoiceCommandSettings
    (
        StoreId BIGINT NOT NULL CONSTRAINT PK_StoreVoiceCommandSettings PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        TriggerKeyword NVARCHAR(100) NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_Trigger DEFAULT(N'Aasha Add'),
        ResponseMode TINYINT NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_Response DEFAULT(4),
        IsEnabled BIT NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_Enabled DEFAULT(1),
        RequireConfirmationForAmbiguousCategory BIT NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_Confirm DEFAULT(1),
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceCommandSettings_UpdatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_StoreVoiceCommandSettings_Stores FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id) ON DELETE CASCADE,
        CONSTRAINT FK_StoreVoiceCommandSettings_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT CK_StoreVoiceCommandSettings_Response CHECK (ResponseMode BETWEEN 1 AND 4)
    );
END;
GO
/* Phase 5F — Voice aliases for alternative store-specific trigger phrases. */
IF OBJECT_ID(N'dbo.StoreVoiceCommandAliases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StoreVoiceCommandAliases
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StoreVoiceCommandAliases PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        StoreId BIGINT NOT NULL,
        Alias NVARCHAR(100) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_StoreVoiceCommandAliases_CreatedUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_StoreVoiceCommandAliases_Settings FOREIGN KEY(StoreId) REFERENCES dbo.StoreVoiceCommandSettings(StoreId) ON DELETE CASCADE,
        CONSTRAINT FK_StoreVoiceCommandAliases_Tenants FOREIGN KEY(TenantId) REFERENCES dbo.Tenants(Id)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StoreVoiceCommandAliases') AND name=N'UX_StoreVoiceCommandAliases_Tenant_Store_Alias')
    CREATE UNIQUE INDEX UX_StoreVoiceCommandAliases_Tenant_Store_Alias ON dbo.StoreVoiceCommandAliases(TenantId, StoreId, Alias);
GO

/* Phase 5C — AuditLogs StoreId FK becomes enforceable after dbo.Stores exists. */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AuditLogs_Stores_StoreId')
    ALTER TABLE dbo.AuditLogs WITH CHECK ADD CONSTRAINT FK_AuditLogs_Stores_StoreId FOREIGN KEY(StoreId) REFERENCES dbo.Stores(Id);
GO

/* Phase 5A/5D/5E/5F — ensure required permission catalog entries exist without duplicate rows. */
DECLARE @Phase5Permissions TABLE(Name NVARCHAR(150));
INSERT INTO @Phase5Permissions(Name) VALUES
(N'TenantUsers.View'),(N'TenantUsers.Create'),(N'TenantUsers.Edit'),(N'TenantUsers.Deactivate'),(N'TenantUsers.AssignRoles'),
(N'TenantStores.View'),(N'TenantStores.Create'),(N'TenantStores.Edit'),
(N'Staff.View'),(N'Staff.Manage'),(N'StaffTracking.View'),(N'StaffPerformance.View'),(N'StaffPerformance.Export'),(N'StaffCustomerInteractions.View'),
(N'StoreCategories.View'),(N'StoreCategories.Manage'),
(N'VoiceCommands.Use'),(N'VoiceCommands.View'),(N'VoiceCommands.Configure'),(N'VoiceCommands.Audit');
INSERT INTO dbo.Permissions(Scope,Name,Description,IsActive,CreatedUtc)
SELECT 2,p.Name,N'Allows '+p.Name+N' operations.',1,SYSUTCDATETIME()
FROM @Phase5Permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.Permissions x WHERE x.Scope=2 AND x.Name=p.Name);
GO

/* Phase 5A — default tenant roles including TenantOwner/ShopOwner/StoreManager/SalesStaff. */
CREATE OR ALTER PROCEDURE dbo.Tenant_ProvisionDefaultRoles
    @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Tenants WHERE Id=@TenantId) THROW 51020,'Tenant does not exist.',1;
    DECLARE @Roles TABLE(Name NVARCHAR(100), Description NVARCHAR(300));
    INSERT INTO @Roles VALUES
    (N'TenantAdmin',N'Full tenant administration.'),(N'TenantOwner',N'Business owner with full tenant operations.'),(N'ShopOwner',N'Shop owner with full tenant operations.'),
    (N'StoreAdmin',N'Assigned-store administration.'),(N'StoreManager',N'Assigned-store staff and customer operations.'),(N'Manager',N'Day-to-day operational management.'),
    (N'SalesStaff',N'Assigned-store staff operations.'),(N'CRMStaff',N'Customer CRM operations.'),(N'BillingStaff',N'Billing operations.'),
    (N'CameraOperator',N'Camera and recognition operations.'),(N'IntegrationAdmin',N'Integration administration.'),(N'Auditor',N'Read-only audit operations.');
    INSERT dbo.Roles(TenantId,Scope,Name,NormalizedName,Description,IsSystem,IsActive,CreatedUtc)
    SELECT @TenantId,2,r.Name,UPPER(r.Name),r.Description,1,1,SYSUTCDATETIME() FROM @Roles r
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles x WHERE x.TenantId=@TenantId AND x.NormalizedName=UPPER(r.Name));

    /* TenantOwner/ShopOwner/TenantAdmin receive all tenant permissions. */
    INSERT dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p
    WHERE r.TenantId=@TenantId AND r.IsActive=1 AND p.Scope=2 AND p.IsActive=1
      AND r.NormalizedName IN(N'TENANTADMIN',N'TENANTOWNER',N'SHOPOWNER')
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);

    /* StoreAdmin/StoreManager remain store-scoped and cannot manage tenant-wide roles/settings. */
    INSERT dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p
    WHERE r.TenantId=@TenantId AND r.IsActive=1 AND p.Scope=2 AND p.IsActive=1
      AND r.NormalizedName IN(N'STOREADMIN',N'STOREMANAGER')
      AND p.Name NOT IN(N'TenantUsers.Create',N'TenantUsers.AssignRoles',N'TenantStores.Create',N'Roles.Manage',N'Settings.Manage')
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);

    /* SalesStaff receives least-privilege staff/customer/category/voice-use permissions. */
    INSERT dbo.RolePermissions(RoleId,PermissionId)
    SELECT r.Id,p.Id FROM dbo.Roles r CROSS JOIN dbo.Permissions p
    WHERE r.TenantId=@TenantId AND r.NormalizedName=N'SALESSTAFF' AND p.Scope=2 AND p.IsActive=1
      AND (p.Name IN(N'TenantDashboard.View',N'Staff.View',N'StoreCategories.View',N'VoiceCommands.Use',N'VoiceCommands.View',N'Customers.View',N'Customers.Create',N'Customers.Edit',N'Visits.View'))
      AND NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id);
END;
GO

/* Phase 5A — upgrade existing tenants with new roles/permissions. */
DECLARE @TenantId BIGINT;
DECLARE Phase5TenantCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Tenants;
OPEN Phase5TenantCursor; FETCH NEXT FROM Phase5TenantCursor INTO @TenantId;
WHILE @@FETCH_STATUS=0 BEGIN EXEC dbo.Tenant_ProvisionDefaultRoles @TenantId=@TenantId; FETCH NEXT FROM Phase5TenantCursor INTO @TenantId; END
CLOSE Phase5TenantCursor; DEALLOCATE Phase5TenantCursor;
GO

/* Phase 5G — real Customer Admin dashboard summary SP. */
CREATE OR ALTER PROCEDURE dbo.TenantDashboard_GetSummary @TenantId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
      (SELECT COUNT_BIG(*) FROM dbo.Users WHERE TenantId=@TenantId AND Scope=2 AND IsActive=1) ActiveUsers,
      (SELECT COUNT_BIG(*) FROM dbo.Stores WHERE TenantId=@TenantId AND IsActive=1) ActiveStores,
      (SELECT COUNT_BIG(*) FROM dbo.StaffProfiles WHERE TenantId=@TenantId AND IsActive=1) ActiveStaff,
      (SELECT COUNT_BIG(*) FROM dbo.ProductCategories WHERE TenantId=@TenantId AND IsActive=1) ActiveCategories,
      (SELECT COUNT_BIG(*) FROM dbo.StaffShifts WHERE TenantId=@TenantId AND Status=2) OpenShifts,
      (SELECT COUNT_BIG(*) FROM dbo.StaffPresenceSessions WHERE TenantId=@TenantId AND ExitedUtc IS NULL) ActivePresenceSessions;
END;
GO

/* Phase 5C — tenant-safe store search SP for admin/report use. */
CREATE OR ALTER PROCEDURE dbo.Store_Search @TenantId BIGINT,@Search NVARCHAR(150)=NULL,@ActiveOnly BIT=0
AS
BEGIN
 SET NOCOUNT ON;
 SELECT Id,StoreCode,StoreName,City,StateOrProvince,CountryCode,Latitude,Longitude,IsLocationVerified,TimeZone,IsActive,UpdatedUtc
 FROM dbo.Stores WHERE TenantId=@TenantId AND (@ActiveOnly=0 OR IsActive=1)
   AND (@Search IS NULL OR StoreCode LIKE N'%'+@Search+N'%' OR StoreName LIKE N'%'+@Search+N'%' OR City LIKE N'%'+@Search+N'%')
 ORDER BY StoreName;
END;
GO

/* Phase 5D — tenant-safe staff search SP. */
CREATE OR ALTER PROCEDURE dbo.Staff_Search @TenantId BIGINT,@StoreId BIGINT=NULL,@Search NVARCHAR(150)=NULL,@ActiveOnly BIT=0
AS
BEGIN
 SET NOCOUNT ON;
 SELECT DISTINCT s.Id,s.UserId,s.EmployeeCode,s.FirstName,s.LastName,s.Mobile,s.IsActive
 FROM dbo.StaffProfiles s LEFT JOIN dbo.UserStoreAssignments usa ON usa.TenantId=s.TenantId AND usa.UserId=s.UserId
 WHERE s.TenantId=@TenantId AND (@StoreId IS NULL OR usa.StoreId=@StoreId) AND (@ActiveOnly=0 OR s.IsActive=1)
 AND (@Search IS NULL OR s.EmployeeCode LIKE N'%'+@Search+N'%' OR s.FirstName LIKE N'%'+@Search+N'%' OR s.LastName LIKE N'%'+@Search+N'%')
 ORDER BY s.FirstName,s.LastName;
END;
GO

/* Phase 5H — database version ledger, idempotent. */
IF NOT EXISTS(SELECT 1 FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.4.0')
 INSERT dbo.DatabaseVersions(VersionNumber,Description,AppliedUtc,AppliedBy)
 VALUES(N'V1.4.0',N'Phase 5 tenant users, stores, staff, taxonomy and dynamic voice configuration',SYSUTCDATETIME(),SUSER_SNAME());
GO
