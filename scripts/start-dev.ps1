<#
Starts backend (dotnet) and frontend (Vite) in separate PowerShell windows.
Usage: .\scripts\start-dev.ps1
#>
param()

function Ensure-CommandExists {
    param([string]$cmd)
    $which = Get-Command $cmd -ErrorAction SilentlyContinue
    if (-not $which) {
        Write-Error "Required command '$cmd' was not found in PATH."
        exit 1
    }
}

Ensure-CommandExists dotnet
Ensure-CommandExists npm

$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path "$repo\.."

$apiPath = Join-Path $root 'Mobile.Remote.Toolkit'
$apiProj = Join-Path $apiPath 'Mobile.Remote.Toolkit.Api.csproj'
$vuePath = Join-Path $root '..\Mobile.Remote.Toolkit.Vue'

Write-Output "Starting API in new window..."
Start-Process -FilePath pwsh -ArgumentList "-NoExit","-Command","Set-Location -LiteralPath '$apiPath'; dotnet run --project '$apiProj' -c Debug"

Start-Sleep -Milliseconds 500

Write-Output "Starting frontend (Vite) in new window..."
Start-Process -FilePath pwsh -ArgumentList "-NoExit","-Command","Set-Location -LiteralPath '$vuePath'; npm run dev:open"

Write-Output "Both processes started. Close the windows to stop them."
