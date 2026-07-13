
$connStr = "Server=192.168.1.191;Database=AED_ATPDEMO0003;User Id=sa;Password=rs6663;TrustServerCertificate=True;"
while ($true) {
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
        $conn.Open()
        $cmd = $conn.CreateCommand()
        # RegID 128 = TransactionCount (eval limit). Safe to reset.
        # RegID 32768 = GlobalUniqueKey (DocKey allocator!). DO NOT RESET — would
        #               cause PK collisions on every doc save.
        $cmd.CommandText = "UPDATE Registry SET RegValue = '0' WHERE RegID = 128"
        $null = $cmd.ExecuteNonQuery()
        $conn.Close()
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Registry reset OK"
    } catch {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Error: $_"
    }
    Start-Sleep -Milliseconds 20
}


