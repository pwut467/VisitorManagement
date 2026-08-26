namespace VisitorManagement.Web.Services;

/// <summary>
/// Periodically probes the cloud SQL Server and pushes any locally pending visits.
/// </summary>
public sealed class CloudSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ICloudConnectionStatus _status;
    private readonly CloudOptions _options;
    private readonly ILogger<CloudSyncBackgroundService> _logger;

    public CloudSyncBackgroundService(
        IServiceScopeFactory scopes,
        ICloudConnectionStatus status,
        IConfiguration config,
        ILogger<CloudSyncBackgroundService> logger)
    {
        _scopes = scopes;
        _status = status;
        _options = CloudOptions.From(config);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Cloud sync background service is disabled.");
            return;
        }

        var healthDelay = TimeSpan.FromSeconds(Math.Max(5, _options.HealthCheckSeconds));
        var syncDelay = TimeSpan.FromSeconds(Math.Max(10, _options.SyncIntervalSeconds));
        var nextSync = TimeHelper.Now;

        // Small startup delay so local DB seeding can finish first.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<ICloudVisitSyncService>();
                var online = await sync.ProbeAsync(stoppingToken);
                if (online && TimeHelper.Now >= nextSync)
                {
                    var count = await sync.SyncPendingAsync(stoppingToken);
                    if (count > 0)
                    {
                        _logger.LogInformation("Synced {Count} pending visit(s) to cloud", count);
                    }

                    nextSync = TimeHelper.Now.Add(syncDelay);
                }
                else if (!online)
                {
                    nextSync = TimeHelper.Now; // retry sync as soon as cloud returns
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Cloud sync loop failed");
                _status.SetHealth(false, ex.GetBaseException().Message);
            }

            try
            {
                await Task.Delay(healthDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
