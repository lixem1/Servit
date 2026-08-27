using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Servit.Api.Services;

public class FileStorageService(IConfiguration configuration, IWebHostEnvironment environment) : IFileStorageService
{
    private readonly string _rootPath = ResolveRootPath(configuration, environment);
    
    // Compression thresholds
    private const long CompressionThresholdBytes = 1_500_000; // 1.5 MB
    private const long MaxImageBytes = 2_000_000; // 2 MB final max
    private const int MaxImageWidth = 1920;
    
    // Image types that should be compressed
    private static readonly HashSet<string> CompressibleImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/heic", "image/heif"
    };

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

        // Compress if it's an image and larger than threshold
        var contentType = GetContentType(fileName);
        if (CompressibleImageTypes.Contains(contentType) && content.Length > CompressionThresholdBytes)
        {
            await CompressAndSaveImageAsync(content, fullPath);
        }
        else
        {
            await using var fileStream = File.Create(fullPath);
            await content.CopyToAsync(fileStream);
        }

        return relativePath;
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            _ => "application/octet-stream"
        };
    }

    private async Task CompressAndSaveImageAsync(Stream sourceStream, string fullPath)
    {
        using var image = await Image.LoadAsync(sourceStream);
        
        // Reduce dimensions if needed
        if (image.Width > MaxImageWidth)
        {
            var ratio = (float)MaxImageWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(MaxImageWidth, newHeight));
        }

        // Save as JPEG with quality adjustment
        var jpegEncoder = new JpegEncoder { Quality = 75 };
        await using var outputStream = File.Create(fullPath);
        await image.SaveAsJpegAsync(outputStream, jpegEncoder);
    }

    public Stream OpenRead(string relativePath)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        return File.OpenRead(fullPath);
    }
}
