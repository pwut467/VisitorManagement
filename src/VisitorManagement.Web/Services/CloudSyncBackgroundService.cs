namespace VisitorManagement.Web.Services;

/// <summary>
/// Periodically probes the cloud SQL Server and pushes any locally pending visits.
/// </summary>
public sealed class CloudSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ICloudConnectionStatus _status;
    private readonly ICloudOptionsProvider _optionsProvider;
    private readonly ILogger<CloudSyncBackgroundService> _logger;

    public CloudSyncBackgroundService(
        IServiceScopeFactory scopes,
        ICloudConnectionStatus status,
        ICloudOptionsProvider optionsProvider,
        ILogger<CloudSyncBackgroundService> logger)
    {
        _scopes = scopes;
        _status = status;
        _optionsProvider = optionsProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var nextSync = TimeHelper.Now;

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = await _optionsProvider.GetAsync(stoppingToken);
            var healthDelay = TimeSpan.FromSeconds(Math.Max(5, options.HealthCheckSeconds));
            var syncDelay = TimeSpan.FromSeconds(Math.Max(10, options.SyncIntervalSeconds));

            if (!options.Enabled)
            {
                _status.SetHealth(false, "ปิดการซิงก์คลาวด์", options);
            }
            else
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
                        nextSync = TimeHelper.Now;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Cloud sync loop failed");
                    _status.SetHealth(false, CloudOptions.DescribeError(ex), options);
                }
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
