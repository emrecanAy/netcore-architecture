using OrderSystem.Models.Abstract;
using OrderSystem.Models.ValueObjects;

namespace OrderSystem.Models.Concrete;

/// <summary>
/// Knowledge-base document aggregate root. Holds the uploaded source and its
/// ingestion lifecycle; the actual retrievable text lives in its
/// <see cref="DocumentChunk"/> children, embedded in the vector store.
/// Ingestion is asynchronous: the upload only stores the bytes and marks the
/// document <see cref="DocumentStatus.Pending"/>; the ingestion worker drives the
/// rest through the behavior methods here.
/// </summary>
public class KnowledgeDocument : Entity
{
    public string Title { get; protected set; } = default!;
    public string FileName { get; protected set; } = default!;
    public string ContentType { get; protected set; } = default!;
    public DocumentStatus Status { get; protected set; } = DocumentStatus.Pending;
    public int ChunkCount { get; protected set; }
    public string? ErrorMessage { get; protected set; }

    /// <summary>
    /// Original uploaded bytes, kept only until indexing succeeds, then cleared to
    /// reclaim space (the chunks are the durable artifact, not the source file).
    /// </summary>
    public byte[]? RawData { get; protected set; }

    // EF Core materialization.
    protected KnowledgeDocument()
    {
    }

    public static KnowledgeDocument Create(string title, string fileName, string contentType, byte[] rawData)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Document title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));
        if (rawData is null || rawData.Length == 0)
            throw new ArgumentException("Uploaded document is empty.", nameof(rawData));

        return new KnowledgeDocument
        {
            Title = title.Trim(),
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            Status = DocumentStatus.Pending,
            RawData = rawData,
        };
    }

    /// <summary>Claims the document for ingestion.</summary>
    public void MarkProcessing()
    {
        Status = DocumentStatus.Processing;
        ErrorMessage = null;
        Touch();
    }

    /// <summary>Marks ingestion complete and releases the source bytes.</summary>
    public void MarkIndexed(int chunkCount)
    {
        if (chunkCount < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkCount), "Chunk count cannot be negative.");

        Status = DocumentStatus.Indexed;
        ChunkCount = chunkCount;
        ErrorMessage = null;
        RawData = null;
        Touch();
    }

    /// <summary>Records an ingestion failure; the source bytes are kept for a retry.</summary>
    public void MarkFailed(string error)
    {
        Status = DocumentStatus.Failed;
        ErrorMessage = string.IsNullOrWhiteSpace(error) ? "Ingestion failed." : error.Trim();
        Touch();
    }
}
