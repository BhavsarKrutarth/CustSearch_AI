[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance = 'KRUTARTH-BHAVSA',

    [Parameter()]
    [switch] $ValidateIdempotency
)

# Phase 5 database runner. Uses Windows integrated security and applies earlier phases first.
$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
$phaseFourRunner = Join-Path $databaseRoot 'run-phase4.ps1'
$phaseFiveScript = Join-Path $databaseRoot '09_Upgrade\V1.4.0_Phase5_TenantStoresStaff.sql'

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required but was not found on PATH.'
}
if (-not (Test-Path -LiteralPath $phaseFourRunner)) {
    throw "Phase 4 runner not found: $phaseFourRunner"
}
if (-not (Test-Path -LiteralPath $phaseFiveScript)) {
    throw "Phase 5 upgrade script not found: $phaseFiveScript"
}

& $phaseFourRunner -ServerInstance $ServerInstance

$applyPhaseFive = {
    Write-Host "Applying Phase 5 V1.4.0 to $ServerInstance"
    & sqlcmd -S $ServerInstance -E -b -r 1 -d CustSearch_AI -i $phaseFiveScript
    if ($LASTEXITCODE -ne 0) {
        throw "Phase 5 database script failed with exit code $LASTEXITCODE"
    }
}

& $applyPhaseFive
if ($ValidateIdempotency) {
    Write-Host 'Reapplying Phase 5 to validate repeat safety.'
    & $applyPhaseFive
}

$validationQuery = @"
SET NOCOUNT ON;
IF DB_ID(N'CustSearch_AI') IS NULL THROW 51050, 'CustSearch_AI database was not found.', 1;
USE [CustSearch_AI];
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.4.0') <> 1 THROW 51051, 'Expected exactly one V1.4.0 database version row.', 1;
DECLARE @Missing TABLE(Name sysname);
INSERT @Missing(Name)
SELECT x.Name FROM (VALUES
(N'Stores'),(N'UserStoreAssignments'),(N'StaffProfiles'),(N'StaffShifts'),
(N'StaffPresenceSessions'),(N'ProductCategories'),(N'StoreVoiceCommandSettings'),(N'StoreVoiceCommandAliases')) x(Name)
WHERE OBJECT_ID(N'dbo.'+x.Name, N'U') IS NULL;
IF EXISTS(SELECT 1 FROM @Missing) THROW 51052, 'One or more Phase 5 tables are missing.', 1;
IF OBJECT_ID(N'dbo.TenantDashboard_GetSummary', N'P') IS NULL THROW 51053, 'TenantDashboard_GetSummary is missing.', 1;
IF OBJECT_ID(N'dbo.Store_Search', N'P') IS NULL THROW 51054, 'Store_Search is missing.', 1;
IF OBJECT_ID(N'dbo.Staff_Search', N'P') IS NULL THROW 51055, 'Staff_Search is missing.', 1;
SELECT VersionNumber, Description, AppliedUtc, AppliedBy FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.4.0';
"@

& sqlcmd -S $ServerInstance -E -b -r 1 -Q $validationQuery
if ($LASTEXITCODE -ne 0) {
    throw "Phase 5 validation failed with exit code $LASTEXITCODE"
}

Write-Host 'Phase 5 database scripts completed and validated successfully.'
