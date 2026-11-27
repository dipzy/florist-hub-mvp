namespace FloristHub.Services.Interfaces.Storage;

/// <summary>
/// Abstração de armazenamento de ficheiros (disco local no MVP; substituível por cloud).</summary>
public interface IFileStorage
{
    Task<StoredFileDescriptor> SaveAsync(Stream content, string contentType, string originalFileName, Guid tenantId, CancellationToken ct = default);

    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct = default);

    void DeleteIfExists(string storagePath);
}

public record StoredFileDescriptor(string StoragePath, string PublicUrl, string ContentType, long SizeBytes);
