namespace ProjetoPizza.Infrastructure.Media;

public sealed class MenuMediaOptions
{
    public const string SectionName = "MenuMedia";
    public string Directory { get; set; } = "data/media";
    public string PublicPath { get; set; } = "/media";
    public long MaxFileBytes { get; set; } = 5 * 1024 * 1024;
}
