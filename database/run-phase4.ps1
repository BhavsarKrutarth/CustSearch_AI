[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance = 'KRUTARTH-BHAVSA',

    [Parameter()]
    [switch] $ValidateIdempotency
)

# Phase 4 local database runner.
# Connection model: Windows Integrated Security against KRUTARTH-BHAVSA / CustSearch_AI.
# The API connection string remains:
# Server=KRUTARTH-BHAVSA;Database=CustSearch_AI;Integrated Security=True;Encrypt=True;TrustServerCertificate=True
#
# This runner first applies Phase 3 dependencies, then applies every Phase 4 SQL script in deterministic order.
# -ValidateIdempotency reapplies only the Phase 4 script set and then runs structural/version validation.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
$databaseName = 'CustSearch_AI'
$phaseThreeRunner = Join-Path $databaseRoot 'run-phase3.ps1'
$phaseScripts = @(
    '02_Tables\004_CreatePlatformTenantManagementTables.sql',
    '03_Indexes\004_PlatformTenantManagement_Indexes.sql',
    '07_StoredProcedures\003_Tenant_ProvisionDefaultRoles.sql',
    '07_StoredProcedures\004_Tenant_GetDetailSummary.sql',
    '07_StoredProcedures\005_Tenant_GetUsageSummary.sql',
    '08_Seed\005_SeedPhase4Defaults.sql',
    '08_Seed\006_RecordPhase4Version.sql'
)

function Assert-SqlCmdAvailable {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw 'sqlcmd is required but was not found on PATH. Install Microsoft SQL Server command-line utilities and retry.'
    }
}

function Invoke-DatabaseScript {
    param(
        [Parameter(Mandatory = $true)][string] $ScriptPath,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -LiteralPath $ScriptPath)) {
        throw "Required database script not found: $ScriptPath"
    }

    Write-Host "Applying $Label to $ServerInstance / $databaseName"
    & sqlcmd -S $ServerInstance -E -b -r 1 -d $databaseName -i $ScriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "Database script failed with exit code ${LASTEXITCODE}: $Label"
    }
}

function Invoke-PhaseFourScripts {
    foreach ($relativeScript in $phaseScripts) {
        Invoke-DatabaseScript -ScriptPath (Join-Path $databaseRoot $relativeScript) -Label $relativeScript
    }
}

function Invoke-ValidationQuery {
    param([Parameter(Mandatory = $true)][string] $Query)

    Write-Host "Validating Phase 4 database objects on $ServerInstance / $databaseName"
    & sqlcmd -S $ServerInstance -E -b -r 1 -d $databaseName -Q $Query
    if ($LASTEXITCODE -ne 0) {
        throw "Phase 4 validation failed with exit code $LASTEXITCODE"
    }
}

Assert-SqlCmdAvailable

if (-not (Test-Path -LiteralPath $phaseThreeRunner)) {
    throw "Phase 3 runner not found: $phaseThreeRunner"
}

Write-Host "Applying prerequisite Phase 0-3 chain to $ServerInstance"
& $phaseThreeRunner -ServerInstance $ServerInstance
if ($LASTEXITCODE -ne 0) {
    throw "Phase 3 prerequisite runner failed with exit code $LASTEXITCODE"
}

Invoke-PhaseFourScripts

if ($ValidateIdempotency) {
    Write-Host 'Reapplying Phase 4 scripts to validate idempotency/re-run safety.'
    Invoke-PhaseFourScripts
}

$validationQuery = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_ID(N'CustSearch_AI') IS NULL
    THROW 50940, 'CustSearch_AI database was not found.', 1;

IF OBJECT_ID(N'dbo.DatabaseVersions', N'U') IS NULL
    THROW 50941, 'DatabaseVersions table is missing.', 1;

IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.3.0') <> 1
    THROW 50942, 'Expected exactly one V1.3.0 Phase 4 database version row.', 1;

DECLARE @MissingTables TABLE(Name sysname);
INSERT @MissingTables(Name)
SELECT x.Name
FROM (VALUES
    (N'SubscriptionPlans'),
    (N'TenantSubscriptions'),
    (N'TenantUsageSnapshots'),
    (N'TenantQuotaOverrides'),
    (N'AuditLogs')
) x(Name)
WHERE OBJECT_ID(N'dbo.' + x.Name, N'U') IS NULL;

IF EXISTS(SELECT 1 FROM @MissingTables)
    THROW 50943, 'One or more Phase 4 tables are missing.', 1;

IF COL_LENGTH(N'dbo.Tenants', N'PrimaryContactName') IS NULL
   OR COL_LENGTH(N'dbo.Tenants', N'PrimaryEmail') IS NULL
   OR COL_LENGTH(N'dbo.Tenants', N'SubscriptionPlanId') IS NULL
   OR COL_LENGTH(N'dbo.Tenants', N'MaxStores') IS NULL
   OR COL_LENGTH(N'dbo.Tenants', N'MaxUsers') IS NULL
   OR COL_LENGTH(N'dbo.Tenants', N'MaxCameras') IS NULL
   OR COL_LENGTH(N'dbo.Tenants', N'UpdatedUtc') IS NULL
    THROW 50944, 'One or more required Phase 4 tenant columns are missing.', 1;

IF OBJECT_ID(N'dbo.Tenant_ProvisionDefaultRoles', N'P') IS NULL
    THROW 50945, 'Tenant_ProvisionDefaultRoles procedure is missing.', 1;
IF OBJECT_ID(N'dbo.Tenant_GetDetailSummary', N'P') IS NULL
    THROW 50946, 'Tenant_GetDetailSummary procedure is missing.', 1;
IF OBJECT_ID(N'dbo.Tenant_GetUsageSummary', N'P') IS NULL
    THROW 50947, 'Tenant_GetUsageSummary procedure is missing.', 1;

IF OBJECT_ID(N'dbo.TR_TenantQuotaOverrides_RequirePlatformActor', N'TR') IS NULL
    THROW 50948, 'TR_TenantQuotaOverrides_RequirePlatformActor trigger is missing.', 1;
IF OBJECT_ID(N'dbo.TR_TenantSubscriptions_OneCurrent', N'TR') IS NULL
    THROW 50949, 'TR_TenantSubscriptions_OneCurrent trigger is missing.', 1;

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SubscriptionPlans') AND name=N'UX_SubscriptionPlans_PlanCode')
    THROW 50950, 'UX_SubscriptionPlans_PlanCode index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TenantUsageSnapshots') AND name=N'UX_TenantUsageSnapshots_Tenant_Period')
    THROW 50951, 'UX_TenantUsageSnapshots_Tenant_Period index is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AuditLogs') AND name=N'IX_AuditLogs_TenantId_CreatedUtc')
    THROW 50952, 'IX_AuditLogs_TenantId_CreatedUtc index is missing.', 1;

IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.Tenants') AND name=N'FK_Tenants_SubscriptionPlans_SubscriptionPlanId')
    THROW 50953, 'Tenant subscription plan foreign key is missing.', 1;

SELECT VersionNumber, Description, AppliedUtc, AppliedBy
FROM dbo.DatabaseVersions
WHERE VersionNumber=N'V1.3.0';
"@

Invoke-ValidationQuery -Query $validationQuery
Write-Host 'Phase 4 database scripts completed and validated successfully.'
