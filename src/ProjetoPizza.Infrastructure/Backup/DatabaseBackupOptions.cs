namespace ProjetoPizza.Infrastructure.Backup;

public sealed class DatabaseBackupOptions
{
    public const string SectionName = "Backup";

    public string Directory { get; set; } = "backups";
    public string PgDumpExecutable { get; set; } = "pg_dump";
    public bool AutomaticEnabled { get; set; }
    public int IntervalHours { get; set; } = 24;
    public int RetentionCount { get; set; } = 30;
}
