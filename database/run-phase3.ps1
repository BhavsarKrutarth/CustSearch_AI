[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance = 'KRUTARTH-BHAVSA'
)

# This runner applies Phase 2 first so Phase 3 always receives its required tenant and user tables.
$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
$phaseTwoRunner = Join-Path $databaseRoot 'run-phase2.ps1'
$phaseScripts = @(
    '02_Tables\003_CreateAuthorizationTables.sql',
    '09_Upgrade\001_AddRefreshTokenIssuedSecurityStamp.sql',
    '03_Indexes\003_Authorization_Indexes.sql',
    '08_Seed\003_SeedAuthorizationCatalog.sql',
    '07_StoredProcedures\002_UserAuthorization_GetForScope.sql',
    '08_Seed\004_RecordPhase3Version.sql'
)

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required but was not found on PATH.'
}

if (-not (Test-Path -LiteralPath $phaseTwoRunner)) {
    throw "Phase 2 runner not found: $phaseTwoRunner"
}

& $phaseTwoRunner -ServerInstance $ServerInstance

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

Write-Host 'Phase 3 database scripts completed successfully.'
