<#
.SYNOPSIS
    Stop + uninstall the ATPApi Windows Service.  RUN AS ADMINISTRATOR.
    Leaves the install dir + appsettings.json in place (delete manually if desired).
#>
[CmdletBinding()]
param([string]$InstallDir = "C:\Program Files\ATPApi")

$ErrorActionPreference = "Continue"
$id = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not (New-Object Security.Principal.WindowsPrincipal $id).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)) {
    throw "This script must be run as Administrator."
}

$exe = Join-Path $InstallDir "ATPApi.exe"
if (Get-Service ATPApi -ErrorAction SilentlyContinue) {
    if (Test-Path $exe) { & $exe stop; & $exe uninstall }
    else { sc.exe stop ATPApi | Out-Null; sc.exe delete ATPApi | Out-Null }
    Write-Host "ATPApi service removed." -ForegroundColor Green
} else {
    Write-Host "ATPApi service not installed." -ForegroundColor Yellow
}
