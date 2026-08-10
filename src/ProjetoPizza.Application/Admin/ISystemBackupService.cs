namespace ProjetoPizza.Application.Admin;

public interface ISystemBackupService
{
    Task<IReadOnlyCollection<DatabaseBackupDto>> ListAsync(CancellationToken cancellationToken);
    Task<DatabaseBackupDto> CreateAsync(string type, CancellationToken cancellationToken);
    Task<SystemBackupFile?> OpenReadAsync(string fileName, CancellationToken cancellationToken);
}

public sealed record SystemBackupFile(Stream Stream, string ContentType, string FileName);
