using Microsoft.Extensions.Options;

namespace EducationalPlatform.Web.Services;

public class ReceiptStorageOptions
{
    /// <summary>Absolute path, or path relative to the content root. Must NOT be under wwwroot.</summary>
    public string Path { get; set; } = "App_Data/receipts";

    /// <summary>Maximum receipt size in bytes. Default 5 MB (pending team decision).</summary>
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
}

/// <summary>
/// Local-disk receipt storage with strict validation: extension allowlist,
/// magic-byte content checks, and a size cap. Files get random names, and the
/// stored path never contains the original file name.
/// </summary>
public class LocalReceiptStorage : IReceiptStorage
{
    private static readonly Dictionary<string, (string ContentType, byte[][] Signatures)> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ("image/jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } }),
        [".jpeg"] = ("image/jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } }),
        [".png"] = ("image/png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } }),
        [".webp"] = ("image/webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } }), // "RIFF"
        [".pdf"] = ("application/pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } }), // "%PDF"
    };

    private readonly string _root;
    private readonly long _maxBytes;

    public LocalReceiptStorage(IWebHostEnvironment environment, IOptions<ReceiptStorageOptions> options)
    {
        var configured = options.Value.Path;
        _root = System.IO.Path.IsPathRooted(configured)
            ? configured
            : System.IO.Path.GetFullPath(System.IO.Path.Combine(environment.ContentRootPath, configured));
        _maxBytes = options.Value.MaxBytes;

        var wwwRoot = System.IO.Path.GetFullPath(environment.WebRootPath);
        if (_root.StartsWith(wwwRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Receipt storage must not be inside wwwroot; receipts are private financial documents.");
    }

    public async Task<string?> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default)
    {
        var extension = System.IO.Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(extension) || !Allowed.TryGetValue(extension, out var kind))
            return null;

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0 || buffer.Length > _maxBytes)
            return null;

        var bytes = buffer.ToArray();
        if (!kind.Signatures.Any(sig => bytes.Take(sig.Length).SequenceEqual(sig)))
            return null;

        Directory.CreateDirectory(_root);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await File.WriteAllBytesAsync(System.IO.Path.Combine(_root, fileName), bytes, cancellationToken);
        return fileName;
    }

    public Task<(Stream Content, string ContentType)?> OpenAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        // Only bare file names produced by SaveAsync are accepted; no directory traversal.
        if (string.IsNullOrEmpty(storedPath)
            || storedPath != System.IO.Path.GetFileName(storedPath)
            || !Allowed.TryGetValue(System.IO.Path.GetExtension(storedPath), out var kind))
        {
            return Task.FromResult<(Stream Content, string ContentType)?>(null);
        }

        var fullPath = System.IO.Path.Combine(_root, storedPath);
        if (!File.Exists(fullPath))
            return Task.FromResult<(Stream Content, string ContentType)?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<(Stream Content, string ContentType)?>((stream, kind.ContentType));
    }
}
