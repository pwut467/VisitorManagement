using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VisitorManagement.Web.Data;

namespace VisitorManagement.Web.Services;

public sealed class CloudConnectionSnapshot
{
    public bool Enabled { get; init; }
    public bool Online { get; init; }
    public bool Configured { get; init; }
    public string Server { get; init; } = "";
    public string Database { get; init; } = "";
    public DateTime? LastCheckedAt { get; init; }
    public string? LastError { get; init; }
    public int PendingSyncCount { get; init; }
}

public interface ICloudConnectionStatus
{
    CloudConnectionSnapshot Current { get; }
    void SetHealth(bool online, string? error, CloudOptions? options = null);
    void SetPendingCount(int count);
}

public sealed class CloudConnectionStatus : ICloudConnectionStatus
{
    private readonly object _gate = new();
    private CloudConnectionSnapshot _current;

    public CloudConnectionStatus(IConfiguration config)
    {
        var opts = CloudOptions.FromConfiguration(config);
        _current = new CloudConnectionSnapshot
        {
            Enabled = opts.Enabled,
            Online = false,
            Configured = opts.IsConfigured,
            Server = opts.Server,
            Database = opts.Database,
            LastCheckedAt = null,
            LastError = !opts.Enabled
                ? "ปิดการซิงก์คลาวด์"
                : opts.IsConfigured
                    ? "ยังไม่ได้ตรวจสอบ"
                    : "ยังไม่ได้ตั้งค่า Username/Password ของ Cloud SQL",
            PendingSyncCount = 0
        };
    }

    public CloudConnectionSnapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void SetHealth(bool online, string? error, CloudOptions? options = null)
    {
        lock (_gate)
        {
            _current = new CloudConnectionSnapshot
            {
                Enabled = options?.Enabled ?? _current.Enabled,
                Online = online,
                Configured = options?.IsConfigured ?? _current.Configured,
                Server = options?.Server ?? _current.Server,
                Database = options?.Database ?? _current.Database,
                LastCheckedAt = TimeHelper.Now,
                LastError = online ? null : error,
                PendingSyncCount = _current.PendingSyncCount
            };
        }
    }

    public void SetPendingCount(int count)
    {
        lock (_gate)
        {
            _current = new CloudConnectionSnapshot
            {
                Enabled = _current.Enabled,
                Online = _current.Online,
                Configured = _current.Configured,
                Server = _current.Server,
                Database = _current.Database,
                LastCheckedAt = _current.LastCheckedAt,
                LastError = _current.LastError,
                PendingSyncCount = Math.Max(0, count)
            };
        }
    }
}

public sealed class CloudOptions
{
    public bool Enabled { get; set; } = true;
    public string Server { get; set; } = "192.168.11.204";
    public string Database { get; set; } = "VisitorManagment";
    public string? UserId { get; set; }
    public string? Password { get; set; }
    public bool UseWindowsAuth { get; set; }
    public int HealthCheckSeconds { get; set; } = 15;
    public int SyncIntervalSeconds { get; set; } = 30;
    public int ConnectTimeoutSeconds { get; set; } = 10;
    public string? ConnectionString { get; set; }

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(Server)
        && !string.IsNullOrWhiteSpace(Database)
        && (UseWindowsAuth
            || (!string.IsNullOrWhiteSpace(UserId) && !string.IsNullOrEmpty(Password))
            || LooksLikeSqlAuthConnectionString(ConnectionString));

    public static CloudOptions FromConfiguration(IConfiguration config)
    {
        var opts = new CloudOptions();
        config.GetSection("Cloud").Bind(opts);
        ApplyConnectionString(opts, config.GetConnectionString("CloudSqlServer"));
        return opts;
    }

    public static void ApplyConnectionString(CloudOptions opts, string? configured)
    {
        var built = BuildConnectionString(opts);
        if (!string.IsNullOrWhiteSpace(built))
        {
            opts.ConnectionString = built;
            return;
        }

        if (string.IsNullOrWhiteSpace(configured))
        {
            opts.ConnectionString = null;
            return;
        }

        if (LooksLikeSqlAuthConnectionString(configured))
        {
            // Mixed Trusted_Connection + User ID often makes SqlClient ignore SQL Auth.
            opts.ConnectionString = SanitizeSqlAuthConnectionString(configured);
            return;
        }

        if (opts.UseWindowsAuth
            && configured.Contains("Trusted_Connection", StringComparison.OrdinalIgnoreCase))
        {
            opts.ConnectionString = configured;
            return;
        }

        opts.ConnectionString = null;
    }

