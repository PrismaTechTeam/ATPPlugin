<#
.SYNOPSIS
    Build + package + upload an ATPApi release to S3.

.DESCRIPTION
    Local release pipeline for the ATPApi webhook/API service. Builds ATPApi (Release)
    and ATPApiUpdater, stages into a clean folder, ZIPs it, computes SHA256, renames the
    ZIP with a hash prefix (so the URL is unguessable), generates a latest.json manifest,
    and uploads both to s3://prisma-atp-updates/atp/.

    After upload, trims old release ZIPs so only the newest $KeepCount remain.
    latest.json is never trimmed.

    NOTE: appsettings.json is EXCLUDED from the ZIP — the customer's existing config is
    preserved on update; a redacted appsettings.sample.json is shipped for reference only.

    This pipeline updates the API SERVICE only. The AutoCount plugin (.app) is shipped
    separately via the AutoCount Plug-in Manager (see build-and-install.bat / regenerate appp).

.EXAMPLE
    .\release.ps1 -Version 1.0.0

.EXAMPLE
    # Build locally but skip S3 upload (dry run)
    .\release.ps1 -Version 0.9.0-dev -NoUpload
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Bucket        = "prisma-atp-updates",
    [string]$Prefix        = "atp",
    [string]$Region        = "ap-southeast-5",
    [string]$Configuration = "Release",
    [int]   $KeepCount     = 3,
    [switch]$NoUpload
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }

$stageRoot  = Join-Path $PSScriptRoot "release-staging"
$outputRoot = Join-Path $PSScriptRoot "release-output"
$stage      = Join-Path $stageRoot "ATPApi-$Version"

Write-Host "=== ATPApi release $Version ===" -ForegroundColor Cyan

# -- 1. Clean stage ----------------------------------------------------------
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage      | Out-Null
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

# -- 2. Build ----------------------------------------------------------------
Write-Host "`n[1/7] Building ATPApi ($Configuration, v$Version)..." -ForegroundColor Yellow
& $dotnet build "ATPApi\ATPApi.csproj" -c $Configuration "/p:Version=$Version" -v minimal --nologo
if ($LASTEXITCODE -ne 0) { throw "ATPApi build failed" }

Write-Host "`n[2/7] Building ATPApiUpdater ($Configuration)..." -ForegroundColor Yellow
& $dotnet build "ATPApiUpdater\ATPApiUpdater.csproj" -c $Configuration -v minimal --nologo
if ($LASTEXITCODE -ne 0) { throw "ATPApiUpdater build failed" }

# -- 3. Stage ----------------------------------------------------------------
Write-Host "`n[3/7] Staging files..." -ForegroundColor Yellow
$apiBin     = Join-Path $PSScriptRoot "ATPApi\bin\$Configuration\net48"
$updaterBin = Join-Path $PSScriptRoot "ATPApiUpdater\bin\$Configuration\net48"

