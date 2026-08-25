[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$databaseRoot = Split-Path -Parent $PSCommandPath
$canonicalPath = Join-Path $databaseRoot 'CustSearchAi.sql'
$upgradePath = Join-Path $databaseRoot '09_Upgrade\V1.15.0_Phase16_OperationalPlatform.sql'
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$canonical = [System.IO.File]::ReadAllText($canonicalPath)
$upgrade = [System.IO.File]::ReadAllText($upgradePath)
if ($canonical -notmatch "VersionNumber=N'V1\.14\.0'") { throw 'Canonical SQL must contain the validated V1.14.0 baseline.' }
$header = @'

-- ============================================================
-- PHASE 16 - OPERATIONAL PLATFORM
-- VERSION: V1.15.0
-- ============================================================
'@
$marker = '-- PHASE 16 - OPERATIONAL PLATFORM'
$markerIndex = $canonical.IndexOf($marker, [StringComparison]::Ordinal)
$headerStart = if ($markerIndex -ge 0) { $canonical.LastIndexOf('-- ============================================================', $markerIndex, [StringComparison]::Ordinal) } else { -1 }
if ($markerIndex -ge 0 -and $headerStart -lt 0) { throw 'Canonical Phase 16 header is malformed.' }
$baseline = if ($headerStart -ge 0) { $canonical.Substring(0, $headerStart).TrimEnd() } else { $canonical.TrimEnd() }
$final = $baseline + $header + $upgrade.Trim() + "`n"
[System.IO.File]::WriteAllText($canonicalPath, $final, $utf8NoBom)
Write-Output 'Synchronized V1.15.0 in database/CustSearchAi.sql.'
