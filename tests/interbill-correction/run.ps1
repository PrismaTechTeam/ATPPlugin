# Does a reading the PARENT has corrected reach the book that bills the end customer?
#
#   .\run.ps1                                  # AED_ATPTEST / DEMO-PL2
#   .\run.ps1 -Book AED_ATPTEST -Contract DEMO-PL
#
# It writes ONE marked row into the parent's zSCP_MeterTrans, checks what the reader hands over,
# and deletes that row again by its marker. It touches no other book and no other row -- in
# particular it does NOT reset the subsidiary the way interbill-check does, so it is safe to run
# while a demo book is set up.
param([string]$Book = "AED_ATPTEST", [string]$Contract = "DEMO-PL2")
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\interbillcorrection.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:System.Data.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\interbillcorrection.exe" $Book $Contract
exit $LASTEXITCODE
