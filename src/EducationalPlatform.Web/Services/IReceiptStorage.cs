namespace EducationalPlatform.Web.Services;

/// <summary>
/// Stores payment receipt images OUTSIDE the public web root. Receipts are
/// financial proofs and can contain sensitive data, so they are only served
/// through an authorized controller action, never as static files.
/// </summary>
public interface IReceiptStorage
{
    /// <summary>
    /// Validates and stores an uploaded receipt. Returns the storage-relative
    /// path to persist on the payment request, or null when the file is
    /// rejected (unsupported type, bad content, or too large).
    /// </summary>
    Task<string?> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default);

    /// <summary>Opens a stored receipt by its storage-relative path, or null when not found.</summary>
    Task<(Stream Content, string ContentType)?> OpenAsync(string storedPath, CancellationToken cancellationToken = default);
}
