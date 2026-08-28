using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace VisitorManagement.Web.Data;

public static class DatabaseBootstrap
{
    public static async Task EnsureMigratedAsync(AppDbContext db, IConfiguration config, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlServer())
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        var connectionString = config.GetConnectionString("SqlServer")
            ?? @"Server=.\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        var summary = SummarizeConnection(connectionString);
        logger.LogInformation("Local SQL Server target: {Summary}", summary);

        try
        {
            await ProbeServerAsync(connectionString, cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or DbException or InvalidOperationException)
        {
            throw new InvalidOperationException(BuildHelpMessage(summary, ex), ex);
        }
    }

    private static async Task ProbeServerAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = Math.Clamp(builderSafeTimeout(connectionString), 3, 15)
        };

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private static int builderSafeTimeout(string connectionString)
    {
        try
        {
            return new SqlConnectionStringBuilder(connectionString).ConnectTimeout;
        }
        catch
        {
            return 5;
        }
    }

    public static string SummarizeConnection(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var auth = builder.IntegratedSecurity || string.IsNullOrWhiteSpace(builder.UserID)
                ? "Trusted_Connection"
                : "User Id=" + builder.UserID;
            return $"Server={builder.DataSource}; Database={builder.InitialCatalog}; {auth}";
        }
        catch
        {
            return "(connection string ไม่ถูกต้อง)";
        }
    }

    public static string BuildHelpMessage(string summary, Exception ex)
    {
        var root = ex.GetBaseException().Message;
        var specific = DescribeCommonFailure(root);

        return
            "สร้าง/เชื่อมต่อฐานข้อมูล VisitorManagment ไม่สำเร็จ\n" +
            $"เป้าหมาย: {summary}\n" +
            $"สาเหตุ: {root}\n\n" +
            (specific is null ? string.Empty : specific + "\n\n") +
            "ตรวจบนเครื่องที่รันเว็บ:\n" +
            "1) ติดตั้งและเปิดบริการ SQL Server Express (หรือ LocalDB / SQL Server)\n" +
            "2) วางไฟล์ appsettings.Local.json ในโฟลเดอร์เดียวกับ VisitorManagement.Web.dll แล้วแก้ ConnectionStrings:SqlServer เช่น\n" +
            "   - Server=.\\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "   - Server=localhost\\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "   - Server=(localdb)\\MSSQLLocalDB;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "   - Server=localhost;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "3) ถ้า host ด้วย IIS: App Pool มักใช้ ApplicationPoolIdentity — Trusted_Connection มักใช้ไม่ได้\n" +
            "   ใช้ SQL Auth แทน เช่น Server=.\\SQLEXPRESS;Database=VisitorManagment;User Id=sa;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "   หรือสร้าง Login ให้ IIS APPPOOL\\ชื่อAppPool แล้วให้สิทธิ์ dbcreator / db_owner\n" +
            "4) สร้าง DB ชื่อ VisitorManagment ใน SSMS ไว้ก่อนก็ได้\n" +
            "5) Docker: docker compose up -d แล้วใช้ Server=localhost,1433;User Id=sa;Password=Your_password123;...\n" +
            "6) ดู logs\\startup-error.txt และ logs\\stdout_*.log ในโฟลเดอร์ publish\n" +
            "ตัวอย่างไฟล์: appsettings.Local.json.example → appsettings.Local.json";
    }

    /// <summary>
    /// Extra Thai guidance for frequent SqlClient failures (shown above the generic checklist).
    /// </summary>
    public static string? DescribeCommonFailure(string rootMessage)
    {
        if (string.IsNullOrWhiteSpace(rootMessage))
        {
            return null;
        }

        if (rootMessage.Contains("other end of the pipe", StringComparison.OrdinalIgnoreCase)
            || rootMessage.Contains("error: 40", StringComparison.OrdinalIgnoreCase)
            || rootMessage.Contains("network-related", StringComparison.OrdinalIgnoreCase)
            || rootMessage.Contains("server was not found", StringComparison.OrdinalIgnoreCase)
            || rootMessage.Contains("could not open a connection", StringComparison.OrdinalIgnoreCase))
        {
            return
                "แปลว่าเครื่องนี้เชื่อม instance SQL ไม่ได้ (มักเพราะยังไม่ได้ติดตั้ง / บริการหยุด / ชื่อ instance ผิด)\n" +
                "ทำทันทีบน Windows:\n" +
                "A) เปิด services.msc → หา \"SQL Server (SQLEXPRESS)\" → Start (และตั้ง Automatic)\n" +
                "B) ถ้าไม่มีบริการนี้ = ยังไม่มี SQL Express → ติดตั้ง SQL Server Express หรือใช้ LocalDB\n" +
                "C) ทางลัดที่ง่าย: ติดตั้ง LocalDB แล้วใส่ใน appsettings.Local.json:\n" +
                "   Server=(localdb)\\MSSQLLocalDB;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
                "D) หรือรัน Docker: docker compose up -d แล้วใช้\n" +
                "   Server=localhost,1433;Database=VisitorManagment;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
                "E) ใน SQL Server Configuration Manager เปิด Named Pipes + TCP/IP ของ SQLEXPRESS แล้วรีสตาร์ทบริการ";
        }

        if (rootMessage.Contains("Login failed", StringComparison.OrdinalIgnoreCase))
        {
            return
                "แปลว่าต่อ SQL ได้แล้ว แต่ login ไม่ผ่าน — ตรวจ User Id/Password หรือให้สิทธิ์บัญชี Windows / IIS APPPOOL บนฐาน VisitorManagment";
        }

        return null;
    }
}

