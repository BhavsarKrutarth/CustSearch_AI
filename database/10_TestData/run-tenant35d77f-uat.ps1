[CmdletBinding()]
param(
    [string]$ServerInstance = 'KRUTARTH-BHAVSA',
    [string]$DatabaseName = 'CustSearch_AI'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^[A-Za-z0-9_]+$') { throw 'DatabaseName contains unsupported characters.' }
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw 'sqlcmd is required.' }

$seedPath = Join-Path $PSScriptRoot 'Tenant35D77F_UatData.sql'
$verifyPath = Join-Path $PSScriptRoot 'Tenant35D77F_UatData_Verify.sql'
& sqlcmd -S $ServerInstance -d $DatabaseName -E -N -C -b -r 1 -i $seedPath
if ($LASTEXITCODE -ne 0) { throw "Tenant UAT seed failed with exit code $LASTEXITCODE." }
& sqlcmd -S $ServerInstance -d $DatabaseName -E -N -C -b -r 1 -i $verifyPath
if ($LASTEXITCODE -ne 0) { throw "Tenant UAT verification failed with exit code $LASTEXITCODE." }
