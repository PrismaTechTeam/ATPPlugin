# What a credit note gives back, against what the invoice actually charged.
#
#   .\run.ps1                # AED_ATPTEST
#   .\run.ps1 AED_ASNDUMMY
#
# Read-only: it re-runs the billing engine over the log and compares, it writes nothing.
param([string]$Book = "AED_ATPTEST")
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\cncredit.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:System.Data.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\cncredit.exe" $Book
exit $LASTEXITCODE
