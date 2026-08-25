[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$ServerInstance = 'KRUTARTH-BHAVSA'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source
$canonicalVerifier = Join-Path $PSScriptRoot 'verify-phase16-canonical.ps1'

if (-not (Test-Path -LiteralPath $canonicalVerifier)) {
    throw 'The Phase 16 canonical verifier is missing.'
}

# Environment certification must use the SQL Server 2022 engine (major version 16). A database
# compatibility level of 160 on a newer engine is useful regression coverage, but is not equivalent.
$majorVersion = (& $sqlcmd -S $ServerInstance -d master -E -C -b -h -1 -W -Q `
    "SET NOCOUNT ON; SELECT CONVERT(int,SERVERPROPERTY('ProductMajorVersion'));" 2>&1 | Out-String).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "Could not connect to SQL Server instance '$ServerInstance'. $majorVersion"
}
if ($majorVersion -ne '16') {
    throw "SQL Server 2022 is required (major version 16); '$ServerInstance' reported major version '$majorVersion'."
}

Write-Host "SQL Server 2022 confirmed on $ServerInstance. Running isolated Phase 16 canonical validation."
& $canonicalVerifier -ServerInstance $ServerInstance
if ($LASTEXITCODE -ne 0) {
    throw "Phase 16 SQL Server 2022 validation failed with exit code $LASTEXITCODE."
}

Write-Host 'Phase 16 SQL Server 2022 validation: PASS'