    public static string BuildConnectionString(CloudOptions opts)
    {
        if (string.IsNullOrWhiteSpace(opts.Server) || string.IsNullOrWhiteSpace(opts.Database))
        {
            return "";
        }

        if (!opts.UseWindowsAuth && string.IsNullOrWhiteSpace(opts.UserId))
        {
            return "";
        }

        // Empty password with SQL Auth is almost always a misconfiguration (wiped by blank DB field).
        if (!opts.UseWindowsAuth && string.IsNullOrEmpty(opts.Password))
        {
            return "";
        }

        var parts = new List<string>
        {
            $"Server={opts.Server}",
            $"Database={opts.Database}",
            "TrustServerCertificate=True",
            "Encrypt=False",
            "MultipleActiveResultSets=True",
            $"Connect Timeout={Math.Clamp(opts.ConnectTimeoutSeconds, 3, 60)}"
        };

        if (opts.UseWindowsAuth)
        {
            parts.Add("Trusted_Connection=True");
        }
        else
        {
            parts.Add($"User Id={opts.UserId!.Trim()}");
            parts.Add($"Password={opts.Password ?? ""}");
        }

        return string.Join(';', parts);
    }

    public static string SanitizeSqlAuthConnectionString(string configured)
    {
        var builder = new SqlConnectionStringBuilder(configured)
        {
            IntegratedSecurity = false,
            TrustServerCertificate = true
        };
        // Remove ambiguous Windows-auth flags when SQL Auth is intended.
        builder.Remove("Trusted_Connection");
        builder.Remove("Integrated Security");
        builder.IntegratedSecurity = false;
        if (!builder.ContainsKey("Encrypt"))
        {
            builder.Encrypt = false;
        }

        return builder.ConnectionString;
    }

    private static bool LooksLikeSqlAuthConnectionString(string? cs) =>
        !string.IsNullOrWhiteSpace(cs)
        && (cs.Contains("User Id=", StringComparison.OrdinalIgnoreCase)
            || cs.Contains("User ID=", StringComparison.OrdinalIgnoreCase)
            || cs.Contains("UID=", StringComparison.OrdinalIgnoreCase));

    public static string DescribeError(Exception ex)
    {
        if (ex is SqlException sql)
        {
            return sql.Number switch
            {
                -2 or 258 => "หมดเวลาเชื่อมต่อ Cloud SQL (ตรวจ IP/พอร์ต 1433 และการเปิด Remote)",
                53 or 40 or 10060 or 10061 => "เข้าถึงเซิร์ฟเวอร์ Cloud ไม่ได้ (เครือข่าย/ไฟร์วอลล์/SQL Browser)",
                18456 => "Login Cloud SQL ไม่สำเร็จ (ตรวจ Username/Password และสิทธิ์)",
                4060 => "เปิดฐานข้อมูล Cloud ไม่ได้ (ตรวจชื่อ Database)",
                18452 => "SQL Server ไม่รับ Windows Auth จากเครื่องนี้ — ใช้ SQL Auth แทน",
                _ => Trim($"SQL {sql.Number}: {sql.Message}")
            };
        }

        var root = ex.GetBaseException().Message;
        if (root.Contains("Login failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Login Cloud SQL ไม่สำเร็จ (ตรวจ Username/Password)";
        }

        if (root.Contains("network-related", StringComparison.OrdinalIgnoreCase)
            || root.Contains("not found or not accessible", StringComparison.OrdinalIgnoreCase)
            || root.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return "เข้าถึงเซิร์ฟเวอร์ Cloud ไม่ได้ (ตรวจ IP 192.168.11.204 พอร์ต 1433 และไฟร์วอลล์)";
        }

        return Trim(root);
    }

    private static string Trim(string message) =>
        message.Length <= 400 ? message : message[..400];
}

public interface ICloudOptionsProvider
{
    Task<CloudOptions> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class CloudOptionsProvider : ICloudOptionsProvider
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopes;

    public CloudOptionsProvider(IConfiguration config, IServiceScopeFactory scopes)
    {
        _config = config;
        _scopes = scopes;
    }

    public async Task<CloudOptions> GetAsync(CancellationToken cancellationToken = default)
    {
        var opts = CloudOptions.FromConfiguration(_config);
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = await db.CompanyProfiles.AsNoTracking()
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (profile is not null)
            {
                opts.Enabled = profile.CloudEnabled;
                if (!string.IsNullOrWhiteSpace(profile.CloudServer))
                {
                    opts.Server = profile.CloudServer.Trim();
                }

                if (!string.IsNullOrWhiteSpace(profile.CloudDatabase))
                {
                    opts.Database = profile.CloudDatabase.Trim();
                }

                opts.UseWindowsAuth = profile.CloudUseWindowsAuth;
                if (!string.IsNullOrWhiteSpace(profile.CloudUserId))
                {
                    opts.UserId = profile.CloudUserId.Trim();
                }

                // Never wipe appsettings password with a blank DB value.
                if (!string.IsNullOrEmpty(profile.CloudPassword))
                {
                    opts.Password = profile.CloudPassword;
                }
            }
        }
        catch
        {
            // Local DB may still be starting; fall back to appsettings only.
        }

        CloudOptions.ApplyConnectionString(opts, _config.GetConnectionString("CloudSqlServer"));
        return opts;
    }
}
