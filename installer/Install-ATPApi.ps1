<#
.SYNOPSIS
    Install (or update) the ATPApi webhook/API as a Windows Service on a CUSTOMER PC.
    RUN AS ADMINISTRATOR (the Install-ATPApi.bat launcher elevates for you).

.DESCRIPTION
    This is a *deployment* installer - it does NOT build anything and needs neither the
    .NET SDK nor the source code. It obtains the already-built ATPApi binaries one of two
    ways, auto-detected in this order:

        1. -SourceDir <folder>            ... explicit folder of extracted release files
        2. ATPApi.exe next to this .ps1   ... "offline" package (binaries shipped alongside)
        3. .\payload\ATPApi.exe           ... binaries in a .\payload subfolder
        4. (none of the above)            ... DOWNLOAD the latest release from S3 and
                                              verify its SHA256 against latest.json

    Then it:
      - stops/removes any existing ATPApi service,
      - copies files into the install dir (your existing appsettings.json is PRESERVED),
      - on a FIRST install, creates appsettings.json from appsettings.sample.json and
        applies any -ApiKey / -Db* / -AutoCountPath values you pass,
      - registers the Topshelf service (install --localsystem --autostart) and starts it,
      - health-checks http://localhost:5007/api/ping.

    Updates after the first install are normally done from the AutoCount plugin's
    "About / Check for Updates" menu (which runs ATPApiUpdater.exe) - but re-running this
    installer also works and will never overwrite the customer's appsettings.json.

.PARAMETER ApiKey
    The X-API-Key PUMS must send. Required for the service to accept webhooks. If omitted
    on a first install, the sample's placeholder is kept and the service is NOT started
    until you edit appsettings.json.

.EXAMPLE
    # Online install - downloads latest from S3, prompts for config:
    powershell -ExecutionPolicy Bypass -File .\Install-ATPApi.ps1 -ApiKey "109izjiwjr14m" `
        -DbServer "localhost,1433" -Database "AED_LIVE" -SqlUser "sa" -SqlPassword "***" `
        -LoginUser "ADMIN" -LoginPassword "***"

.EXAMPLE
    # Offline install from a folder you copied to the PC:
    powershell -ExecutionPolicy Bypass -File .\Install-ATPApi.ps1 -SourceDir "D:\ATPApi-package"
#>
[CmdletBinding()]
param(
    [string]$InstallDir    = "C:\Program Files\ATPApi",
    [string]$ServiceName   = "ATPApi",
    [string]$ManifestUrl   = "https://prisma-atp-updates.s3.ap-southeast-5.amazonaws.com/atp/latest.json",
    [string]$SourceDir     = "",
    [string]$HealthUrl     = "http://localhost:5007/api/ping",

    # --- First-install config (written into appsettings.json; ignored on re-install) ---
    [string]$ApiKey        = "",
    [string]$DbServer      = "",
    [string]$Database      = "",
    [string]$SqlUser       = "",
    [string]$SqlPassword   = "",
    [string]$LoginUser     = "",
    [string]$LoginPassword = "",
    [string]$AutoCountPath = "",

    [switch]$NoStart,
    # Install the files + register the service but DO NOT start it; print a checklist of what to
    # configure in appsettings.json and the exact commands to start it afterwards.
    [switch]$ConfigureOnly
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Write-Step($n, $t) { Write-Host "`n[$n] $t" -ForegroundColor Yellow }
function Write-Ok($t)       { Write-Host "  $t" -ForegroundColor Green }
function Write-Warn2($t)    { Write-Host "  $t" -ForegroundColor DarkYellow }

# ---------------------------------------------------------------------------
# 0. Must be Administrator
# ---------------------------------------------------------------------------
$me = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $me.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)) {
    throw "This script must run as Administrator (service install + write to '$InstallDir'). Use Install-ATPApi.bat which elevates automatically."
}

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "   ATPApi service installer"               -ForegroundColor Cyan
Write-Host "   install dir : $InstallDir"              -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

