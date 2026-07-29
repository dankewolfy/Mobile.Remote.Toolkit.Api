# Ejecutar UNA SOLA VEZ por máquina, desde una PowerShell abierta como Administrador
# (click derecho en PowerShell > "Ejecutar como administrador", luego correr este script).
#
# Crea dos Scheduled Tasks con privilegios altos, asociadas al usuario que las crea,
# sin ningún disparador automático (solo se ejecutan cuando algo las invoca explícitamente
# con "schtasks /run /tn <nombre>" o Start-ScheduledTask). La API de Mobile Remote Toolkit
# las dispara para iniciar/detener el mirror de iOS por USB (IosScreenCaptureTool) sin
# necesitar privilegios de administrador en el proceso de la API en sí.

$exePath = Join-Path $PSScriptRoot "IosScreenCaptureTool.exe"
$user = "$env:USERDOMAIN\$env:USERNAME"

$startAction = New-ScheduledTaskAction -Execute $exePath -Argument "--start-minimized"
$startPrincipal = New-ScheduledTaskPrincipal -UserId $user -RunLevel Highest -LogonType Interactive
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero)
Register-ScheduledTask -TaskName "MobileRemoteToolkit_IosMirror_Start" -Action $startAction -Principal $startPrincipal -Settings $settings -Force

$stopAction = New-ScheduledTaskAction -Execute "taskkill.exe" -Argument "/IM IosScreenCaptureTool.exe /F"
$stopPrincipal = New-ScheduledTaskPrincipal -UserId $user -RunLevel Highest -LogonType Interactive
Register-ScheduledTask -TaskName "MobileRemoteToolkit_IosMirror_Stop" -Action $stopAction -Principal $stopPrincipal -Settings $settings -Force

Write-Host ""
Write-Host "Tareas creadas:"
Get-ScheduledTask -TaskName "MobileRemoteToolkit_IosMirror_*" | Select-Object TaskName, State
