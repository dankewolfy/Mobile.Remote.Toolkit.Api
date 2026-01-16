<#
publish-exe.ps1

Builds the Vue frontend and publishes the ASP.NET API as a single-file executable.

Usage:
  .\publish-exe.ps1 [-Runtime win-x64] [-Configuration Release] [-OutputDir <path>] [-Zip]

Requirements: `dotnet`, `node`, `npm` in PATH.
#>

[CmdletBinding()]
param(
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release',
    [string]$OutputDir = '',
    [switch]$Zip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Ensure-CommandExists {
    param([string]$cmd)
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Error "Required command '$cmd' was not found in PATH."
        exit 2
    }
}

Ensure-CommandExists dotnet
Ensure-CommandExists npm
Ensure-CommandExists node

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path "$scriptDir\.."

$apiDir = Join-Path $repoRoot 'Mobile.Remote.Toolkit'
$apiProj = Join-Path $apiDir 'Mobile.Remote.Toolkit.Api.csproj'

# Vue project is sibling to this repo folder (one level up)
$vueDir = Resolve-Path (Join-Path $repoRoot '..\Mobile.Remote.Toolkit.Vue')

if (-not (Test-Path $apiProj)) {
    Write-Error "API project not found at $apiProj"
    exit 3
}

if (-not (Test-Path $vueDir)) {
    Write-Warning "Vue project directory not found at $vueDir. Skipping frontend build."
    $buildFrontend = $false
} else {
    $buildFrontend = $true
}

if (-not $OutputDir) {
    $OutputDir = Join-Path $apiDir 'publish'
}

Write-Host "Publishing with Runtime=$Runtime Configuration=$Configuration Output=$OutputDir" -ForegroundColor Cyan

if ($buildFrontend) {
    Write-Host "Building Vue frontend in: $vueDir" -ForegroundColor Green
    Push-Location $vueDir
    try {
        $hasPackageLock = Test-Path 'package-lock.json'
        $hasPackageJson = Test-Path 'package.json'
        if ($hasPackageLock -or $hasPackageJson) {
            Write-Host 'Installing frontend dependencies (npm ci)...'
            npm ci --silent
        }
        Write-Host 'Running frontend build (npm run build)...'
        npm run build --silent
    }
    catch {
        Write-Error "Frontend build failed: $_"
        Pop-Location
        exit 4
    }
    finally {
        Pop-Location
    }
}

# Ensure output directory
if (Test-Path $OutputDir) {
    Write-Host "Cleaning existing output directory: $OutputDir" -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $OutputDir | Out-Null

Write-Host 'Running dotnet publish...' -ForegroundColor Green

$publishArgs = @(
    'publish',
    "`"$apiProj`"",
    '-c', $Configuration,
    '-r', $Runtime,
    '--self-contained', 'true',
    '/p:PublishSingleFile=true',
    '/p:PublishTrimmed=false',
    '/p:PublishReadyToRun=true',
    '-o', "`"$OutputDir`""
)

$p = Start-Process -FilePath dotnet -ArgumentList $publishArgs -NoNewWindow -Wait -PassThru
if ($p.ExitCode -ne 0) {
    Write-Error "dotnet publish failed with exit code $($p.ExitCode)"
    exit 5
}

Write-Host 'Publish finished.' -ForegroundColor Cyan

# Find main executable (largest .exe file) for convenience
$exeFiles = Get-ChildItem -Path $OutputDir -Filter *.exe -File -Recurse | Sort-Object Length -Descending
if ($exeFiles.Length -gt 0) {
    $mainExe = $exeFiles[0].FullName
    Write-Host "Main executable: $mainExe" -ForegroundColor Yellow
} else {
    Write-Warning 'No .exe file was found in the publish output.'
}

if ($Zip.IsPresent) {
    $zipPath = Join-Path (Split-Path $OutputDir -Parent) ((Split-Path $OutputDir -Leaf) + '.zip')
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Write-Host "Creating zip: $zipPath" -ForegroundColor Green
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($OutputDir, $zipPath)
    Write-Host "Created: $zipPath" -ForegroundColor Cyan
}

Write-Host "Done. Output in: $OutputDir" -ForegroundColor Green

exit 0
