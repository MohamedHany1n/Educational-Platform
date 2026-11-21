using EducationalPlatform.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace EducationalPlatform.Tests;

/// <summary>
/// Receipt storage validation: extension allowlist, magic-byte checks, size
/// cap, and the hard refusal to store under wwwroot.
/// </summary>
public class ReceiptStorageTests : IDisposable
{
    private class StubEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private readonly string _contentRoot;
    private readonly string _wwwRoot;
    private readonly StubEnvironment _environment;

    public ReceiptStorageTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), $"receipts-test-{Guid.NewGuid():N}");
        _wwwRoot = Path.Combine(_contentRoot, "wwwroot");
        Directory.CreateDirectory(_wwwRoot);
        _environment = new StubEnvironment { ContentRootPath = _contentRoot, WebRootPath = _wwwRoot };
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
            Directory.Delete(_contentRoot, recursive: true);
    }

    private LocalReceiptStorage CreateStorage(string path = "App_Data/receipts", long maxBytes = 1024)
        => new(_environment, Options.Create(new ReceiptStorageOptions { Path = path, MaxBytes = maxBytes }));

    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    [Fact]
    public async Task Save_ValidPng_StoresAndCanOpen()
    {
        var storage = CreateStorage();

        var stored = await storage.SaveAsync(new MemoryStream(PngBytes), "my receipt.png");

        Assert.NotNull(stored);
        Assert.Equal(stored, Path.GetFileName(stored));
        Assert.True(File.Exists(Path.Combine(_contentRoot, "App_Data", "receipts", stored)));

        var opened = await storage.OpenAsync(stored);
        Assert.NotNull(opened);
        Assert.Equal("image/png", opened.Value.ContentType);
        opened.Value.Content.Dispose();
    }

    [Fact]
    public async Task Save_DisallowedExtension_Rejected()
    {
        var storage = CreateStorage();
        Assert.Null(await storage.SaveAsync(new MemoryStream(PngBytes), "evil.exe"));
        Assert.Null(await storage.SaveAsync(new MemoryStream(PngBytes), "page.html"));
    }

    [Fact]
    public async Task Save_WrongMagicBytes_Rejected()
    {
        var storage = CreateStorage();
        // A .png name whose content is not a PNG.
        Assert.Null(await storage.SaveAsync(new MemoryStream(new byte[] { 1, 2, 3, 4 }), "fake.png"));
    }

    [Fact]
    public async Task Save_TooLarge_Rejected()
    {
        var storage = CreateStorage(maxBytes: 4);
        Assert.Null(await storage.SaveAsync(new MemoryStream(PngBytes), "big.png"));
    }

    [Fact]
    public async Task Open_TraversalPath_Rejected()
    {
        var storage = CreateStorage();
        Assert.Null(await storage.OpenAsync("../secrets.png"));
        Assert.Null(await storage.OpenAsync("sub/dir/x.png"));
        Assert.Null(await storage.OpenAsync("x.exe"));
    }

    [Fact]
    public void Constructor_PathInsideWwwRoot_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => CreateStorage("wwwroot/receipts"));
    }
}
