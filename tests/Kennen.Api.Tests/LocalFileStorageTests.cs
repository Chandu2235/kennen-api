using Kennen.Api.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text;

namespace Kennen.Api.Tests;

public class LocalFileStorageTests : IDisposable
{
    private readonly string _root;
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "kennen-storage-tests", Guid.NewGuid().ToString("N"));
        _storage = new LocalFileStorage(
            Options.Create(new FileStorageOptions { RootPath = _root }),
            new StubHostEnvironment());
    }

    [Fact]
    public async Task SaveAsync_RoundTripsContent()
    {
        var key = await SaveAsync("resume bytes", "Jane Doe CV.pdf");

        await using var stream = await _storage.OpenAsync(key);

        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        Assert.Equal("resume bytes", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task SaveAsync_DiscardsUploaderSuppliedFileNameButKeepsExtension()
    {
        var key = await SaveAsync("x", "Jane Doe CV.pdf");

        Assert.EndsWith(".pdf", key);
        Assert.DoesNotContain("Jane", key);
        Assert.DoesNotContain(" ", key);
    }

    [Fact]
    public async Task SaveAsync_GeneratesADistinctKeyPerUpload()
    {
        var first = await SaveAsync("a", "cv.pdf");
        var second = await SaveAsync("b", "cv.pdf");

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("../../../windows/win.ini")]
    [InlineData("..\\..\\secrets.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("")]
    public async Task OpenAsync_RefusesKeysThatEscapeTheStorageRoot(string maliciousKey)
    {
        // A tampered storage key must never resolve to a file outside the storage root.
        Assert.Null(await _storage.OpenAsync(maliciousKey));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheStoredFile()
    {
        var key = await SaveAsync("x", "cv.pdf");

        await _storage.DeleteAsync(key);

        Assert.Null(await _storage.OpenAsync(key));
    }

    [Fact]
    public async Task DeleteAsync_IsSafeForAnUnknownKey()
    {
        await _storage.DeleteAsync("2020/01/does-not-exist.pdf");
    }

    private async Task<string> SaveAsync(string content, string fileName)
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return await _storage.SaveAsync(source, fileName, "application/pdf");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
    }
}
