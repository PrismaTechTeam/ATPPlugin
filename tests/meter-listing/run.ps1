# Summary Sales Invoice Meter Listing, checked against what was really billed.
#
# Read only. Builds the listing through ScpMeterListing.Build and asserts:
#   NET = current - previous - FOC;  charge = NET x rate less the rebate;
#   every contract closes with a .C COMBINE row whose grand total = meters + rental;
#   and the contract's grand total equals the invoices that stand in dbo.IV.
#
#   powershell -ExecutionPolicy Bypass -File tests\meter-listing\run.ps1

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$out  = Join-Path $env:TEMP 'atp-meterlisting'
if (-not (Test-Path $out)) { New-Item -ItemType Directory -Path $out | Out-Null }
$exe  = Join-Path $out 'meterlisting.exe'
$csc  = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$ac   = 'C:\Program Files\AutoCount\Accounting 2.2'
$dll  = 'C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll'

& $csc /nologo /target:exe /platform:x64 "/out:$exe" "/r:$dll" "/r:$ac\AutoCount.dll" `
    "/r:$ac\AutoCount.Accounting.dll" /r:System.Data.dll /r:System.Windows.Forms.dll `
    (Join-Path $here 'MeterListingCheck.cs')
if (-not $?) { Write-Host '   compile FAILED'; exit 1 }
& $exe
exit $LASTEXITCODE