$exe       = Join-Path $InstallDir "ATPApi.exe"
$cfgPath   = Join-Path $InstallDir "appsettings.json"
$firstTime = -not (Test-Path $cfgPath)
$tempStage = $null

# ---------------------------------------------------------------------------
# 1. Resolve the file source (offline folder OR download + verify)
# ---------------------------------------------------------------------------
Write-Step "1/6" "Locating release files..."

function Test-Payload($dir) { $dir -and (Test-Path (Join-Path $dir "ATPApi.exe")) }

$payloadDir = $null
if     ($SourceDir -and (Test-Payload $SourceDir))                       { $payloadDir = $SourceDir }
elseif (Test-Payload $PSScriptRoot)                                      { $payloadDir = $PSScriptRoot }
elseif (Test-Payload (Join-Path $PSScriptRoot "payload"))               { $payloadDir = (Join-Path $PSScriptRoot "payload") }

if ($payloadDir) {
    Write-Ok "using local files: $payloadDir"
} else {
    Write-Ok "no local files found - downloading latest release"
    Write-Host "  manifest: $ManifestUrl"
    $manifest = Invoke-RestMethod -Uri ($ManifestUrl + "?t=" + [guid]::NewGuid()) -TimeoutSec 30
    Write-Host ("  version : {0}  ({1})" -f $manifest.version, $manifest.releaseDate)

    $tempStage = Join-Path $env:TEMP ("ATPApi-install-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $tempStage -Force | Out-Null
    $zip = Join-Path $tempStage "ATPApi.zip"

    Write-Host "  downloading $($manifest.downloadUrl)"
    Invoke-WebRequest -Uri $manifest.downloadUrl -OutFile $zip -UseBasicParsing -TimeoutSec 300

    $actual = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
    if ($manifest.sha256 -and ($actual -ne $manifest.sha256.ToLower())) {
        throw "SHA256 mismatch! expected $($manifest.sha256) got $actual - download corrupt, aborting."
    }
    Write-Ok "sha256 verified"

    $extract = Join-Path $tempStage "extract"
    Expand-Archive -Path $zip -DestinationPath $extract -Force
    if (-not (Test-Payload $extract)) { throw "Downloaded package does not contain ATPApi.exe." }
    $payloadDir = $extract
}

# ---------------------------------------------------------------------------
# 2. Stop / remove any existing service
# ---------------------------------------------------------------------------
Write-Step "2/6" "Removing existing service (if any)..."
$existing = Get-Service $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if (Test-Path $exe) { & $exe stop 2>$null | Out-Null; & $exe uninstall 2>$null | Out-Null }
    else { & sc.exe stop $ServiceName | Out-Null; & sc.exe delete $ServiceName | Out-Null }
    Start-Sleep -Seconds 2
    Write-Ok "previous service removed"
} else {
    Write-Ok "none installed"
}

# ---------------------------------------------------------------------------
# 3. Copy files (preserve customer appsettings.json)
# ---------------------------------------------------------------------------
Write-Step "3/6" "Copying files to $InstallDir..."
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

$backupCfg = $null
if (-not $firstTime) {
    $backupCfg = Join-Path $env:TEMP ("atpapi-cfg-" + [guid]::NewGuid().ToString("N") + ".json")
    Copy-Item $cfgPath $backupCfg -Force
    Write-Ok "preserving existing appsettings.json"
}

# Copy everything from the payload into the install dir, EXCEPT appsettings.json (preserved /
# seeded separately). Iterate the payload's TOP-LEVEL items and Copy-Item -Recurse each one so the
# folder structure is preserved natively - no relative-path math (which previously mis-rooted files
# under a stray subfolder). Stray appsettings backups/logs from a dirty source are skipped too.
Get-ChildItem -Path $payloadDir -Force |
    Where-Object { $_.Name -ne 'appsettings.json' -and $_.Name -ne 'appsettings.json.bak' -and $_.Name -ne 'logs' } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $InstallDir -Recurse -Force }
