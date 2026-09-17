# Builds the inter-billing screens once, off-screen, and reports what throws.
#
#   .\smoke-forms.ps1                 # against AED_ATPTEST
#   .\smoke-forms.ps1 -Book AED_ASNDUMMY
#
# A designer or Load-time fault survives a green compile and a green engine harness -- it waits for
# somebody to click the menu item. This clicks it first. No window is shown.
param([string]$Book = "AED_ATPTEST")
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\smokeforms.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" `
       /r:"$ac\DevExpress.Utils.v22.2.dll" /r:"$ac\DevExpress.Data.v22.2.dll" `
       /r:"$ac\DevExpress.XtraEditors.v22.2.dll" /r:"$ac\DevExpress.XtraGrid.v22.2.dll" `
       /r:"$ac\DevExpress.XtraBars.v22.2.dll" /r:"$ac\DevExpress.XtraLayout.v22.2.dll" `
       /r:System.Data.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "$sp\SmokeForms.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\smokeforms.exe" $Book
exit $LASTEXITCODE
