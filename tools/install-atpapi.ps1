<#
.SYNOPSIS
    Build + install the ATPApi webhook/API as a Windows Service.  RUN AS ADMINISTRATOR.

.DESCRIPTION
    1. Builds ATPApi + ATPApiUpdater (Release).
    2. Stops/uninstalls any existing ATPApi service.
    3. Copies the build output (+ ATPApiUpdater.exe) into the install dir.
       Your existing appsettings.json is PRESERVED on re-install; on a first install the
       build's appsettings.json is copied (edit it before/after — DB profile + ApiKey).
    4. Registers the service (Topshelf: install --localsystem --autostart) and starts it.
    5. Health-checks http://localhost:5007/api/ping.

.EXAMPLE
    # From an elevated PowerShell:
    powershell -ExecutionPolicy Bypass -File .\tools\install-atpapi.ps1
#>
[CmdletBinding()]
param(
    [string]$InstallDir    = "C:\Program Files\ATPApi",
    [string]$Configuration = "Release",
    [string]$HealthUrl     = "http://localhost:5007/api/ping"
)

$ErrorActionPreference = "Stop"

# must be admin
$id = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not (New-Object Security.Principal.WindowsPrincipal $id).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)) {
    throw "This script must be run as Administrator (service install + write to Program Files)."
}

$root   = Split-Path $PSScriptRoot -Parent
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$exe = Join-Path $InstallDir "ATPApi.exe"

Write-Host "=== ATPApi install ===" -ForegroundColor Cyan
Write-Host "  install dir = $InstallDir"

# -- 1. Build ---------------------------------------------------------------
Write-Host "`n[1/5] Building (Release)..." -ForegroundColor Yellow
& $dotnet build "$root\ATPApi\ATPApi.csproj"               -c $Configuration -v minimal --nologo
if ($LASTEXITCODE -ne 0) { throw "ATPApi build failed" }
& $dotnet build "$root\ATPApiUpdater\ATPApiUpdater.csproj" -c $Configuration -v minimal --nologo
if ($LASTEXITCODE -ne 0) { throw "ATPApiUpdater build failed" }

$apiBin = Join-Path $root "ATPApi\bin\$Configuration\net48"
$updBin = Join-Path $root "ATPApiUpdater\bin\$Configuration\net48"

# -- 2. Stop / uninstall existing -------------------------------------------
if (Get-Service ATPApi -ErrorAction SilentlyContinue) {
    Write-Host "`n[2/5] Removing existing ATPApi service..." -ForegroundColor Yellow
    if (Test-Path $exe) { & $exe stop 2>$null; & $exe uninstall 2>$null }
    else { sc.exe stop ATPApi | Out-Null; sc.exe delete ATPApi | Out-Null }
    Start-Sleep -Seconds 2
} else {
    Write-Host "`n[2/5] No existing service" -ForegroundColor Yellow
}

# -- 3. Copy files (preserve existing appsettings.json) ----------------------
Write-Host "`n[3/5] Copying to $InstallDir..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
$preserveCfg = Join-Path $InstallDir "appsettings.json"
$backupCfg = $null
if (Test-Path $preserveCfg) {
    $backupCfg = Join-Path $env:TEMP ("atpapi-appsettings-" + [guid]::NewGuid().ToString("N") + ".json")
    Copy-Item $preserveCfg $backupCfg -Force
    Write-Host "  preserving existing appsettings.json"
}
Copy-Item (Join-Path $apiBin "*") $InstallDir -Recurse -Force
Copy-Item (Join-Path $updBin "ATPApiUpdater.exe") $InstallDir -Force
if ($backupCfg) { Copy-Item $backupCfg $preserveCfg -Force; Remove-Item $backupCfg -Force }

# -- 4. Install + start ------------------------------------------------------
Write-Host "`n[4/5] Installing + starting service..." -ForegroundColor Yellow
& $exe install --localsystem --autostart
if ($LASTEXITCODE -ne 0) { throw "Service install failed (exit $LASTEXITCODE)" }
& $exe start
Start-Sleep -Seconds 3

# -- 5. Health check ---------------------------------------------------------
Write-Host "`n[5/5] Health check $HealthUrl ..." -ForegroundColor Yellow
$ok = $false
for ($i = 0; $i -lt 20; $i++) {
    try {
        $r = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 5
        if ($r.StatusCode -eq 200) { Write-Host "  OK: $($r.Content)" -ForegroundColor Green; $ok = $true; break }
    } catch { Start-Sleep -Seconds 2 }
}

Write-Host "`n=== DONE ===" -ForegroundColor Green
Write-Host ("Service: " + ((Get-Service ATPApi -ErrorAction SilentlyContinue | Select-Object -Expand Status)))
if (-not $ok) {
    Write-Host "Health check did NOT pass. Check:" -ForegroundColor Red
    Write-Host "  - $InstallDir\appsettings.json (DB profile reachable? ApiKey set?)"
    Write-Host "  - C:\ProgramData\ATPApi\updater.log and the service's own logs"
    Write-Host "  - AutoCount install path (set \"AutoCountInstallPath\" in appsettings.json if non-standard)"
}
