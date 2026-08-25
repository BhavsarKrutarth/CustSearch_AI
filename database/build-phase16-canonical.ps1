[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$databaseRoot = Split-Path -Parent $PSCommandPath
$canonicalPath = Join-Path $databaseRoot 'CustSearchAi.sql'
$upgradePath = Join-Path $databaseRoot '09_Upgrade\V1.15.0_Phase16_Operations.sql'
$canonical = [IO.File]::ReadAllText($canonicalPath)
if ($canonical -notmatch "VersionNumber=N'V1\.14\.0'") { throw 'Canonical SQL does not contain V1.14.0.' }
if ($canonical -match "VersionNumber=N'V1\.15\.0'") { Write-Output 'Canonical V1.15.0 already present.'; exit 0 }
$lineEnding = "`n"
$separator = if ($canonical.EndsWith("`n")) { $lineEnding } else { $lineEnding + $lineEnding }
$header = $separator + '-- ============================================================' + $lineEnding +
    '-- PHASE 16 - OPERATIONAL PLATFORM' + $lineEnding +
    '-- VERSION: V1.15.0' + $lineEnding +
    '-- ============================================================' + $lineEnding
$upgrade = [IO.File]::ReadAllText($upgradePath).Replace("`r`n", "`n").Replace("`n", $lineEnding).Trim()

# Preserve the canonical file's existing bytes/line endings so a phase append does not
# create a misleading whole-file diff. This script only adds the validated upgrade block.
[IO.File]::WriteAllText($canonicalPath, $canonical + $header + $upgrade + $lineEnding, [Text.UTF8Encoding]::new($false))
Write-Output 'Canonical V1.15.0 synchronized.'
