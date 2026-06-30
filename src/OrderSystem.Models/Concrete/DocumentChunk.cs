using OrderSystem.Models.Abstract;

namespace OrderSystem.Models.Concrete;

/// <summary>
/// A retrievable slice of a <see cref="KnowledgeDocument"/>. Modeled as its own
/// entity (not navigated from the document aggregate) because retrieval queries
/// span chunks across every document. The chunk's <see cref="Entity.Id"/> is the
/// key under which its embedding is stored in the vector store.
/// </summary>
public class DocumentChunk : Entity
{
    public Guid DocumentId { get; protected set; }

    /// <summary>Zero-based position of this chunk within its source document.</summary>
    public int Ordinal { get; protected set; }

    public string Content { get; protected set; } = default!;

    /// <summary>Rough token count, used for budgeting context sent to the model.</summary>
    public int TokenEstimate { get; protected set; }

    // EF Core materialization.
    protected DocumentChunk()
    {
    }

    public DocumentChunk(Guid documentId, int ordinal, string content, int tokenEstimate)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("Document id is required.", nameof(documentId));
        if (ordinal < 0)
            throw new ArgumentOutOfRangeException(nameof(ordinal), "Ordinal cannot be negative.");
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Chunk content is required.", nameof(content));

        DocumentId = documentId;
        Ordinal = ordinal;
        Content = content;
        TokenEstimate = tokenEstimate < 0 ? 0 : tokenEstimate;
    }
}
