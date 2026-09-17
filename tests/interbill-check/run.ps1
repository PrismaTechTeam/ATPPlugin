# Drives the inter-billing module end to end against two REAL account books.
#
#   Book A  -BookA   AED_ATPTEST    the parent -- owns the machines, takes the readings
#   Book B  -BookB   AED_ASNDUMMY   the subsidiary -- has the end customer, issues the bill
#
# The defaults are the direction the product is actually used in. They were the other way round at
# first, which meant a plain `run.ps1` reset and rewrote the very book somebody was demonstrating
# from. A test that damages the thing it is testing is worse than no test.
#
# Setting up a parent book from scratch: create it in AutoCount (Manage Account Book), then
#
#   .\install-into-book.ps1 -Database <book>
#   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d <book> -I -i seed-charge-items.sql
#   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d <book> -I -i ..\..\ServiceContractPhotocopier\SQL_Seed_GLMast_Debtors.sql
#   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d <book> -I -i ..\..\ServiceContractPhotocopier\SQL_Seed_Debtor.sql
#   sqlcmd -S "localhost,1433" -U sa -P "rs6663" -d <book> -I -i seed-parent-book.sql
#
# Every run resets both books first, so it can be run as often as you like and the counts it checks
# always mean the same thing. -Mode clean resets and stops.
param([string]$Mode = "", [string]$BookA = "AED_ATPTEST", [string]$BookB = "AED_ASNDUMMY")
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\interbillcheck.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:System.Data.dll "$sp\Program.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
# An EMPTY string is dropped on its way to a native exe, which silently shifted the book name
# into the mode argument and ran the whole suite against the default book while reporting
# success. Send a word either way so the positions cannot move.
if ([string]::IsNullOrWhiteSpace($Mode)) { $Mode = "run" }
& "$sp\interbillcheck.exe" $Mode $BookA $BookB
exit $LASTEXITCODE
