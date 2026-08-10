using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ProjetoPizza.Application.Catalog;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Media;

public sealed class LocalMenuImageStorage(
    IHostEnvironment environment,
    IOptions<MenuMediaOptions> options) : IMenuImageStorage
{
    private readonly MenuMediaOptions _options = options.Value;

    public async Task<string> StoreAsync(
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new BusinessRuleException("menu_image.file_name", "Image file name is required.");
        }

        var directory = ResolveDirectory();
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.upload");
        try
        {
            await using (var destination = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > _options.MaxFileBytes)
                    {
                        throw new BusinessRuleException("menu_image.size", "Menu images must be at most 5 MB.");
                    }
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            var signature = new byte[12];
            await using (var source = File.OpenRead(temporaryPath))
            {
                _ = await source.ReadAsync(signature, cancellationToken);
            }
            var extension = DetectExtension(signature, contentType);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            File.Move(temporaryPath, Path.Combine(directory, fileName));
            return $"{_options.PublicPath.TrimEnd('/')}/{fileName}";
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public Task DeleteAsync(string publicUrl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var prefix = _options.PublicPath.TrimEnd('/') + "/";
        if (!publicUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;
        var fileName = Path.GetFileName(publicUrl);
        if (string.IsNullOrWhiteSpace(fileName)) return Task.CompletedTask;
        var path = Path.Combine(ResolveDirectory(), fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolveDirectory() => Path.GetFullPath(
        Path.IsPathRooted(_options.Directory)
            ? _options.Directory
            : Path.Combine(environment.ContentRootPath, _options.Directory));

    private static string DetectExtension(byte[] bytes, string contentType)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff)
            return ".jpg";
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4e && bytes[3] == 0x47)
            return ".png";
        if (bytes.Length >= 12 &&
            bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return ".webp";

        throw new BusinessRuleException(
            "menu_image.format",
            $"Unsupported menu image format '{contentType}'. Use JPEG, PNG or WebP.");
    }
}
