<#
build-electron.ps1

Automates: build Vue, publish API EXE, prepare Electron app and run electron-builder to create Windows installer (NSIS).

Usage: .\build-electron.ps1 [-Runtime win-x64] [-Configuration Release]

Requirements: node, npm, dotnet available in PATH.
#>

[CmdletBinding()]
param(
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Ensure-CommandExists { param([string]$c) if (-not (Get-Command $c -ErrorAction SilentlyContinue)) { Write-Error "Command $c not found"; exit 2 } }

Ensure-CommandExists npm
Ensure-CommandExists node
Ensure-CommandExists dotnet

$root = Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')

$vueDir = Join-Path $root '..\Mobile.Remote.Toolkit.Vue'
$apiScript = Join-Path $root 'scripts\publish-exe.ps1'
$electronDir = Join-Path $root 'Mobile.Remote.Toolkit.Electron'

Write-Host "Building Vue app..." -ForegroundColor Cyan
Push-Location $vueDir
try { npm ci --silent; npm run build --silent } catch { Pop-Location; Write-Error "Vue build failed: $_"; exit 3 } finally { Pop-Location }

Write-Host "Publishing API exe (single-file)..." -ForegroundColor Cyan
if (-not (Test-Path $apiScript)) { Write-Error "Missing $apiScript"; exit 4 }
& powershell -NoProfile -ExecutionPolicy Bypass -File $apiScript -Runtime $Runtime -Configuration $Configuration -OutputDir (Join-Path $root 'Mobile.Remote.Toolkit\publish')

Write-Host "Preparing Electron app folder..." -ForegroundColor Cyan
# copy built Vue into electron app folder
$electronAppDist = Join-Path $electronDir 'app\dist'
if (Test-Path $electronAppDist) { Remove-Item $electronAppDist -Recurse -Force }
New-Item -ItemType Directory -Path $electronAppDist -Force | Out-Null
Copy-Item -Path (Join-Path $vueDir 'dist\*') -Destination $electronAppDist -Recurse -Force

Write-Host "Installing Electron dependencies..." -ForegroundColor Cyan
Push-Location $electronDir
try {
    if (-not (Test-Path 'package-lock.json')) { npm install --no-audit --no-fund --silent } else { npm ci --silent }
} catch { Pop-Location; Write-Error "Electron npm install failed: $_"; exit 5 } finally { Pop-Location }

Write-Host "Building Electron installer (this may take a while)..." -ForegroundColor Cyan
Push-Location $electronDir
try {
    npm run dist --silent
} catch { Pop-Location; Write-Error "Electron build failed: $_"; exit 6 } finally { Pop-Location }

Write-Host "Electron build finished. Check $electronDir\dist for installers." -ForegroundColor Green

exit 0
