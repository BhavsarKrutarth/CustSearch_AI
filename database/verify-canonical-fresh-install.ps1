[CmdletBinding()]
param(
    [string]$ServerInstance = 'KRUTARTH-BHAVSA',
    [string]$DatabaseName = ('CustSearch_AI_Verify_' + (Get-Date -Format 'yyyyMMddHHmmss') + '_' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
)

$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^CustSearch_AI_Verify_[A-Za-z0-9_]{1,70}$') {
    throw 'DatabaseName must be a disposable CustSearch_AI_Verify_* name.'
}

$sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source
$databaseRoot = Split-Path -Parent $PSCommandPath
$canonicalPath = Join-Path $databaseRoot 'CustSearchAi.sql'
$temporarySql = Join-Path ([System.IO.Path]::GetTempPath()) ('custsearch-canonical-' + [Guid]::NewGuid().ToString('N') + '.sql')
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$exitCode = 0

try {
    & $sqlcmd -S $ServerInstance -d master -E -C -b -Q "IF DB_ID(N'$DatabaseName') IS NOT NULL THROW 55300,'Disposable validation database already exists.',1;"
    if ($LASTEXITCODE -ne 0) { throw "Preflight failed with exit code $LASTEXITCODE." }

    $canonical = [System.IO.File]::ReadAllText($canonicalPath)
    if ($canonical -notmatch "VersionNumber=N'V1\.16\.0'") { throw 'Canonical SQL does not contain V1.16.0.' }
    $isolated = $canonical.Replace('CustSearch_AI', $DatabaseName)
    [System.IO.File]::WriteAllText($temporarySql, $isolated, $utf8NoBom)

    & $sqlcmd -S $ServerInstance -d master -E -C -b -i $temporarySql
    if ($LASTEXITCODE -ne 0) { throw "Canonical install failed with exit code $LASTEXITCODE." }

    $verification = @"
SET NOCOUNT ON;
IF DB_NAME()<>N'$DatabaseName' THROW 55301,'Validation connected to the wrong database.',1;
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions)<>17 THROW 55302,'Canonical version count is invalid.',1;
IF (SELECT COUNT(*) FROM dbo.DatabaseVersions WHERE VersionNumber=N'V1.16.0')<>1 THROW 55303,'Canonical V1.16.0 is invalid.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>75 THROW 55304,'Canonical table count is invalid.',1;
IF (SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped=0)<>75 THROW 55305,'Canonical procedure count is invalid.',1;
IF OBJECT_ID(N'dbo.SecurityObservation_Ingest',N'P') IS NULL THROW 55306,'Canonical security ingestion procedure is missing.',1;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
SELECT DB_NAME() DatabaseName,75 TableCount,75 ProcedureCount,17 VersionCount,N'PASS' Result;
"@
    & $sqlcmd -S $ServerInstance -d $DatabaseName -E -C -b -Q $verification
    if ($LASTEXITCODE -ne 0) { throw "Canonical verification failed with exit code $LASTEXITCODE." }
}
catch {
    $exitCode = 1
    Write-Error $_
}
finally {
    if ([System.IO.File]::Exists($temporarySql)) { [System.IO.File]::Delete($temporarySql) }
    $drop = "IF DB_ID(N'$DatabaseName') IS NOT NULL BEGIN ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DatabaseName]; END; IF DB_ID(N'$DatabaseName') IS NOT NULL THROW 55307,'Disposable validation database cleanup failed.',1; SELECT N'$DatabaseName' DatabaseName,N'DROPPED' Cleanup;"
    & $sqlcmd -S $ServerInstance -d master -E -C -b -Q $drop
    if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
}

exit $exitCode
