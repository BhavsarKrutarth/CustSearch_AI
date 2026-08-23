[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance = 'KRUTARTH-BHAVSA',

    [Parameter()]
    [switch] $ValidateIdempotency
)

# Phase 6 local database runner.
# Applies/validates the complete Phase 5 chain first, then V1.5.0 shopper customer + anonymous visitor persistence.
# Smart Profile in Phase 6 intentionally uses factual data from Customers + CustomerStoreAssignments + converted AnonymousVisitors;
# later household/visit/purchase/preference phases are not fabricated here.
# -ValidateIdempotency reapplies V1.5.0 and verifies duplicate-safe objects/version data.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
$databaseName = 'CustSearch_AI'
$phaseFiveRunner = Join-Path $databaseRoot 'run-phase5.ps1'
$phaseSixScript = Join-Path $databaseRoot '09_Upgrade\V1.5.0_Phase6_ShopperCustomers.sql'

function Assert-SqlCmdAvailable {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw 'sqlcmd is required but was not found on PATH. Install Microsoft SQL Server command-line utilities and retry.'
    }
}

function Invoke-PhaseSixUpgrade {
    if (-not (Test-Path -LiteralPath $phaseSixScript)) {
        throw "Phase 6 upgrade script not found: $phaseSixScript"
    }

    Write-Host "Applying Phase 6 V1.5.0 to $ServerInstance / $databaseName"
    & sqlcmd -S $ServerInstance -E -b -r 1 -d $databaseName -i $phaseSixScript
    if ($LASTEXITCODE -ne 0) {
        throw "Phase 6 database script failed with exit code $LASTEXITCODE"
    }
}

function Invoke-ValidationQuery {
    param([Parameter(Mandatory = $true)][string] $Query)

    Write-Host "Validating Phase 6 database objects on $ServerInstance / $databaseName"
    & sqlcmd -S $ServerInstance -E -b -r 1 -d $databaseName -Q $Query
    if ($LASTEXITCODE -ne 0) {
        throw "Phase 6 validation failed with exit code $LASTEXITCODE"
    }
}

Assert-SqlCmdAvailable

if (-not (Test-Path -LiteralPath $phaseFiveRunner)) {
    throw "Phase 5 runner not found: $phaseFiveRunner"
}

Write-Host "Applying and validating prerequisite Phase 0-5 chain on $ServerInstance"
& $phaseFiveRunner -ServerInstance $ServerInstance

Invoke-PhaseSixUpgrade

if ($ValidateIdempotency) {
    Write-Host 'Reapplying Phase 6 V1.5.0 to validate idempotency/re-run safety.'
    Invoke-PhaseSixUpgrade
}

$validationQuery = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_ID(N'CustSearch_AI') IS NULL
    THROW 51150, 'CustSearch_AI database was not found.', 1;

IF OBJECT_ID(N'dbo.DatabaseVersions', N'U') IS NULL
    THROW 51151, 'DatabaseVersions table is missing.', 1;
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.5.0') <> 1
    THROW 51152, 'Expected exactly one V1.5.0 Phase 6 database version row.', 1;

-- Phase 6 requires the Phase 5 store/authorization foundation before customer isolation can be enforced.
IF OBJECT_ID(N'dbo.Stores', N'U') IS NULL
    THROW 51153, 'Phase 5 Stores table is missing. Run database/run-phase5.ps1 first.', 1;
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL OR OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
    THROW 51154, 'Phase 5 authorization foundation is missing.', 1;

-- Phase 6A/6B: core customer and anonymous visitor tables.
IF OBJECT_ID(N'dbo.Customers',N'U') IS NULL
    THROW 51155, 'Customers table is missing.', 1;
IF OBJECT_ID(N'dbo.CustomerStoreAssignments',N'U') IS NULL
    THROW 51156, 'CustomerStoreAssignments table is missing.', 1;
IF OBJECT_ID(N'dbo.AnonymousVisitors',N'U') IS NULL
    THROW 51157, 'AnonymousVisitors table is missing.', 1;

-- Phase 6C: tenant/store-safe search procedures.
IF OBJECT_ID(N'dbo.Customer_Search',N'P') IS NULL
    THROW 51158, 'Customer_Search procedure is missing.', 1;
