# Deletes A/R invoices from a book through AutoCount's own InvoiceCommand.
#
#   .\delete-invoices.ps1 -Database AED_ASNDUMMY -DocNo I-000005
#   .\delete-invoices.ps1 -Database AED_ASNDUMMY -All
#
# For demo books. An invoice generated while proving the module works is still a real document --
# it is in the A/R list, the debtor's ledger and the audit trail -- so it comes out the way
# AutoCount put it in, not with a DELETE statement.
param(
    [Parameter(Mandatory = $true)][string]$Database,
    [string]$DocNo = "",
    [switch]$All
)
if (-not $All -and $DocNo -eq "") { Write-Host "give -DocNo <no> or -All"; exit 2 }
$which = if ($All) { "ALL" } else { $DocNo }

$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\deleteinvoices.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:"$ac\AutoCount.Sales.dll" /r:System.Data.dll "$sp\DeleteInvoices.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\deleteinvoices.exe" $Database $which
exit $LASTEXITCODE
