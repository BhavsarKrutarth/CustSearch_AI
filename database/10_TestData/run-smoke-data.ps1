[CmdletBinding()]
param(
    [string]$ServerInstance = 'localhost',
    [string]$DatabaseName = 'CustSearch_AI'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^[A-Za-z0-9_]+$') { throw 'DatabaseName contains unsupported characters.' }
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw 'sqlcmd is required.' }
$password = $env:CUSTSEARCH_SMOKE_PASSWORD
if ([string]::IsNullOrWhiteSpace($password) -or $password.Length -lt 12) {
    throw 'Set CUSTSEARCH_SMOKE_PASSWORD to a local value of at least 12 characters. It is never written to Git.'
}

# Produce the ASP.NET Core Identity V3 PBKDF2-SHA512 format used by PasswordHasher<UserAccount>.
$iterations = 100000
$salt = [byte[]]::new(16)
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $random.GetBytes($salt) } finally { $random.Dispose() }
$derive = [Security.Cryptography.Rfc2898DeriveBytes]::new(
    $password,
    $salt,
    $iterations,
    [Security.Cryptography.HashAlgorithmName]::SHA512)
$subkey = $derive.GetBytes(32)
$payload = [byte[]]::new(61)
$payload[0] = 1
function Set-NetworkInt([byte[]]$Buffer, [int]$Offset, [uint32]$Value) {
    $Buffer[$Offset] = ($Value -shr 24) -band 255
    $Buffer[$Offset + 1] = ($Value -shr 16) -band 255
    $Buffer[$Offset + 2] = ($Value -shr 8) -band 255
    $Buffer[$Offset + 3] = $Value -band 255
}
Set-NetworkInt $payload 1 2
Set-NetworkInt $payload 5 $iterations
Set-NetworkInt $payload 9 $salt.Length
[Array]::Copy($salt,0,$payload,13,$salt.Length)
[Array]::Copy($subkey,0,$payload,29,$subkey.Length)
$hash = [Convert]::ToBase64String($payload)

$scriptPath = Join-Path $PSScriptRoot 'AllPhases_SmokeData.sql'
& sqlcmd -S $ServerInstance -d $DatabaseName -E -C -b -r 1 -v "SmokePasswordHash=`"$hash`"" -i $scriptPath
if ($LASTEXITCODE -ne 0) { throw "Smoke data failed with exit code $LASTEXITCODE." }
