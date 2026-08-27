using SkiaSharp;

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
        sourceStream.Position = 0;
        using var skBitmap = SKBitmap.Decode(sourceStream);
        
        if (skBitmap == null)
        {
            sourceStream.Position = 0;
            await using var fileStream = File.Create(fullPath);
            await sourceStream.CopyToAsync(fileStream);
            return;
        }

        // Resize if needed
        SKBitmap scaledBitmap;
        if (skBitmap.Width > MaxImageWidth)
        {
            int newHeight = (int)(skBitmap.Height * (float)MaxImageWidth / skBitmap.Width);
            var destInfo = new SKImageInfo(MaxImageWidth, newHeight);
            scaledBitmap = new SKBitmap(destInfo);
            
            using var canvas = new SKCanvas(scaledBitmap);
            var srcRect = new SKRect(0, 0, skBitmap.Width, skBitmap.Height);
            var dstRect = new SKRect(0, 0, MaxImageWidth, newHeight);
            canvas.DrawBitmap(skBitmap, srcRect, dstRect, null);
        }
        else
        {
            scaledBitmap = skBitmap;
        }

        try
        {
            using var image = SKImage.FromBitmap(scaledBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 75);
            
            await using var outputStream = File.Create(fullPath);
            await data.AsStream().CopyToAsync(outputStream);
        }
        finally
        {
            if (scaledBitmap != skBitmap)
                scaledBitmap?.Dispose();
        }
    }

    public Stream OpenRead(string relativePath)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        return File.OpenRead(fullPath);
    }
}