IF OBJECT_ID(N'dbo.AnonymousVisitor_Search',N'P') IS NULL
    THROW 51159, 'AnonymousVisitor_Search procedure is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.Customer_Search') AND name=N'@TenantId')
   OR NOT EXISTS(SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.Customer_Search') AND name=N'@AllowedStoreIdsCsv')
    THROW 51160, 'Customer_Search tenant/store authorization parameters are missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitor_Search') AND name=N'@TenantId')
   OR NOT EXISTS(SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitor_Search') AND name=N'@AllowedStoreIdsCsv')
    THROW 51161, 'AnonymousVisitor_Search tenant/store authorization parameters are missing.', 1;

-- Phase 6D smart-profile database foundation: factual customer identity/contact, store visibility and explicit visitor conversions.
DECLARE @RequiredCustomerColumns TABLE(Name sysname);
INSERT @RequiredCustomerColumns(Name) VALUES
(N'TenantId'),(N'CustomerCode'),(N'FirstName'),(N'LastName'),(N'Mobile'),(N'Email'),(N'Notes'),(N'IsActive'),(N'CreatedUtc'),(N'UpdatedUtc');
IF EXISTS(
    SELECT 1 FROM @RequiredCustomerColumns c
    WHERE COL_LENGTH(N'dbo.Customers', c.Name) IS NULL
)
    THROW 51162, 'One or more Phase 6 customer smart-profile foundation columns are missing.', 1;

IF COL_LENGTH(N'dbo.AnonymousVisitors', N'ConvertedCustomerId') IS NULL
   OR COL_LENGTH(N'dbo.AnonymousVisitors', N'ConvertedUtc') IS NULL
   OR COL_LENGTH(N'dbo.AnonymousVisitors', N'LastSeenUtc') IS NULL
    THROW 51163, 'Anonymous visitor conversion/last-seen columns required by Smart Profile are missing.', 1;

-- Phase 6G: tenant-safe composite relationships prevent cross-tenant customer/store/visitor links.
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'UX_Customers_Tenant_Id' AND is_unique=1)
    THROW 51164, 'UX_Customers_Tenant_Id unique index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'UX_Stores_Tenant_Id' AND is_unique=1)
    THROW 51165, 'UX_Stores_Tenant_Id unique index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_CustomerStoreAssignments_Customers_TenantCustomer' AND parent_object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments'))
    THROW 51166, 'Tenant-safe customer assignment -> customer foreign key is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_CustomerStoreAssignments_Stores_TenantStore' AND parent_object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments'))
    THROW 51167, 'Tenant-safe customer assignment -> store foreign key is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AnonymousVisitors_Stores_TenantStore' AND parent_object_id=OBJECT_ID(N'dbo.AnonymousVisitors'))
    THROW 51168, 'Tenant-safe visitor -> store foreign key is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AnonymousVisitors_Customers_TenantCustomer' AND parent_object_id=OBJECT_ID(N'dbo.AnonymousVisitors'))
    THROW 51169, 'Tenant-safe visitor -> converted customer foreign key is missing.', 1;

-- Phase 6 indexes for lookup, store scoping, filtered identity fields and visitor activity/conversion queries.
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'UX_Customers_Tenant_CustomerCode' AND is_unique=1)
    THROW 51170, 'UX_Customers_Tenant_CustomerCode unique index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'IX_Customers_Tenant_Mobile')
    THROW 51171, 'IX_Customers_Tenant_Mobile index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customers') AND name=N'IX_Customers_Tenant_Email')
    THROW 51172, 'IX_Customers_Tenant_Email index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments') AND name=N'IX_CustomerStoreAssignments_Tenant_Store')
    THROW 51173, 'IX_CustomerStoreAssignments_Tenant_Store index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CustomerStoreAssignments') AND name=N'UX_CustomerStoreAssignments_Primary' AND is_unique=1)
    THROW 51174, 'UX_CustomerStoreAssignments_Primary filtered unique index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'UX_AnonymousVisitors_Tenant_Store_Code' AND is_unique=1)
    THROW 51175, 'UX_AnonymousVisitors_Tenant_Store_Code unique index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'IX_AnonymousVisitors_Tenant_Store_Active_LastSeen')
    THROW 51176, 'IX_AnonymousVisitors_Tenant_Store_Active_LastSeen index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AnonymousVisitors') AND name=N'IX_AnonymousVisitors_Tenant_ConvertedCustomer')
    THROW 51177, 'IX_AnonymousVisitors_Tenant_ConvertedCustomer index is missing.', 1;

-- Phase 6 permissions must exist once for tenant roles/CRM staff; TenantId is supplied by authenticated server context, not payloads.
DECLARE @MissingPermissions TABLE(Name nvarchar(150));
INSERT @MissingPermissions(Name)
SELECT required.Name
FROM (VALUES
    (N'Customers.View'),
    (N'Customers.Create'),
    (N'Customers.Edit'),
    (N'Visitors.View'),
    (N'Visitors.Convert')
) required(Name)
WHERE NOT EXISTS(
    SELECT 1 FROM dbo.Permissions p
    WHERE p.Scope=2 AND p.Name=required.Name AND p.IsActive=1
);
IF EXISTS(SELECT 1 FROM @MissingPermissions)
    THROW 51178, 'One or more Phase 6 customer/visitor permissions are missing/inactive.', 1;

-- Duplicate-safe checks: one version row and no duplicate business keys inside the same tenant/store scope.
IF EXISTS(
    SELECT TenantId, CustomerCode
    FROM dbo.Customers
    GROUP BY TenantId, CustomerCode
    HAVING COUNT_BIG(*) > 1
)
    THROW 51179, 'Duplicate tenant customer codes were detected.', 1;
IF EXISTS(
    SELECT TenantId, StoreId, VisitorCode
    FROM dbo.AnonymousVisitors
    GROUP BY TenantId, StoreId, VisitorCode
    HAVING COUNT_BIG(*) > 1
)
    THROW 51180, 'Duplicate anonymous visitor codes were detected inside a tenant/store.', 1;
IF EXISTS(
    SELECT CustomerId
    FROM dbo.CustomerStoreAssignments
    WHERE IsPrimary=1
    GROUP BY CustomerId
    HAVING COUNT_BIG(*) > 1
)
    THROW 51181, 'A customer has more than one primary store assignment.', 1;

SELECT VersionNumber, Description, AppliedUtc, AppliedBy
FROM dbo.DatabaseVersions
WHERE VersionNumber=N'V1.5.0';
"@

Invoke-ValidationQuery -Query $validationQuery
Write-Host 'Phase 6 database scripts completed and validated successfully.'
