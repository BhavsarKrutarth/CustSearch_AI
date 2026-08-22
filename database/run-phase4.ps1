[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance = 'KRUTARTH-BHAVSA'
)

# This runner applies Phase 3 first so tenant management always receives identity and authorization dependencies.
$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
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

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required but was not found on PATH.'
}

if (-not (Test-Path -LiteralPath $phaseThreeRunner)) {
    throw "Phase 3 runner not found: $phaseThreeRunner"
}

& $phaseThreeRunner -ServerInstance $ServerInstance

foreach ($relativeScript in $phaseScripts) {
    $scriptPath = Join-Path $databaseRoot $relativeScript
    if (-not (Test-Path -LiteralPath $scriptPath)) {
        throw "Required database script not found: $scriptPath"
    }

    Write-Host "Applying $relativeScript to $ServerInstance"
    & sqlcmd -S $ServerInstance -E -b -r 1 -i $scriptPath
    if ($LASTEXITCODE -ne 0) {
        throw "Database script failed with exit code ${LASTEXITCODE}: $relativeScript"
    }
}

Write-Host 'Phase 4 database scripts completed successfully.'
