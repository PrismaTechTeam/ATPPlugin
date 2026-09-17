# What the Meter Invoice Run shows for one day: the invoices the data layer folds the rows into,
# then the form opened off-screen (day strip, summary, grid, no overlaps).
#
#   .\run.ps1                                   # AED_ATPTEST, Sep 2026, the 1st
#   .\run.ps1 -Book AED_ATPTEST -Year 2026 -Month 9 -Day 7
#
# Read-only: keys nothing, generates nothing.
param([string]$Book = "AED_ATPTEST", [int]$Year = 2026, [int]$Month = 9, [int]$Day = 1)
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\invoicerun.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" `
       /r:System.Data.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\invoicerun.exe" $Book $Year $Month $Day
exit $LASTEXITCODE
