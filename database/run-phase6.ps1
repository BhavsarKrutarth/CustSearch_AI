[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ServerInstance = 'KRUTARTH-BHAVSA',

    [Parameter()]
    [switch] $ValidateIdempotency
)

# Phase 6 database runner. Uses Windows integrated security and applies the completed Phase 5 chain first.
$ErrorActionPreference = 'Stop'
$databaseRoot = $PSScriptRoot
$phaseFiveRunner = Join-Path $databaseRoot 'run-phase5.ps1'
$phaseSixScript = Join-Path $databaseRoot '09_Upgrade\V1.5.0_Phase6_ShopperCustomers.sql'

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required but was not found on PATH.'
}
if (-not (Test-Path -LiteralPath $phaseFiveRunner)) {
    throw "Phase 5 runner not found: $phaseFiveRunner"
}
if (-not (Test-Path -LiteralPath $phaseSixScript)) {
    throw "Phase 6 upgrade script not found: $phaseSixScript"
}

& $phaseFiveRunner -ServerInstance $ServerInstance

$applyPhaseSix = {
    Write-Host "Applying Phase 6 V1.5.0 to $ServerInstance"
    & sqlcmd -S $ServerInstance -E -b -r 1 -d CustSearch_AI -i $phaseSixScript
    if ($LASTEXITCODE -ne 0) {
        throw "Phase 6 database script failed with exit code $LASTEXITCODE"
    }
}

& $applyPhaseSix
if ($ValidateIdempotency) {
    Write-Host 'Reapplying Phase 6 to validate repeat safety.'
    & $applyPhaseSix
}

$validationQuery = @"
SET NOCOUNT ON;
IF DB_ID(N'CustSearch_AI') IS NULL THROW 51150, 'CustSearch_AI database was not found.', 1;
USE [CustSearch_AI];
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.5.0') <> 1 THROW 51151, 'Expected exactly one V1.5.0 database version row.', 1;
IF OBJECT_ID(N'dbo.Customers',N'U') IS NULL THROW 51152, 'Customers table is missing.', 1;
IF OBJECT_ID(N'dbo.CustomerStoreAssignments',N'U') IS NULL THROW 51153, 'CustomerStoreAssignments table is missing.', 1;
IF OBJECT_ID(N'dbo.AnonymousVisitors',N'U') IS NULL THROW 51154, 'AnonymousVisitors table is missing.', 1;
IF OBJECT_ID(N'dbo.Customer_Search',N'P') IS NULL THROW 51155, 'Customer_Search procedure is missing.', 1;
IF OBJECT_ID(N'dbo.AnonymousVisitor_Search',N'P') IS NULL THROW 51156, 'AnonymousVisitor_Search procedure is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_CustomerStoreAssignments_Customers_TenantCustomer') THROW 51157, 'Tenant-safe customer assignment FK is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AnonymousVisitors_Stores_TenantStore') THROW 51158, 'Tenant-safe visitor/store FK is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Customers.View') THROW 51159, 'Customers.View permission is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.Permissions WHERE Scope=2 AND Name=N'Visitors.Convert') THROW 51160, 'Visitors.Convert permission is missing.', 1;
SELECT VersionNumber,Description,AppliedUtc,AppliedBy FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.5.0';
"@

& sqlcmd -S $ServerInstance -E -b -r 1 -Q $validationQuery
if ($LASTEXITCODE -ne 0) {
    throw "Phase 6 validation failed with exit code $LASTEXITCODE"
}

Write-Host 'Phase 6 database scripts completed and validated successfully.'
