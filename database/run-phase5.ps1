[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance = 'KRUTARTH-BHAVSA',

    [Parameter()]
    [switch] $ValidateIdempotency
)

# Phase 5 local database runner.
# Applies/validates the complete Phase 4 dependency chain first, then V1.4.0.
# Phase 5 reuses the existing Users/Roles/Permissions authorization schema and adds tenant-store/staff/category/voice objects.
# -ValidateIdempotency reapplies V1.4.0 and verifies that no duplicate version/object state is produced.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
$databaseName = 'CustSearch_AI'
$phaseFourRunner = Join-Path $databaseRoot 'run-phase4.ps1'
$phaseFiveScript = Join-Path $databaseRoot '09_Upgrade\V1.4.0_Phase5_TenantStoresStaff.sql'

function Assert-SqlCmdAvailable {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw 'sqlcmd is required but was not found on PATH. Install Microsoft SQL Server command-line utilities and retry.'
    }
}

function Invoke-PhaseFiveUpgrade {
    if (-not (Test-Path -LiteralPath $phaseFiveScript)) {
        throw "Phase 5 upgrade script not found: $phaseFiveScript"
    }

    Write-Host "Applying Phase 5 V1.4.0 to $ServerInstance / $databaseName"
    & sqlcmd -S $ServerInstance -E -b -r 1 -d $databaseName -i $phaseFiveScript
    if ($LASTEXITCODE -ne 0) {
        throw "Phase 5 database script failed with exit code $LASTEXITCODE"
    }
}

function Invoke-ValidationQuery {
    param([Parameter(Mandatory = $true)][string] $Query)

    Write-Host "Validating Phase 5 database objects on $ServerInstance / $databaseName"
    & sqlcmd -S $ServerInstance -E -b -r 1 -d $databaseName -Q $Query
    if ($LASTEXITCODE -ne 0) {
        throw "Phase 5 validation failed with exit code $LASTEXITCODE"
    }
}

Assert-SqlCmdAvailable

if (-not (Test-Path -LiteralPath $phaseFourRunner)) {
    throw "Phase 4 runner not found: $phaseFourRunner"
}

Write-Host "Applying and validating prerequisite Phase 0-4 chain on $ServerInstance"
& $phaseFourRunner -ServerInstance $ServerInstance

Invoke-PhaseFiveUpgrade

if ($ValidateIdempotency) {
    Write-Host 'Reapplying Phase 5 V1.4.0 to validate idempotency/re-run safety.'
    Invoke-PhaseFiveUpgrade
}

$validationQuery = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_ID(N'CustSearch_AI') IS NULL
    THROW 51050, 'CustSearch_AI database was not found.', 1;

IF OBJECT_ID(N'dbo.DatabaseVersions', N'U') IS NULL
    THROW 51051, 'DatabaseVersions table is missing.', 1;
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.4.0') <> 1
    THROW 51052, 'Expected exactly one V1.4.0 Phase 5 database version row.', 1;

-- Phase 5A: existing identity/authorization foundation must remain available.
DECLARE @MissingAuthorizationTables TABLE(Name sysname);
INSERT @MissingAuthorizationTables(Name)
SELECT x.Name
FROM (VALUES
    (N'Users'),
    (N'Roles'),
    (N'Permissions'),
    (N'UserRoles'),
    (N'RolePermissions')
) x(Name)
WHERE OBJECT_ID(N'dbo.' + x.Name, N'U') IS NULL;
IF EXISTS(SELECT 1 FROM @MissingAuthorizationTables)
    THROW 51053, 'One or more Users/Roles authorization tables required by Phase 5 are missing.', 1;

-- Phase 5B-5F: store assignment, stores, staff, shifts/presence, categories and dynamic voice configuration.
DECLARE @MissingPhase5Tables TABLE(Name sysname);
INSERT @MissingPhase5Tables(Name)
SELECT x.Name
FROM (VALUES
    (N'Stores'),
    (N'UserStoreAssignments'),
    (N'StaffProfiles'),
    (N'StaffShifts'),
    (N'StaffPresenceSessions'),
    (N'ProductCategories'),
    (N'StoreVoiceCommandSettings'),
    (N'StoreVoiceCommandAliases')
) x(Name)
WHERE OBJECT_ID(N'dbo.' + x.Name, N'U') IS NULL;
IF EXISTS(SELECT 1 FROM @MissingPhase5Tables)
    THROW 51054, 'One or more Phase 5 tables are missing.', 1;

-- Required Phase 5 stored procedures.
IF OBJECT_ID(N'dbo.Tenant_ProvisionDefaultRoles', N'P') IS NULL
    THROW 51055, 'Tenant_ProvisionDefaultRoles is missing.', 1;