if ($backupCfg) { Copy-Item $backupCfg $cfgPath -Force; Remove-Item $backupCfg -Force }
Write-Ok "files copied"

# ---------------------------------------------------------------------------
# 4. First-install config (from appsettings.sample.json) + apply overrides
# ---------------------------------------------------------------------------
Write-Step "4/6" "Configuring appsettings.json..."
if ($firstTime) {
    $sample = Join-Path $InstallDir "appsettings.sample.json"
    if (Test-Path $sample) { Copy-Item $sample $cfgPath -Force }
    else {
        # Fallback template if the package shipped no sample.
        @'
{
  "BaseUrl": "http://localhost:5007",
  "Environment": "prod",
  "ApiKey": "<SET_ME>",
  "AutoCountInstallPath": "C:\\Program Files\\AutoCount\\Accounting 2.2",
  "Cors": { "AllowedOrigin": "*" },
  "Logging": { "MinimumLevel": "Information", "FilePath": "logs/atpapi-.log", "FileRollingDays": 14 },
  "DefaultProfile": "atplugin",
  "Profiles": {
    "atplugin": {
      "Name": "ATPPlugin",
      "Server": "<SET_ME>", "Database": "<SET_ME>",
      "SqlUser": "<SET_ME>", "SqlPassword": "<SET_ME>",
      "LoginUser": "<SET_ME>", "LoginPassword": "<SET_ME>"
    }
  }
}
'@ | Set-Content -Path $cfgPath -Encoding ASCII
    }

    $cfg = Get-Content $cfgPath -Raw | ConvertFrom-Json
    if ($ApiKey)        { $cfg.ApiKey = $ApiKey }
    $pName = $cfg.DefaultProfile
    $p = $cfg.Profiles.$pName
    if ($DbServer)      { $p.Server        = $DbServer }
    if ($Database)      { $p.Database      = $Database }
    if ($SqlUser)       { $p.SqlUser       = $SqlUser }
    if ($SqlPassword)   { $p.SqlPassword   = $SqlPassword }
    if ($LoginUser)     { $p.LoginUser     = $LoginUser }
    if ($LoginPassword) { $p.LoginPassword = $LoginPassword }
    if ($AutoCountPath) {
        if ($cfg.PSObject.Properties['AutoCountInstallPath']) { $cfg.AutoCountInstallPath = $AutoCountPath }
        else { $cfg | Add-Member -NotePropertyName AutoCountInstallPath -NotePropertyValue $AutoCountPath }
    }
    $json = $cfg | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText($cfgPath, $json, (New-Object System.Text.UTF8Encoding $false))
    Write-Ok "created appsettings.json"
} else {
    Write-Ok "kept existing appsettings.json (no changes)"
}

# Does the config still have placeholders? If so we must NOT start (it would crash).
$needsEdit = (Get-Content $cfgPath -Raw) -match "<SET_ME>"
if ($needsEdit) {
    Write-Warn2 "appsettings.json still has <SET_ME> placeholders - edit it before starting."
    Write-Warn2 "  file: $cfgPath"
}

# ---------------------------------------------------------------------------
# 5. Install + start the service
# ---------------------------------------------------------------------------
Write-Step "5/6" "Installing service '$ServiceName'..."
& $exe install --localsystem --autostart
if ($LASTEXITCODE -ne 0) { throw "Service install failed (exit $LASTEXITCODE)." }
Write-Ok "service registered"

$skipStart = $NoStart -or $needsEdit -or $ConfigureOnly
if ($skipStart) {
    Write-Warn2 "Service registered but NOT started (configure first)."
} else {
    & $exe start
    Start-Sleep -Seconds 3
}

