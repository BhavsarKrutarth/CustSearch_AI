[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$ServerInstance = 'localhost',

    [Parameter()]
    [ValidatePattern('^[A-Za-z0-9_]+$')]
    [string]$DatabaseName = 'CustSearch_AI',

    [Parameter()]
    [switch]$ValidateIdempotency
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
$upgradePath = Join-Path $databaseRoot '09_Upgrade\V1.15.0_Phase16_Operations.sql'
$verifyPath = Join-Path $databaseRoot 'verify-phase16.sql'
$temporaryUpgrade = Join-Path ([IO.Path]::GetTempPath()) ("custsearch-phase16-{0}.sql" -f [guid]::NewGuid().ToString('N'))
$temporaryVerify = Join-Path ([IO.Path]::GetTempPath()) ("custsearch-phase16-verify-{0}.sql" -f [guid]::NewGuid().ToString('N'))

# The committed T-SQL remains SSMS-friendly with an explicit database context. The local runner
# substitutes only the validated database identifier and always uses Windows Integrated Security.
function New-DatabaseSpecificScript([string]$Source, [string]$Destination) {
    $sql = [IO.File]::ReadAllText($Source).Replace('USE [CustSearch_AI];', "USE [$DatabaseName];")
    [IO.File]::WriteAllText($Destination, $sql, [Text.UTF8Encoding]::new($false))
}

function Invoke-SqlFile([string]$Path, [string]$Description) {
    Write-Host "$Description on $ServerInstance / $DatabaseName"
    & sqlcmd -S $ServerInstance -d master -E -C -b -r 1 -i $Path
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required but was not found on PATH.'
}
if (-not (Test-Path -LiteralPath $upgradePath) -or -not (Test-Path -LiteralPath $verifyPath)) {
    throw 'Phase 16 upgrade or verifier script is missing.'
}

try {
    New-DatabaseSpecificScript $upgradePath $temporaryUpgrade
    New-DatabaseSpecificScript $verifyPath $temporaryVerify
    Invoke-SqlFile $temporaryUpgrade 'Applying Phase 16 V1.15.0'
    if ($ValidateIdempotency) {
        Invoke-SqlFile $temporaryUpgrade 'Reapplying Phase 16 V1.15.0 for idempotency validation'
    }
    Invoke-SqlFile $temporaryVerify 'Verifying Phase 16 V1.15.0 and constraints'
}
finally {
    if ([IO.File]::Exists($temporaryUpgrade)) { [IO.File]::Delete($temporaryUpgrade) }
    if ([IO.File]::Exists($temporaryVerify)) { [IO.File]::Delete($temporaryVerify) }
}
