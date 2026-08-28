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
        return
            "สร้าง/เชื่อมต่อฐานข้อมูล VisitorManagment ไม่สำเร็จ\n" +
            $"เป้าหมาย: {summary}\n" +
            $"สาเหตุ: {root}\n\n" +
            "ตรวจบนเครื่องที่รันเว็บ:\n" +
            "1) ติดตั้งและเปิดบริการ SQL Server Express (หรือ LocalDB / SQL Server)\n" +
            "2) แก้ ConnectionStrings:SqlServer ใน appsettings.Local.json ให้ตรง instance จริง เช่น\n" +
            "   - Server=.\\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "   - Server=(localdb)\\MSSQLLocalDB;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "   - Server=localhost;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "3) บัญชี Windows ที่รันแอปต้องมีสิทธิ์สร้างฐานข้อมูล (dbcreator) หรือสร้าง DB ชื่อ VisitorManagment ไว้ก่อน\n" +
            "4) หรือใช้ Docker: docker compose up -d แล้วตั้ง Server=localhost,1433;User Id=sa;Password=Your_password123;...\n" +
            "คัดลอกไฟล์ตัวอย่าง: src/VisitorManagement.Web/appsettings.Local.json.example → appsettings.Local.json";
    }
}
