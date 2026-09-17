# Installs this plug-in into an account book other than the dev one, and brings that book to the
# state it reaches after AutoCount has opened it once (package registered, schema migrated, access
# rights granted).
#
#   .\install-into-book.ps1 -Database AED_ASNDUMMY
#
# The book must already exist -- create it in AutoCount (Manage Account Book). This only installs
# into it.
param(
  [Parameter(Mandatory = $true)][string]$Database,
  [string]$Server   = "localhost,1433",
  [string]$User     = "sa",
  [string]$Password = "rs6663",
  [string]$GrantTo  = "ADMIN"
)
$sp  = $PSScriptRoot
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$ac  = "C:\Program Files\AutoCount\Accounting 2.2"
$dll = "C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll"
& $csc /nologo /target:exe /platform:x64 /out:"$sp\installintobook.exe" /r:"$dll" `
       /r:"$ac\AutoCount.dll" /r:"$ac\AutoCount.Accounting.dll" /r:"$ac\AutoCount.MainEntry.dll" /r:"$ac\AutoCount.Accounting.UI.dll" `
       /r:System.Data.dll "$sp\InstallIntoBook.cs"
if ($LASTEXITCODE -ne 0) { exit 1 }
& "$sp\installintobook.exe" $Database $Server $User $Password $GrantTo
exit $LASTEXITCODE
