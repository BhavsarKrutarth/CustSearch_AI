[CmdletBinding()]
param(
    [string]$SqlServerInstance = 'KRUTARTH-BHAVSA',
    [switch]$InstallDependencies,
    [switch]$SkipDatabase
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = $PSScriptRoot
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) {
    $localDotnet
} else {
    (Get-Command dotnet -ErrorAction SilentlyContinue).Source
}
if (-not $dotnet) { throw '.NET SDK executable was not found.' }

function Invoke-Gate {
    param([Parameter(Mandatory)][string]$Name,[Parameter(Mandatory)][scriptblock]$Action)
    Write-Host "`n== $Name ==" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE." }
}

Push-Location $repositoryRoot
try {
    if (-not $SkipDatabase) {
        Invoke-Gate 'Canonical SQL fresh-install verification' {
            & '.\database\verify-canonical-fresh-install.ps1' -ServerInstance $SqlServerInstance
        }
    }

    Invoke-Gate '.NET restore' { & $dotnet restore '.\CustSearch_AI.sln' }
    Invoke-Gate '.NET Release build' { & $dotnet build '.\CustSearch_AI.sln' --configuration Release --no-restore }
    Invoke-Gate '.NET Release tests' { & $dotnet test '.\CustSearch_AI.sln' --configuration Release --no-build }

    if ($InstallDependencies) {
        Invoke-Gate 'Python development dependencies' { python -m pip install --disable-pip-version-check -r '.\src\CustSearch.AI\requirements-dev.txt' }
    }
    Invoke-Gate 'Python Ruff' { python -m ruff check '.\src\CustSearch.AI' '.\tests\CustSearch.AI.Tests' }
    $env:PYTHONPATH = Join-Path $repositoryRoot 'src\CustSearch.AI'
    Invoke-Gate 'Python pytest' { python -m pytest -q '.\tests\CustSearch.AI.Tests' }

    Push-Location '.\src\CustSearch.Admin'
    try {
        if ($InstallDependencies) { Invoke-Gate 'Angular npm ci' { npm ci --no-audit --no-fund } }
        Invoke-Gate 'Angular dependency audit' { npm audit --audit-level=high }
        Invoke-Gate 'Angular lint' { npm run lint }
        Invoke-Gate 'Angular unit tests' { npm run test:ci }
        Invoke-Gate 'Angular production build' { npm run build:production }
    }
    finally { Pop-Location }

    $publishedWebConfig = Join-Path $repositoryRoot 'src\CustSearch.Admin\dist\custsearch-admin\browser\web.config'
    if (-not (Test-Path -LiteralPath $publishedWebConfig)) { throw 'Angular production output is missing web.config.' }
    [xml](Get-Content -Raw -LiteralPath $publishedWebConfig) | Out-Null

    Push-Location '.\tests\CustSearch.Admin.E2E'
    try {
        if ($InstallDependencies) {
            Invoke-Gate 'Playwright npm ci' { npm ci --no-audit --no-fund }
            Invoke-Gate 'Playwright Chromium install' { npx playwright install chromium }
        }
        Invoke-Gate 'Playwright dependency audit' { npm audit --audit-level=high }
        Invoke-Gate 'Playwright regression' { npm test }
    }
    finally { Pop-Location }

    Write-Host "`nAll requested quality gates passed." -ForegroundColor Green
}
finally {
    Pop-Location
}
