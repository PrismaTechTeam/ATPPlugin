<#
.SYNOPSIS
    Stop + remove the ATPApi Windows Service from a customer PC. RUN AS ADMINISTRATOR
    (the Uninstall-ATPApi.bat launcher elevates for you).

.DESCRIPTION
    Stops and unregisters the ATPApi service (Topshelf 'stop' + 'uninstall', falling back
    to sc.exe). By default the install folder is LEFT IN PLACE so appsettings.json and logs
    survive a later reinstall. Pass -Purge to delete the whole install dir as well.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Uninstall-ATPApi.ps1
.EXAMPLE
    # Also delete C:\Program Files\ATPApi (config + logs included):
    powershell -ExecutionPolicy Bypass -File .\Uninstall-ATPApi.ps1 -Purge
#>
[CmdletBinding()]
param(
    [string]$InstallDir  = "C:\Program Files\ATPApi",
    [string]$ServiceName = "ATPApi",
    [switch]$Purge
)

$ErrorActionPreference = "Continue"

$me = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $me.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)) {
    throw "This script must run as Administrator. Use Uninstall-ATPApi.bat which elevates automatically."
}

Write-Host "=== ATPApi uninstall ===" -ForegroundColor Cyan
$exe = Join-Path $InstallDir "ATPApi.exe"

if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "  stopping + removing service '$ServiceName'..." -ForegroundColor Yellow
    if (Test-Path $exe) { & $exe stop 2>$null | Out-Null; & $exe uninstall 2>$null | Out-Null }
    else { & sc.exe stop $ServiceName | Out-Null; & sc.exe delete $ServiceName | Out-Null }
    Start-Sleep -Seconds 2
    Write-Host "  service removed" -ForegroundColor Green
} else {
    Write-Host "  service '$ServiceName' is not installed" -ForegroundColor DarkYellow
}

if ($Purge) {
    if (Test-Path $InstallDir) {
        Write-Host "  deleting $InstallDir ..." -ForegroundColor Yellow
        Remove-Item $InstallDir -Recurse -Force -ErrorAction SilentlyContinue
        if (Test-Path $InstallDir) { Write-Host "  WARNING: some files could not be deleted (in use?)." -ForegroundColor Red }
        else { Write-Host "  install dir deleted" -ForegroundColor Green }
    }
} else {
    Write-Host "  install dir left in place: $InstallDir" -ForegroundColor DarkYellow
    Write-Host "  (run with -Purge to delete config + logs too)"
}

Write-Host "`n=== DONE ===" -ForegroundColor Green