# Copy API output, minus noise. AutoCount.*.dll loads at runtime from the installed
# AutoCount dir via the AssemblyResolve hook (see Program.cs), so it isn't shipped.
$excludePatterns = @("*.pdb", "*.xml", "AutoCount.*.dll", "AutoCount.*.xml")
Get-ChildItem -Path $apiBin -Recurse -File | ForEach-Object {
    if ($_.Name -eq "appsettings.json") { return }
    if ($_.FullName -match '\\logs\\')   { return }
    foreach ($pat in $excludePatterns) {
        if ($_.Name -like $pat) { return }
    }
    $rel = $_.FullName.Substring($apiBin.Length).TrimStart('\')
    $dst = Join-Path $stage $rel
    New-Item -ItemType Directory -Path (Split-Path $dst) -Force | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $dst -Force
}

# Ship a redacted template config (first-install reference only; the updater never
# overwrites the customer's own appsettings.json).
$srcCfg = Join-Path $apiBin "appsettings.json"
if (Test-Path $srcCfg) {
    $cfg = Get-Content $srcCfg -Raw | ConvertFrom-Json
    $redact = "<SET_ME>"

    if ($cfg.PSObject.Properties['ApiKey'])   { $cfg.ApiKey = $redact }
    if ($cfg.Profiles) {
        foreach ($pn in $cfg.Profiles.PSObject.Properties.Name) {
            $p = $cfg.Profiles.$pn
            foreach ($f in 'Server','Database','SqlUser','SqlPassword','LoginUser','LoginPassword') {
                if ($p.PSObject.Properties[$f]) { $p.$f = $redact }
            }
        }
    }

    $sampleJson = $cfg | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText(
        (Join-Path $stage "appsettings.sample.json"),
        $sampleJson,
        (New-Object System.Text.UTF8Encoding $false))
}

# Bundle the updater exe so customers always get the latest updater.
Copy-Item (Join-Path $updaterBin "ATPApiUpdater.exe") $stage -Force

$stagedCount = (Get-ChildItem $stage -Recurse -File).Count
Write-Host "  staged $stagedCount files into $stage"

# -- 4. ZIP + SHA256 ---------------------------------------------------------
Write-Host "`n[4/7] Zipping + hashing..." -ForegroundColor Yellow
$tmpZip = Join-Path $outputRoot "ATPApi-$Version.zip"
if (Test-Path $tmpZip) { Remove-Item $tmpZip -Force }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $tmpZip -CompressionLevel Optimal

$hashFull  = (Get-FileHash $tmpZip -Algorithm SHA256).Hash.ToLower()
$hashShort = $hashFull.Substring(0, 16)
$finalName = "ATPApi-$Version-$hashShort.zip"
$finalZip  = Join-Path $outputRoot $finalName
if (Test-Path $finalZip) { Remove-Item $finalZip -Force }
Move-Item $tmpZip $finalZip -Force

$sizeBytes = (Get-Item $finalZip).Length
Write-Host ("  {0} ({1:N0} bytes)" -f $finalName, $sizeBytes)
Write-Host "  sha256: $hashFull"

# -- 5. Manifest -------------------------------------------------------------
Write-Host "`n[5/7] Writing manifest..." -ForegroundColor Yellow
$downloadUrl = "https://$Bucket.s3.$Region.amazonaws.com/$Prefix/$finalName"
$manifest = [ordered]@{
    version     = $Version
    releaseDate = (Get-Date -Format "yyyy-MM-dd")
    downloadUrl = $downloadUrl
    sha256      = $hashFull
    sizeBytes   = $sizeBytes
}
$manifestPath = Join-Path $outputRoot "latest.json"
$manifestJson = $manifest | ConvertTo-Json -Depth 3
# UTF-8 without BOM (PS 5.1's Set-Content -Encoding utf8 prepends a BOM that trips JSON parsers).
[System.IO.File]::WriteAllText($manifestPath, $manifestJson, (New-Object System.Text.UTF8Encoding $false))

# -- 6. Upload ---------------------------------------------------------------
if ($NoUpload) {
    Write-Host "`n[6/7] Skipping upload (-NoUpload)" -ForegroundColor DarkYellow
} else {
    Write-Host "`n[6/7] Uploading to s3://$Bucket/$Prefix/" -ForegroundColor Yellow
    aws s3 cp $finalZip "s3://$Bucket/$Prefix/$finalName" --no-progress
    if ($LASTEXITCODE -ne 0) { throw "S3 upload (zip) failed" }

    aws s3 cp $manifestPath "s3://$Bucket/$Prefix/latest.json" `
        --content-type "application/json" `
        --cache-control "no-cache, max-age=0" `
        --no-progress
    if ($LASTEXITCODE -ne 0) { throw "S3 upload (manifest) failed" }
    Write-Host "  uploaded"
}

# -- 7. Cleanup old versions -------------------------------------------------
if ($NoUpload) {
    Write-Host "`n[7/7] Skipping cleanup (-NoUpload)" -ForegroundColor DarkYellow
} else {
    Write-Host "`n[7/7] Trimming to last $KeepCount release ZIPs..." -ForegroundColor Yellow
    $listJson = aws s3api list-objects-v2 --bucket $Bucket --prefix "$Prefix/ATPApi-" `
        --query "Contents[?ends_with(Key, '.zip')].{Key:Key,Date:LastModified}" --output json
    if ($LASTEXITCODE -ne 0) { throw "S3 list failed" }

    $items = $listJson | ConvertFrom-Json
    if ($null -eq $items) { $items = @() }
    if ($items.PSObject.TypeNames[0] -notmatch '^System\.Object\[\]$') { $items = @($items) }

    $sorted = $items | Sort-Object { [datetime]$_.Date } -Descending
    $toDelete = @($sorted) | Select-Object -Skip $KeepCount
    if ($toDelete.Count -eq 0) {
        Write-Host "  nothing to delete ($($items.Count) ZIP(s) total, keeping all)"
    } else {
        foreach ($obj in $toDelete) {
            aws s3 rm "s3://$Bucket/$($obj.Key)"
            if ($LASTEXITCODE -eq 0) { Write-Host "  deleted: $($obj.Key)" }
        }
    }
}

# -- Summary -----------------------------------------------------------------
Write-Host "`n=== DONE ===" -ForegroundColor Green
Write-Host "Version:     $Version"
Write-Host "Local ZIP:   $finalZip"
Write-Host "Manifest:    $manifestPath"
Write-Host "SHA256:      $hashFull"
if (-not $NoUpload) {
    Write-Host "Download:    $downloadUrl"
    Write-Host "Manifest:    https://$Bucket.s3.$Region.amazonaws.com/$Prefix/latest.json"
}
