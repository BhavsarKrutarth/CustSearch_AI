[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$databaseRoot=Split-Path -Parent $PSCommandPath
$canonicalPath=Join-Path $databaseRoot 'CustSearchAi.sql'
$upgradePath=Join-Path $databaseRoot '09_Upgrade\V1.16.0_Phase18_RetailSecurity.sql'
$encoding=[System.Text.UTF8Encoding]::new($false)
$canonical=[System.IO.File]::ReadAllText($canonicalPath)
$upgrade=[System.IO.File]::ReadAllText($upgradePath)
if($canonical -notmatch "VersionNumber=N'V1\.15\.0'"){throw 'Canonical SQL must contain V1.15.0.'}
$header="`n`n-- ============================================================`n-- PHASE 18 - REVIEWABLE RETAIL SECURITY`n-- VERSION: V1.16.0`n-- ============================================================`n"
$marker='-- PHASE 18 - REVIEWABLE RETAIL SECURITY';$index=$canonical.IndexOf($marker,[StringComparison]::Ordinal)
if($index -ge 0){$start=$canonical.LastIndexOf('-- ============================================================',$index,[StringComparison]::Ordinal);if($start -lt 0){throw 'Canonical Phase 18 header is malformed.'};$canonical=$canonical.Substring(0,$start).TrimEnd()}
[System.IO.File]::WriteAllText($canonicalPath,$canonical+$header+$upgrade.Trim()+"`n",$encoding)
Write-Output 'Synchronized V1.16.0 in database/CustSearchAi.sql.'
