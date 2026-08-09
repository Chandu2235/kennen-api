namespace Kennen.Api.Storage;

/// <summary>
/// Abstracts résumé storage so the local-disk implementation used in development can be
/// swapped for Azure Blob / S3 in production without touching controller code.
/// </summary>
public interface IFileStorage
{
    /// <summary>Persists the stream and returns an opaque storage key.</summary>
    Task<string> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default);

    /// <summary>Opens a previously saved file, or returns null when the key no longer resolves.</summary>
    Task<Stream?> OpenAsync(string storageKey, CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
