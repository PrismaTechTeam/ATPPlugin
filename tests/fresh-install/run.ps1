# Does the plug-in install on an account book that has never had it?
#
# Every book we develop against has had the plug-in for months, so a migration that reads a column a
# LATER migration creates never fails here -- the column has been in the table since the day it was
# first added. On a customer's new book it is the first thing to break, and it stops the plug-in
# loading at all. 1.5.0.0 shipped with exactly that fault (18/9). This is the test that would have
# caught it.
#
# Restores a copy of a plug-in-free book under a scratch name, runs the plug-in's own migrations on
# it twice (a fresh install, then a normal second load), and checks the book came out whole.
# The source book is only ever backed up COPY_ONLY; nothing in it is touched.
#
#   powershell -ExecutionPolicy Bypass -File tests\fresh-install\run.ps1 [-Source AED_ATPIMPORT0001]

param([string]$Source = 'AED_ATPIMPORT0001', [string]$Scratch = 'AED_ATPFRESH_TEST')

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sql  = { param($q) & sqlcmd -S 'localhost,1433' -U sa -P 'rs6663' -b -h -1 -W -Q $q }
$bak  = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\' + $Scratch + '_src.bak'
$data = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\'

Write-Host "=== fresh install: $Source -> $Scratch ==="
& $sql "BACKUP DATABASE [$Source] TO DISK = N'$bak' WITH COPY_ONLY, INIT, FORMAT" | Out-Null
$files = & sqlcmd -S 'localhost,1433' -U sa -P 'rs6663' -h -1 -W -s '|' -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = N'$bak'"
$moves = @()
foreach ($line in $files) {
    $f = $line -split '\|'
    if ($f.Count -lt 3 -or $f[0].Trim().Length -eq 0) { continue }
    $ext = if ($f[2].Trim() -eq 'L') { '_Log.ldf' } else { '_Data.mdf' }
    $moves += "MOVE N'" + $f[0].Trim() + "' TO N'" + $data + $Scratch + $ext + "'"
}
& $sql ("IF DB_ID('$Scratch') IS NOT NULL BEGIN ALTER DATABASE [$Scratch] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Scratch]; END") | Out-Null
& $sql ("RESTORE DATABASE [$Scratch] FROM DISK = N'$bak' WITH " + ($moves -join ', ') + ", RECOVERY") | Out-Null

$exe = Join-Path $env:TEMP 'atp-freshinstall.exe'
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe /platform:x64 "/out:$exe" `
    '/r:C:\Program Files\AutoCount\Accounting 2.2\AutoCount.dll' /r:System.Data.dll (Join-Path $here 'FreshInstall.cs')
if (-not $?) { Write-Host '   compile FAILED'; exit 1 }
& $exe $Scratch
$rc = $LASTEXITCODE

# The scratch book is ours alone; it goes once the answer is in.
& $sql ("IF DB_ID('$Scratch') IS NOT NULL BEGIN ALTER DATABASE [$Scratch] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Scratch]; END") | Out-Null
exit $rc
