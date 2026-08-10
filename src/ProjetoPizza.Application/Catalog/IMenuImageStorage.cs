namespace ProjetoPizza.Application.Catalog;

public interface IMenuImageStorage
{
    Task<string> StoreAsync(
        Stream content,
        string contentType,
        string originalFileName,
        CancellationToken cancellationToken);

    Task DeleteAsync(string publicUrl, CancellationToken cancellationToken);
}
