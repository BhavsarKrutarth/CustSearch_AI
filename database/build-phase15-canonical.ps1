[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$databaseRoot = Split-Path -Parent $PSCommandPath
$canonicalPath = Join-Path $databaseRoot 'CustSearchAi.sql'
$upgradePath = Join-Path $databaseRoot '09_Upgrade\V1.14.0_Phase15_ReportsExports.sql'
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

$canonical = [System.IO.File]::ReadAllText($canonicalPath)
$upgrade = [System.IO.File]::ReadAllText($upgradePath)
if ($canonical -notmatch "VersionNumber=N'V1\.13\.0'") {
    throw 'Canonical SQL must contain the validated V1.13.0 baseline.'
}

$header = @'

-- ============================================================
-- PHASE 15 - REPORTS AND ASYNC EXPORTS
-- VERSION: V1.14.0
-- ============================================================
'@
$versionMatches = [regex]::Matches($canonical, "VersionNumber=N'V1\.14\.0'").Count
if ($versionMatches -gt 0 -and $versionMatches -ne 2) {
    throw "Canonical V1.14.0 marker count is unexpected: $versionMatches."
}
$phaseMarker = '-- PHASE 15 - REPORTS AND ASYNC EXPORTS'
$markerIndex = $canonical.IndexOf($phaseMarker, [StringComparison]::Ordinal)
if ($versionMatches -gt 0 -and $markerIndex -lt 0) {
    throw 'Canonical V1.14.0 exists without the expected Phase 15 marker.'
}
$headerStart = if ($markerIndex -ge 0) { $canonical.LastIndexOf('-- ============================================================', $markerIndex, [StringComparison]::Ordinal) } else { -1 }
if ($markerIndex -ge 0 -and $headerStart -lt 0) { throw 'Canonical Phase 15 header is malformed.' }
$baseline = if ($headerStart -ge 0) { $canonical.Substring(0, $headerStart).TrimEnd() } else { $canonical.TrimEnd() }
$final = $baseline + $header + $upgrade.Trim() + "`n"
[System.IO.File]::WriteAllText($canonicalPath, $final, $utf8NoBom)
Write-Output 'Synchronized V1.14.0 in database/CustSearchAi.sql.'
