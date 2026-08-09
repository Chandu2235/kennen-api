using Microsoft.Extensions.Options;

namespace Kennen.Api.Storage;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Absolute or content-root-relative directory that holds uploaded résumés.</summary>
    public string RootPath { get; set; } = "storage/resumes";

    /// <summary>Hard cap on résumé size. Enforced again by the request body size limit.</summary>
    public long MaxResumeBytes { get; set; } = 5 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = { ".pdf", ".doc", ".docx" };

    public string[] AllowedContentTypes { get; set; } =
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
}

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<FileStorageOptions> options, IHostEnvironment env)
    {
        var configured = options.Value.RootPath;
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(env.ContentRootPath, configured);

        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        // The uploader never influences the path: we keep only the extension and generate the name.
        var extension = Path.GetExtension(originalFileName);
        var now = DateTimeOffset.UtcNow;
        var relativeDirectory = Path.Combine(now.ToString("yyyy"), now.ToString("MM"));
        var key = Path.Combine(relativeDirectory, $"{Guid.NewGuid():N}{extension}").Replace('\\', '/');

        var absolutePath = ResolveAbsolutePath(key)
            ?? throw new InvalidOperationException("Generated storage key resolved outside the storage root.");

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var target = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(target, ct);

        return key;
    }

    public Task<Stream?> OpenAsync(string storageKey, CancellationToken ct = default)
    {
        var absolutePath = ResolveAbsolutePath(storageKey);
        if (absolutePath is null || !File.Exists(absolutePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var absolutePath = ResolveAbsolutePath(storageKey);
        if (absolutePath is not null && File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves a storage key against the root and rejects anything that escapes it,
    /// so a tampered key stored in the database cannot become a path traversal.
    /// </summary>
    private string? ResolveAbsolutePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var candidate = Path.GetFullPath(Path.Combine(_root, storageKey));
        var rootWithSeparator = Path.GetFullPath(_root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }
}
