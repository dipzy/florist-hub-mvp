using FloristHub.Services.Interfaces.Storage;
using Microsoft.Extensions.Configuration;

namespace FloristHub.Services.Storage;

public class LocalDiskFileStorage : IFileStorage
{
    private readonly string _rootDirectory;
    private readonly string _publicBasePath;

    public LocalDiskFileStorage(IConfiguration configuration)
    {
        var local = configuration.GetSection("Storage:Local");
        _rootDirectory = Path.GetFullPath(
            local["RootPath"] ?? configuration["FileStorage:RootPath"] ?? "uploads");
        _publicBasePath = (
            local["PublicBasePath"] ?? configuration["FileStorage:PublicBasePath"] ?? "/api/files")
            .TrimEnd('/');
        Directory.CreateDirectory(_rootDirectory);
    }

    public async Task<StoredFileDescriptor> SaveAsync(
        Stream content,
        string contentType,
        string originalFileName,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8)
            ext = GuessExtension(contentType);

        var safeName = $"{Guid.NewGuid():N}{ext}";
        var relative = Path.Combine(tenantId.ToString("N"), safeName);
        var absolute = Path.Combine(_rootDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);

        await using (var fs = File.Create(absolute))
            await content.CopyToAsync(fs, ct);

        var size = new FileInfo(absolute).Length;
        var storagePath = relative.Replace('\\', '/');
        var publicUrl = $"{_publicBasePath}/{storagePath}";
        return new StoredFileDescriptor(storagePath, publicUrl, contentType, size);
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        var absolute = Path.Combine(_rootDirectory, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolute))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(absolute);
        return Task.FromResult<Stream?>(stream);
    }

    public void DeleteIfExists(string storagePath)
    {
        var absolute = Path.Combine(_rootDirectory, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolute))
            File.Delete(absolute);
    }

    private static string GuessExtension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".bin"
    };
}
