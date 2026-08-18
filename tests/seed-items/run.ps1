# Creates the copier models the line-merging scenarios use, in AED_ATPTEST. Re-runnable.
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\seeditems.exe" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:System.Data.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\seeditems.exe"
exit $LASTEXITCODE
