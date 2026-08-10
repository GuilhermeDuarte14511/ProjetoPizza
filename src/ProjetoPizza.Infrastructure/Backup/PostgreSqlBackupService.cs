using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using ProjetoPizza.Application.Admin;

namespace ProjetoPizza.Infrastructure.Backup;

public sealed class PostgreSqlBackupService(
    IConfiguration configuration,
    IOptions<DatabaseBackupOptions> options,
    ILogger<PostgreSqlBackupService> logger) : ISystemBackupService
{
    private const string FilePrefix = "projeto-pizza-";
    private readonly DatabaseBackupOptions _options = options.Value;
    private readonly SemaphoreSlim _backupLock = new(1, 1);

    public Task<IReadOnlyCollection<DatabaseBackupDto>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = GetBackupDirectory();
        Directory.CreateDirectory(directory);
        IReadOnlyCollection<DatabaseBackupDto> result = Directory
            .EnumerateFiles(directory, $"{FilePrefix}*.dump", SearchOption.TopDirectoryOnly)
            .Select(ToDto)
            .OrderByDescending(backup => backup.CreatedAt)
            .ToArray();
        return Task.FromResult(result);
    }

    public async Task<DatabaseBackupDto> CreateAsync(string type, CancellationToken cancellationToken)
    {
        await _backupLock.WaitAsync(cancellationToken);
        try
        {
            var normalizedType = type.Equals("automatic", StringComparison.OrdinalIgnoreCase)
                ? "automatic"
                : "manual";
            var directory = GetBackupDirectory();
            Directory.CreateDirectory(directory);
            var timestamp = DateTimeOffset.UtcNow;
            var fileName = $"{FilePrefix}{normalizedType}-{timestamp:yyyyMMdd-HHmmssfff}.dump";
            var targetPath = Path.Combine(directory, fileName);
            var partialPath = targetPath + ".partial";
            var connectionString = configuration.GetConnectionString("PostgreSql")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required.");
            var connection = new NpgsqlConnectionStringBuilder(connectionString);

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.PgDumpExecutable,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("--format=custom");
            startInfo.ArgumentList.Add("--no-owner");
            startInfo.ArgumentList.Add("--no-privileges");
            startInfo.ArgumentList.Add($"--host={connection.Host}");
            startInfo.ArgumentList.Add($"--port={connection.Port}");
            startInfo.ArgumentList.Add($"--username={connection.Username}");
            startInfo.ArgumentList.Add($"--dbname={connection.Database}");
            startInfo.ArgumentList.Add($"--file={partialPath}");
            if (!string.IsNullOrEmpty(connection.Password))
            {
                startInfo.Environment["PGPASSWORD"] = connection.Password;
            }

            using var process = new Process { StartInfo = startInfo };
            var started = false;
            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("Não foi possível iniciar o pg_dump.");
                }
                started = true;

                var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                var error = await standardError;
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"pg_dump terminou com código {process.ExitCode}: {Limit(error, 1000)}");
                }

                File.Move(partialPath, targetPath);
                await EnforceRetentionAsync(cancellationToken);
                logger.LogInformation("Database backup {BackupFile} created successfully.", fileName);
                return ToDto(targetPath);
            }
            catch
            {
                if (started && !process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None);
                }

                if (File.Exists(partialPath))
                {
                    File.Delete(partialPath);
                }

                throw;
            }
        }
        finally
        {
            _backupLock.Release();
        }
    }

    public Task<SystemBackupFile?> OpenReadAsync(string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeFileName = Path.GetFileName(fileName);
        if (!string.Equals(safeFileName, fileName, StringComparison.Ordinal) ||
            !safeFileName.StartsWith(FilePrefix, StringComparison.Ordinal) ||
            !safeFileName.EndsWith(".dump", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<SystemBackupFile?>(null);
        }

        var path = Path.Combine(GetBackupDirectory(), safeFileName);
        if (!File.Exists(path))
        {
            return Task.FromResult<SystemBackupFile?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        return Task.FromResult<SystemBackupFile?>(new SystemBackupFile(
            stream,
            "application/vnd.postgresql.custom-backup",
            safeFileName));
    }

    private async Task EnforceRetentionAsync(CancellationToken cancellationToken)
    {
        var retained = Math.Clamp(_options.RetentionCount, 1, 365);
        var obsolete = (await ListAsync(cancellationToken)).Skip(retained).ToArray();
        foreach (var backup in obsolete)
        {
            File.Delete(Path.Combine(GetBackupDirectory(), backup.FileName));
        }
    }

    private string GetBackupDirectory()
    {
        var configured = string.IsNullOrWhiteSpace(_options.Directory) ? "backups" : _options.Directory;
        return Path.GetFullPath(configured, AppContext.BaseDirectory);
    }

    private static DatabaseBackupDto ToDto(string path)
    {
        var info = new FileInfo(path);
        var type = info.Name.Contains("-automatic-", StringComparison.OrdinalIgnoreCase) ? "Automático" : "Manual";
        return new DatabaseBackupDto(info.Name, info.LastWriteTimeUtc, info.Length, type);
    }

    private static string Limit(string value, int length) =>
        value.Length <= length ? value.Trim() : value[..length].Trim();
}
