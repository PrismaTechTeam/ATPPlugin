# Builds DEMO-01 .. DEMO-12 -- one contract per billing-format preset plus a legacy control -- and
# the copier models they need, in AED_ATPTEST. Re-runnable: only the DEMO-* rows are rebuilt.
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\seedcontracts.exe" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:System.Data.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\seedcontracts.exe"
exit $LASTEXITCODE
