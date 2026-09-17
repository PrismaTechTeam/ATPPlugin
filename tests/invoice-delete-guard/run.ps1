# A month comes off newest first, or not at all.
#
# Checks the DB trigger that holds the rule inside AutoCount itself (the plugin's own delete is
# checked by ScpInvoiceDelete.LaterInvoices before it asks the question). Every delete here runs
# inside a transaction that is rolled back, so the book is left exactly as it was found.
#
#   powershell -ExecutionPolicy Bypass -File tests\invoice-delete-guard\run.ps1 [-Book AED_ATPTEST] [-ContractNo 'SC 000000032']

param(
    [string]$Book = 'AED_ATPTEST',
    [string]$ContractNo = 'SC 000000032'
)

$ErrorActionPreference = 'Stop'
$fail = 0
function Check([string]$what, [bool]$ok) {
    if ($ok) { Write-Host "   ok    $what" } else { Write-Host "   FAIL  $what"; $script:fail++ }
}
function Sql([string]$q) {
    & sqlcmd -S 'localhost,1433' -U sa -P 'rs6663' -d $Book -h -1 -W -Q "SET NOCOUNT ON; $q" 2>&1 | Out-String
}
function TryDelete([string]$docNoList) {
    Sql "BEGIN TRAN; DELETE FROM dbo.IV WHERE DocNo IN ($docNoList); PRINT 'ALLOWED'; IF @@TRANCOUNT > 0 ROLLBACK TRAN;"
}

Write-Host "=== invoice delete guard ($Book, $ContractNo) ==="

Check "the trigger is on dbo.IV" ((Sql "SELECT COUNT(*) FROM sys.triggers WHERE name = 'TR_zSCP2_IV_NoDeleteEarlierBilledMonth'").Trim() -eq '1')

# The contract's invoiced months, newest last.
$rows = (Sql @"
SELECT DISTINCT e.PeriodYear * 100 + e.PeriodMonth, RTRIM(e.InvoicedDocNo)
  FROM dbo.zSCP2_MeterEntry e
  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey
  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey
  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey
 WHERE c.ContractNo = '$ContractNo' AND e.InvoicedDocKey IS NOT NULL
 ORDER BY 1
"@) -split "`r?`n" | Where-Object { $_ -match '^\d{6}\s' }

$months = @{}
foreach ($r in $rows) {
    $p, $doc = ($r -split '\s+', 2)
    if (-not $months.ContainsKey($p)) { $months[$p] = @() }
    $months[$p] += $doc.Trim()
}
$keys = $months.Keys | Sort-Object

if ($keys.Count -lt 2) {
    Write-Host "   SKIP  $ContractNo has fewer than two invoiced months — nothing to order."
    exit 0
}
Write-Host ("   months invoiced: " + ($keys -join ', '))

$newest = $keys[-1]
foreach ($k in $keys) {
    $docs = "'" + ($months[$k] -join "','") + "'"
    $out = TryDelete $docs
    if ($k -eq $newest) {
        Check "the newest month ($k) deletes" ($out -match 'ALLOWED')
    } else {
        Check "an earlier month ($k) is refused" ($out -match 'cannot be deleted')
        Check "   and the refusal names a later month" ($out -match 'is already invoiced for')
    }
}

# Every month at once is the whole contract coming off — nothing is left measuring from a gap.
$all = "'" + (($months.Values | ForEach-Object { $_ }) -join "','") + "'"
Check "all months in one delete are allowed" ((TryDelete $all) -match 'ALLOWED')

# Nothing was actually removed.
$still = (Sql "SELECT COUNT(*) FROM dbo.IV WHERE DocNo IN ($all)").Trim()
Check "every invoice is still in the book ($still)" ([int]$still -eq (($months.Values | ForEach-Object { $_ }) | Measure-Object).Count)

if ($fail -eq 0) { Write-Host "   ALL OK"; exit 0 } else { Write-Host "   $fail FAILED"; exit 1 }
