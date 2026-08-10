using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjetoPizza.Application.Admin;

namespace ProjetoPizza.Infrastructure.Backup;

public sealed class DatabaseBackupWorker(
    ISystemBackupService backupService,
    IOptions<DatabaseBackupOptions> options,
    ILogger<DatabaseBackupWorker> logger) : BackgroundService
{
    private readonly DatabaseBackupOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.AutomaticEnabled)
        {
            return;
        }

        var interval = TimeSpan.FromHours(Math.Clamp(_options.IntervalHours, 1, 168));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var latest = (await backupService.ListAsync(stoppingToken)).FirstOrDefault();
                if (latest is null || DateTimeOffset.UtcNow - latest.CreatedAt >= interval)
                {
                    await backupService.CreateAsync("automatic", stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Automatic database backup failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
