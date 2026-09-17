# The contract list's audit columns and the machine list's hidden date columns.
#
#   .\run.ps1        # AED_ATPTEST
#
# Read-only: both screens are opened off-screen, read and disposed. Nothing is saved.
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\contractcolumns.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" `
       /r:System.Data.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\contractcolumns.exe"
exit $LASTEXITCODE
