# ทดสอบว่าเครื่องนี้ต่อ SQL Express ได้ด้วย connection string เดียวกับเว็บ
# รันใน PowerShell:  .\scripts\test-sql-connection.ps1

param(
    [string]$ConnectionString = "Server=localhost\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
)

$ErrorActionPreference = "Stop"
Write-Host "Testing: $ConnectionString"

$candidates = @(
    $ConnectionString,
    ($ConnectionString -replace 'Server=\.\\SQLEXPRESS', 'Server=localhost\SQLEXPRESS'),
    ($ConnectionString -replace 'Server=localhost\\SQLEXPRESS', 'Server=tcp:127.0.0.1\SQLEXPRESS')
) | Select-Object -Unique

Add-Type -AssemblyName System.Data
foreach ($cs in $candidates) {
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection $cs
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DbName"
        $reader = $cmd.ExecuteReader()
        if ($reader.Read()) {
            Write-Host "OK  Server=$($reader['ServerName'])  Database=$($reader['DbName'])" -ForegroundColor Green
            Write-Host "ใช้ connection string นี้ใน appsettings.Local.json:"
            Write-Host $cs
        }
        $conn.Close()
        exit 0
    }
    catch {
        Write-Host "FAIL $cs" -ForegroundColor Yellow
        Write-Host "  $($_.Exception.Message)"
    }
}

Write-Host ""
Write-Host "ยังต่อไม่ได้ — ตรวจ services.msc (SQL Server SQLEXPRESS), เปิด TCP/IP ใน Configuration Manager, หรือใช้ SQL Auth สำหรับ IIS" -ForegroundColor Red
exit 1