# ---------------------------------------------------------------------------
# 6. Health check
# ---------------------------------------------------------------------------
Write-Step "6/6" "Health check $HealthUrl ..."
$ok = $false
if (-not $skipStart) {
    for ($i = 0; $i -lt 20; $i++) {
        try {
            $r = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) { $ok = $true; Write-Ok "OK: $($r.Content)"; break }
        } catch { Start-Sleep -Seconds 2 }
    }
}

# clean temp download
if ($tempStage -and (Test-Path $tempStage)) { Remove-Item $tempStage -Recurse -Force -ErrorAction SilentlyContinue }

$svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
Write-Host "`n===========================================" -ForegroundColor Green
Write-Host "   INSTALL DONE" -ForegroundColor Green
Write-Host ("   service : {0}" -f ($(if ($svc) { $svc.Status } else { "NOT INSTALLED" })))
Write-Host ("   config  : {0}" -f $cfgPath)
Write-Host "===========================================" -ForegroundColor Green

if ($skipStart) {
    # -------- Configure-first: list what to set, then how to run --------
    Write-Host "`n-----------------------------------------------------------" -ForegroundColor Yellow
    Write-Host "  STEP 1 - CONFIGURE  (edit this file)" -ForegroundColor Yellow
    Write-Host "-----------------------------------------------------------" -ForegroundColor Yellow
    Write-Host "  $cfgPath`n"
    Write-Host "  Set these values (replace <SET_ME>):"
    Write-Host '    ApiKey                          : the X-API-Key PUMS sends   (e.g. 109izjiwjr14m)'
    Write-Host '    Profiles.atplugin.Server        : SQL server                 (e.g. localhost,1433)'
    Write-Host '    Profiles.atplugin.Database      : AutoCount account-book DB  (e.g. AED_ATPDEMO0003)'
    Write-Host '    Profiles.atplugin.SqlUser       : SQL login                  (e.g. sa)'
    Write-Host '    Profiles.atplugin.SqlPassword   : SQL password'
    Write-Host '    Profiles.atplugin.LoginUser     : AutoCount login            (e.g. ADMIN)'
    Write-Host '    Profiles.atplugin.LoginPassword : AutoCount password         (e.g. ADMIN)'
    Write-Host '    BaseUrl                         : leave http://localhost:5007 unless changing port'
    Write-Host '    AutoCountInstallPath (optional) : only if AutoCount is in a non-default folder'

    Write-Host "`n-----------------------------------------------------------" -ForegroundColor Cyan
    Write-Host "  STEP 2 - RUN  (after saving appsettings.json)" -ForegroundColor Cyan
    Write-Host "-----------------------------------------------------------" -ForegroundColor Cyan
    Write-Host "  Open PowerShell as Administrator and run:`n"
    Write-Host "    # start the service"            -ForegroundColor Gray
    Write-Host "    Start-Service $ServiceName"
    Write-Host "`n    # check it is up (expect ok:true)" -ForegroundColor Gray
    Write-Host "    Get-Service $ServiceName"
    Write-Host "    Invoke-RestMethod $HealthUrl -Headers @{ 'X-Api-Key' = '<your ApiKey>' }"
    Write-Host "`n    # restart after any future appsettings.json change" -ForegroundColor Gray
    Write-Host "    Restart-Service $ServiceName"
    Write-Host "`n  (Logs: $InstallDir\logs\   |   Windows Event Viewer > Application)`n"
}
elseif (-not $ok) {
    Write-Host "`nService started but is not answering /api/ping yet. Checklist:" -ForegroundColor Red
    Write-Host "  1. appsettings.json: ApiKey + DB (Server/Database/SqlUser/SqlPassword) + LoginUser/LoginPassword"
    Write-Host "  2. AutoCount must be installed on this PC (auto-detected; set AutoCountInstallPath if non-standard)."
    Write-Host "  3. Restart-Service $ServiceName    (then re-check $HealthUrl)"
    Write-Host "  4. Logs: $InstallDir\logs\  and Windows Event Viewer > Application"
    exit 1
}
else {
    Write-Host "`nService is running and healthy. Nothing more to do." -ForegroundColor Green
}
