# Renders the sample invoices through AutoCount's own Sales Invoice layout and checks the book is
# untouched. Pass a layout name to force one; omit it to take the book's default.
param([string]$Template = "")
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\reallayout.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:"$ac\AutoCount.MainEntry.dll" `
       /r:"$ac\AutoCount.Sales.dll" /r:"$ac\AutoCount.UI.dll" `
       /r:"$ac\DevExpress.XtraReports.v22.2.dll" /r:"$ac\DevExpress.XtraPrinting.v22.2.dll" `
       /r:"$ac\DevExpress.Printing.v22.2.Core.dll" /r:"$ac\DevExpress.Data.v22.2.dll" `
       /r:"$ac\DevExpress.Drawing.v22.2.dll" /r:"$ac\DevExpress.Utils.v22.2.dll" `
       /r:System.Data.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "$sp\RealLayoutCheck.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\reallayout.exe" $Template
exit $LASTEXITCODE
