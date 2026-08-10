using System.Globalization;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Infrastructure.Persistence;

namespace ProjetoPizza.Infrastructure.Printing;

public sealed class PrintQueueWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PrintQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected error while processing the print queue.");
            }
        }
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ProjetoPizzaDbContext>();
        var interrupted = await context.PrintJobs
            .Where(job => job.Status == PrintJobStatus.Processing)
            .ToArrayAsync(cancellationToken);
        foreach (var job in interrupted) job.Fail("API restarted while the print job was processing.");
        if (interrupted.Length > 0) await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ProjetoPizzaDbContext>();
        var now = DateTimeOffset.UtcNow;
        var job = await context.PrintJobs
            .Where(candidate =>
                (candidate.Status == PrintJobStatus.Pending ||
                 (candidate.Status == PrintJobStatus.Failed && candidate.Attempts < 5)) &&
                candidate.NextAttemptAt <= now)
            .OrderBy(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null) return;

        var printer = await context.Devices.SingleAsync(device => device.Id == job.PrinterId, cancellationToken);
        job.Start();
        await context.SaveChangesAsync(cancellationToken);
        try
        {
            if (printer.Status is DeviceStatus.Blocked or DeviceStatus.Maintenance)
                throw new InvalidOperationException("A impressora está bloqueada ou em manutenção.");
            if (string.IsNullOrWhiteSpace(printer.IpAddress) || !printer.PrinterPort.HasValue)
                throw new InvalidOperationException("A impressora não possui host e porta configurados.");

            await SendEscPosAsync(
                printer.IpAddress, printer.PrinterPort.Value,
                job.Payload, job.Copies, cancellationToken);
            job.Complete();
            printer.UpdateStatus(DeviceStatus.Online, null, false, "Network", printer.IpAddress, null);
            logger.LogInformation("Print job {PrintJobId} completed on {PrinterName}.", job.Id.Value, printer.Name);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            job.Fail(exception.Message);
            printer.UpdateStatus(DeviceStatus.Offline, null, false, "NetworkError", printer.IpAddress, null);
            logger.LogWarning(exception, "Print job {PrintJobId} failed on {PrinterName}.", job.Id.Value, printer.Name);
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SendEscPosAsync(
        string host,
        int port,
        string payload,
        int copies,
        CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken);
        await using var stream = client.GetStream();
        var text = RemoveDiacritics(payload).Replace("\n", "\r\n", StringComparison.Ordinal);
        var content = Encoding.ASCII.GetBytes(text);
        for (var copy = 0; copy < copies; copy++)
        {
            await stream.WriteAsync(new byte[] { 0x1b, 0x40 }, cancellationToken); // initialize
            await stream.WriteAsync(content, cancellationToken);
            await stream.WriteAsync(new byte[] { 0x0a, 0x0a, 0x0a, 0x1d, 0x56, 0x41, 0x03 }, cancellationToken); // feed + cut
        }
        await stream.FlushAsync(cancellationToken);
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
