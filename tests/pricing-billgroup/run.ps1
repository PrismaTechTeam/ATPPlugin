# Meters & Pricing: "What the invoice prints" shows one invoice per bill group.
#   .\run.ps1
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\pricingbillgroup.exe" /r:"$dll" /r:"$ac\AutoCount.dll" `
       /r:System.Data.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\pricingbillgroup.exe"
exit $LASTEXITCODE
