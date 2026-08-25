[CmdletBinding()]
param(
    [string]$ServerInstance = 'KRUTARTH-BHAVSA',
    [string]$DatabaseName = ('CustSearch_AI_Phase16_Verify_' + (Get-Date -Format 'yyyyMMddHHmmss') + '_' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
)

$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^CustSearch_AI_Phase16_Verify_[A-Za-z0-9_]{1,70}$') {
    throw 'DatabaseName must be an isolated CustSearch_AI_Phase16_Verify_* name.'
}

$databaseRoot = Split-Path -Parent $PSCommandPath
$canonicalPath = Join-Path $databaseRoot 'CustSearchAi.sql'
$upgradePath = Join-Path $databaseRoot '09_Upgrade\V1.15.0_Phase16_Operations.sql'
$temporarySql = Join-Path ([IO.Path]::GetTempPath()) ('custsearch-phase16-' + [Guid]::NewGuid().ToString('N') + '.sql')
$encoding = [Text.UTF8Encoding]::new($false)
$sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source
$exitCode = 0

try {
    & $sqlcmd -S $ServerInstance -d master -E -C -b -Q "IF DB_ID(N'$DatabaseName') IS NOT NULL THROW 55230,'Disposable validation database already exists.',1;"
    if ($LASTEXITCODE -ne 0) { throw "Phase 16 preflight failed with exit code $LASTEXITCODE." }

    $canonical = [IO.File]::ReadAllText($canonicalPath)
    if ($canonical -notmatch "VersionNumber=N'V1\.14\.0'") { throw 'Canonical SQL does not contain the validated V1.14.0 prerequisite.' }
    if ($canonical -notmatch "VersionNumber=N'V1\.15\.0'") {
        $canonical = $canonical.TrimEnd() + "`n`n-- ============================================================`n-- PHASE 16 - OPERATIONAL PLATFORM`n-- VERSION: V1.15.0`n-- ============================================================`n" + [IO.File]::ReadAllText($upgradePath).Trim() + "`n"
    }
    [IO.File]::WriteAllText($temporarySql, $canonical.Replace('CustSearch_AI', $DatabaseName), $encoding)
    & $sqlcmd -S $ServerInstance -d master -E -C -b -i $temporarySql
    if ($LASTEXITCODE -ne 0) { throw "Phase 16 canonical install failed with exit code $LASTEXITCODE." }
    & $sqlcmd -S $ServerInstance -d $DatabaseName -E -C -b -Q "SET NOCOUNT ON;IF(SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.15.0')<>1 THROW 55231,'Canonical V1.15.0 invalid',1;IF OBJECT_ID(N'dbo.OperationalSettings',N'U') IS NULL OR OBJECT_ID(N'dbo.WorkerLeases',N'U') IS NULL OR OBJECT_ID(N'dbo.OperationalRetention_Run',N'P') IS NULL THROW 55232,'Canonical Phase 16 objects missing',1;DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;SELECT DB_NAME() DatabaseName,N'PASS' Result;"
    if ($LASTEXITCODE -ne 0) { throw "Phase 16 canonical verification failed with exit code $LASTEXITCODE." }
}
catch {
    $exitCode = 1
    Write-Error $_
}
finally {
    if ([IO.File]::Exists($temporarySql)) { [IO.File]::Delete($temporarySql) }
    & $sqlcmd -S $ServerInstance -d master -E -C -b -Q "IF DB_ID(N'$DatabaseName') IS NOT NULL BEGIN ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$DatabaseName];END;IF DB_ID(N'$DatabaseName') IS NOT NULL THROW 55233,'Disposable database cleanup failed.',1;SELECT N'$DatabaseName' DatabaseName,N'DROPPED' Cleanup;"
    if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
}
exit $exitCode
