using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VisitorManagement.Web.Services;

namespace VisitorManagement.Web.Data;

public static class DatabaseBootstrap
{
    public static async Task EnsureMigratedAsync(
        AppDbContext db,
        IConfiguration config,
        ILogger logger,
        SqlConnectionResolver? connectionResolver = null,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlServer())
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        var configured = connectionResolver?.ConnectionString
            ?? config.GetConnectionString("SqlServer")
            ?? SqlConnectionResolver.DefaultSqlServer;

        var summary = SummarizeConnection(configured);
        logger.LogInformation("Local SQL Server target: {Summary}", summary);

        try
        {
            var working = await ResolveWorkingConnectionStringAsync(configured, logger, cancellationToken);
            if (!string.Equals(working, configured, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Named-pipe / default endpoint failed; using alternate SQL connection: {Summary}",
                    SummarizeConnection(working));
            }

            connectionResolver?.Use(working);
            db.Database.SetConnectionString(working);
            await db.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or DbException or InvalidOperationException)
        {
            throw new InvalidOperationException(BuildHelpMessage(SummarizeConnection(configured), ex), ex);
        }
    }

    public static async Task<string> ResolveWorkingConnectionStringAsync(
        string connectionString,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        Exception? last = null;
        foreach (var candidate in BuildConnectionCandidates(connectionString))
        {
            try
            {
                await ProbeServerAsync(candidate, cancellationToken);
                return candidate;
            }
            catch (Exception ex) when (ex is SqlException or DbException or InvalidOperationException)
            {
                last = ex;
                logger?.LogDebug(ex, "SQL probe failed for {Summary}", SummarizeConnection(candidate));
            }
        }

        throw last ?? new InvalidOperationException("ไม่สามารถเชื่อมต่อ SQL Server ได้");
    }

    public static IReadOnlyList<string> BuildConnectionCandidates(string connectionString)
    {
        var list = new List<string>();
        void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!list.Exists(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(value);
            }
        }

        Add(connectionString);

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var dataSource = (builder.DataSource ?? string.Empty).Trim();
            if (dataSource.Length == 0 || dataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase))
            {
                return list;
            }

            var withoutProtocol = dataSource;
            foreach (var prefix in new[] { "tcp:", "np:", "lpc:" })
            {
                if (withoutProtocol.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    withoutProtocol = withoutProtocol[prefix.Length..];
                    break;
                }
            }

            Add(WithDataSource(builder, "tcp:" + withoutProtocol));

            if (withoutProtocol.StartsWith(".\\", StringComparison.Ordinal) || withoutProtocol.StartsWith("./", StringComparison.Ordinal))
            {
                var instance = withoutProtocol[2..];
                Add(WithDataSource(builder, $@"tcp:127.0.0.1\{instance}"));
                Add(WithDataSource(builder, $@"tcp:localhost\{instance}"));
                Add(WithDataSource(builder, $@"localhost\{instance}"));
                Add(WithDataSource(builder, $@"127.0.0.1\{instance}"));
            }
            else if (withoutProtocol.Equals(".", StringComparison.Ordinal) || withoutProtocol.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                Add(WithDataSource(builder, "tcp:127.0.0.1"));
                Add(WithDataSource(builder, "tcp:localhost"));
            }
            else if (withoutProtocol.Contains('\\'))
            {
                Add(WithDataSource(builder, "tcp:" + withoutProtocol));
            }
        }
        catch
        {
            // Keep the original candidate only.
        }

        return list;
    }

    private static string WithDataSource(SqlConnectionStringBuilder template, string dataSource)
    {
        var copy = new SqlConnectionStringBuilder(template.ConnectionString)
        {
            DataSource = dataSource,
            TrustServerCertificate = true
        };
        return copy.ConnectionString;
    }

    private static async Task ProbeServerAsync(string connectionString, CancellationToken cancellationToken)
    {
        var original = new SqlConnectionStringBuilder(connectionString);
        var catalog = string.IsNullOrWhiteSpace(original.InitialCatalog) ? "VisitorManagment" : original.InitialCatalog;
        var timeout = Math.Clamp(original.ConnectTimeout <= 0 ? 5 : original.ConnectTimeout, 3, 15);

        // Prefer the real DB (user may have created VisitorManagment already), then master.
        foreach (var initialCatalog in new[] { catalog, "master" }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = initialCatalog,
                ConnectTimeout = timeout,
                TrustServerCertificate = true
            };

            try
            {
                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
                return;
            }
            catch (SqlException) when (!string.Equals(initialCatalog, "master", StringComparison.OrdinalIgnoreCase))
            {
                // Try master next (DB name typo / not created yet).
            }
        }

        // Final attempt so the caller receives the real error.
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master",
                ConnectTimeout = timeout,
                TrustServerCertificate = true
            };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
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
            "ถ้าติดตั้ง SQL Express และสร้างฐาน VisitorManagment ไว้แล้ว ให้ตรวจว่า:\n" +
            "1) บริการ SQL Server (SQLEXPRESS) สถานะ Running (services.msc)\n" +
            "2) ใน SQL Server Configuration Manager → Protocols for SQLEXPRESS เปิด TCP/IP และ Named Pipes แล้ว Restart บริการ\n" +
            "3) วาง appsettings.Local.json ข้าง VisitorManagement.Web.dll ใช้ TCP ชัดเจน เช่น\n" +
            "   Server=localhost\\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "4) ชื่อฐานต้องสะกดตรงนี้: VisitorManagment (ไม่มี e หลัง Manag)\n" +
            "5) ถ้า host ด้วย IIS: อย่าใช้ Trusted_Connection — ใช้ SQL Auth เช่น\n" +
            "   Server=localhost\\SQLEXPRESS;Database=VisitorManagment;User Id=sa;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
            "   หรือสร้าง Login IIS APPPOOL\\ชื่อพูล ให้เป็น db_owner ของ VisitorManagment\n" +
            "6) ทดสอบใน SSMS ด้วย connection string ชุดเดียวกับแอป ก่อนรีสตาร์ทเว็บ\n" +
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
                "แม้ติดตั้ง SQL Express แล้ว ก็ยังเจอข้อความนี้ได้เมื่อ Named Pipes (`Server=.\\SQLEXPRESS`) ใช้ไม่ได้ หรือบริการหยุดชั่วคราว\n" +
                "ทำทันทีบน Windows:\n" +
                "A) services.msc → SQL Server (SQLEXPRESS) → Start / Restart\n" +
                "B) SQL Server Configuration Manager → SQL Server Network Configuration → Protocols for SQLEXPRESS\n" +
                "   เปิด TCP/IP + Named Pipes → Restart บริการ SQLEXPRESS\n" +
                "C) ใส่ใน appsettings.Local.json (บังคับใช้ TCP/hostname):\n" +
                "   Server=localhost\\SQLEXPRESS;Database=VisitorManagment;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True\n" +
                "D) ถ้าเป็น IIS ให้ใช้ SQL Auth (User Id/Password) แทน Trusted_Connection\n" +
                "E) ตรวจชื่อฐานให้ตรง: VisitorManagment";
        }

        if (rootMessage.Contains("Login failed", StringComparison.OrdinalIgnoreCase))
        {
            return
                "แปลว่าต่อ SQL ได้แล้ว แต่ login ไม่ผ่าน — ตรวจ User Id/Password หรือให้สิทธิ์บัญชี Windows / IIS APPPOOL บนฐาน VisitorManagment";
        }

        return null;
    }
}
