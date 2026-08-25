namespace Servit.Api.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, string subfolder);

    Stream OpenRead(string relativePath);
}
