# What the Meters & Pricing footer says about how many invoices a contract sends.
#
#   .\run.ps1                                   # AED_ATPTEST / DEMO-3G
#   .\run.ps1 -Book AED_ATPTEST -Contract DEMO-PL
#
# Read-only: it opens the dialog off-screen, reads the label and disposes it. Nothing is saved.
param([string]$Book = "AED_ATPTEST", [string]$Contract = "DEMO-3G")
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
$dx  = "$ac\DevExpress.XtraEditors.v22.2.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\billingsetupsummary.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:"$dx" `
       /r:System.Data.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\billingsetupsummary.exe" $Book $Contract
$code = $LASTEXITCODE

# Does the Minimum / waive dialog fit inside its own box? It has been cut off three times and
# never by anything a build would catch, so it is measured here -- with the window really shown,
# because Control.Visible answers "no" about everything on a form that was never displayed.
& $csc /nologo /target:exe /platform:x64 /out:"$sp\dialogfit.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:"$dx" `
       /r:"$ac\DevExpress.Utils.v22.2.dll" `
       /r:System.Data.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "$sp\Dialog.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\dialogfit.exe"
if ($LASTEXITCODE -ne 0) { $code = $LASTEXITCODE }
# And the Meter Reading screen itself: the day strip wraps when a book bills on many days, and
# the box has to grow with it. Same reason as the dialog -- nothing in a build notices a form
# whose controls are stacked on top of each other.
& $csc /nologo /target:exe /platform:x64 /out:"$sp\screenfit.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:"$dx" `
       /r:"$ac\DevExpress.Utils.v22.2.dll" `
       /r:System.Data.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "$sp\Screen.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\screenfit.exe" $Book
if ($LASTEXITCODE -ne 0) { $code = $LASTEXITCODE }

exit $code
