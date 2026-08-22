[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance = 'KRUTARTH-BHAVSA'
)

$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
$scripts = @(
    '01_Database\001_CreateDatabase.sql',
    '02_Tables\001_CreateDatabaseVersions.sql',
    '03_Indexes\001_DatabaseVersions_Indexes.sql',
    '08_Seed\001_RecordFoundationVersion.sql'
)

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required but was not found on PATH.'
}

foreach ($relativeScript in $scripts) {
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

Write-Host 'Foundation database scripts completed successfully.'