IF OBJECT_ID(N'dbo.TenantDashboard_GetSummary', N'P') IS NULL
    THROW 51056, 'TenantDashboard_GetSummary is missing.', 1;
IF OBJECT_ID(N'dbo.Store_Search', N'P') IS NULL
    THROW 51057, 'Store_Search is missing.', 1;
IF OBJECT_ID(N'dbo.Staff_Search', N'P') IS NULL
    THROW 51058, 'Staff_Search is missing.', 1;

-- Required uniqueness/query indexes.
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Stores') AND name=N'UX_Stores_Tenant_StoreCode' AND is_unique=1)
    THROW 51059, 'UX_Stores_Tenant_StoreCode unique index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.UserStoreAssignments') AND name=N'IX_UserStoreAssignments_Tenant_Store')
    THROW 51060, 'IX_UserStoreAssignments_Tenant_Store index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffProfiles') AND name=N'UX_StaffProfiles_Tenant_EmployeeCode' AND is_unique=1)
    THROW 51061, 'UX_StaffProfiles_Tenant_EmployeeCode unique index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffShifts') AND name=N'IX_StaffShifts_Tenant_Store_Start')
    THROW 51062, 'IX_StaffShifts_Tenant_Store_Start index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StaffPresenceSessions') AND name=N'IX_StaffPresence_Tenant_Store_Entered')
    THROW 51063, 'IX_StaffPresence_Tenant_Store_Entered index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductCategories') AND name=N'UX_ProductCategories_Tenant_Store_Code' AND is_unique=1)
    THROW 51064, 'UX_ProductCategories_Tenant_Store_Code unique index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.StoreVoiceCommandAliases') AND name=N'UX_StoreVoiceCommandAliases_Tenant_Store_Alias' AND is_unique=1)
    THROW 51065, 'UX_StoreVoiceCommandAliases_Tenant_Store_Alias unique index is missing.', 1;

-- Required relationships/constraints for tenant-safe Phase 5 data.
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_UserStoreAssignments_Stores_StoreId' AND parent_object_id=OBJECT_ID(N'dbo.UserStoreAssignments'))
    THROW 51066, 'UserStoreAssignments -> Stores foreign key is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_StaffShifts_Stores' AND parent_object_id=OBJECT_ID(N'dbo.StaffShifts'))
    THROW 51067, 'StaffShifts -> Stores foreign key is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_StaffPresence_Stores' AND parent_object_id=OBJECT_ID(N'dbo.StaffPresenceSessions'))
    THROW 51068, 'StaffPresenceSessions -> Stores foreign key is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_ProductCategories_Stores' AND parent_object_id=OBJECT_ID(N'dbo.ProductCategories'))
    THROW 51069, 'ProductCategories -> Stores foreign key is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_StoreVoiceCommandSettings_Stores' AND parent_object_id=OBJECT_ID(N'dbo.StoreVoiceCommandSettings'))
    THROW 51070, 'StoreVoiceCommandSettings -> Stores foreign key is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AuditLogs_Stores_StoreId' AND parent_object_id=OBJECT_ID(N'dbo.AuditLogs'))
    THROW 51071, 'AuditLogs -> Stores foreign key is missing.', 1;

-- Tenant-level Phase 5 permissions are materialized once and reused by default roles.
DECLARE @MissingPermissions TABLE(Name nvarchar(150));
INSERT @MissingPermissions(Name)
SELECT required.Name
FROM (VALUES
    (N'TenantUsers.View'),
    (N'TenantUsers.Create'),
    (N'TenantUsers.Edit'),
    (N'TenantUsers.Deactivate'),
    (N'TenantUsers.AssignRoles'),
    (N'TenantStores.View'),
    (N'TenantStores.Create'),
    (N'TenantStores.Edit'),
    (N'Staff.View'),
    (N'Staff.Manage'),
    (N'StaffTracking.View'),
    (N'StoreCategories.View'),
    (N'StoreCategories.Manage'),
    (N'VoiceCommands.Use'),
    (N'VoiceCommands.View'),
    (N'VoiceCommands.Configure'),
    (N'VoiceCommands.Audit')
) required(Name)
WHERE NOT EXISTS(
    SELECT 1 FROM dbo.Permissions p
    WHERE p.Scope=2 AND p.Name=required.Name AND p.IsActive=1
);
IF EXISTS(SELECT 1 FROM @MissingPermissions)
    THROW 51072, 'One or more Phase 5 permissions are missing/inactive.', 1;

SELECT VersionNumber, Description, AppliedUtc, AppliedBy
FROM dbo.DatabaseVersions
WHERE VersionNumber=N'V1.4.0';
"@

Invoke-ValidationQuery -Query $validationQuery
Write-Host 'Phase 5 database scripts completed and validated successfully.'
