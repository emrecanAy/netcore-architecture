namespace OrderSystem.Services.Abstract;

/// <summary>
/// Extracts plain text from an uploaded document, dispatching by content type
/// (PDF, Word, plain text/markdown).
/// </summary>
public interface IDocumentTextExtractor
{
    /// <summary>True if this extractor understands the given file.</summary>
    bool CanExtract(string contentType, string fileName);

    /// <summary>Extracts plain text; throws if the content type is unsupported.</summary>
    string Extract(string contentType, string fileName, byte[] data);
}
