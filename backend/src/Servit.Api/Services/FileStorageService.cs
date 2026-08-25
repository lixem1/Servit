namespace Servit.Api.Services;

public class FileStorageService(IConfiguration configuration, IWebHostEnvironment environment) : IFileStorageService
{
    private readonly string _rootPath = ResolveRootPath(configuration, environment);

    private static string ResolveRootPath(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["Storage:RootPath"];
        var rootPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "uploads")
            : configuredPath;

        Directory.CreateDirectory(rootPath);
        return rootPath;
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string subfolder)
    {
        var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var relativePath = Path.Combine(subfolder, safeFileName);
        var fullPath = Path.Combine(_rootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream);

        return relativePath;
    }

    public Stream OpenRead(string relativePath)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        return File.OpenRead(fullPath);
    }
}
