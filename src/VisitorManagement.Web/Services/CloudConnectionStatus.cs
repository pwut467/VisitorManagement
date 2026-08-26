namespace VisitorManagement.Web.Services;

public sealed class CloudConnectionSnapshot
{
    public bool Enabled { get; init; }
    public bool Online { get; init; }
    public string Server { get; init; } = "";
    public string Database { get; init; } = "";
    public DateTime? LastCheckedAt { get; init; }
    public string? LastError { get; init; }
    public int PendingSyncCount { get; init; }
}

public interface ICloudConnectionStatus
{
    CloudConnectionSnapshot Current { get; }
    void SetHealth(bool online, string? error);
    void SetPendingCount(int count);
}

public sealed class CloudConnectionStatus : ICloudConnectionStatus
{
    private readonly object _gate = new();
    private CloudConnectionSnapshot _current;

    public CloudConnectionStatus(IConfiguration config)
    {
        var opts = CloudOptions.From(config);
        _current = new CloudConnectionSnapshot
        {
            Enabled = opts.Enabled,
            Online = false,
            Server = opts.Server,
            Database = opts.Database,
            LastCheckedAt = null,
            LastError = opts.Enabled ? "ยังไม่ได้ตรวจสอบ" : "ปิดการซิงก์คลาวด์",
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

    public void SetHealth(bool online, string? error)
    {
        lock (_gate)
        {
            _current = new CloudConnectionSnapshot
            {
                Enabled = _current.Enabled,
                Online = online,
                Server = _current.Server,
                Database = _current.Database,
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
    public int HealthCheckSeconds { get; set; } = 15;
    public int SyncIntervalSeconds { get; set; } = 30;
    public string? ConnectionString { get; set; }

    public static CloudOptions From(IConfiguration config)
    {
        var opts = new CloudOptions();
        config.GetSection("Cloud").Bind(opts);

        if (!string.IsNullOrWhiteSpace(opts.UserId))
        {
            opts.ConnectionString = BuildConnectionString(opts);
        }
        else
        {
            opts.ConnectionString ??= config.GetConnectionString("CloudSqlServer");
            if (string.IsNullOrWhiteSpace(opts.ConnectionString))
            {
                opts.ConnectionString = BuildConnectionString(opts);
            }
        }

        if (string.IsNullOrWhiteSpace(opts.ConnectionString))
        {
            opts.Enabled = false;
        }

        return opts;
    }

    public static string BuildConnectionString(CloudOptions opts)
    {
        if (string.IsNullOrWhiteSpace(opts.Server) || string.IsNullOrWhiteSpace(opts.Database))
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
            "Connect Timeout=3"
        };

        if (!string.IsNullOrWhiteSpace(opts.UserId))
        {
            parts.Add($"User Id={opts.UserId}");
            parts.Add($"Password={opts.Password ?? ""}");
        }
        else
        {
            parts.Add("Trusted_Connection=True");
        }

        return string.Join(';', parts);
    }
}
