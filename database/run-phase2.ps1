[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance = 'KRUTARTH-BHAVSA'
)

$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
$foundationRunner = Join-Path $databaseRoot 'run-foundation.ps1'
$phaseScripts = @(
    '02_Tables\002_CreateTenantAuthenticationTables.sql',
    '03_Indexes\002_TenantAuthentication_Indexes.sql',
    '07_StoredProcedures\001_User_GetByIdForTenant.sql',
    '08_Seed\002_RecordPhase2Version.sql'
)

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required but was not found on PATH.'
}

if (-not (Test-Path -LiteralPath $foundationRunner)) {
    throw "Foundation runner not found: $foundationRunner"
}

& $foundationRunner -ServerInstance $ServerInstance

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

Write-Host 'Phase 2 database scripts completed successfully.'
