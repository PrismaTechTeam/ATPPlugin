# The Inter-Billing board against two real books.
#
#   .\run.ps1 -Mode setup    # seed HQ (AED_ASNDUMMY), connect AED_ATPTEST to it, take 4 contracts, HQ adds a machine
#   .\run.ps1                # check: every board state, the jobs, the reading write, the screen
#   .\run.ps1 -Mode clean    # remove what setup made (refuses contracts that already have invoices)
param([string]$Mode = "check")
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
if ($Mode -eq "setup") {
    & sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ASNDUMMY -I -b -i "$sp\..\interbill-check\seed-parent-book.sql"
    if ($LASTEXITCODE -ne 0) { exit 1 }
    # Local taken contracts point at HQ keys; clear them before HQ's contracts are recreated.
    & "$sp\interbillboard.exe" clean
    & sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d AED_ASNDUMMY -I -b -i "$sp\seed-board-hq.sql"
    if ($LASTEXITCODE -ne 0) { exit 1 }
}
& $csc /nologo /target:exe /platform:x64 /out:"$sp\interbillboard.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" `
       /r:System.Data.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\interbillboard.exe" $Mode
exit $LASTEXITCODE
