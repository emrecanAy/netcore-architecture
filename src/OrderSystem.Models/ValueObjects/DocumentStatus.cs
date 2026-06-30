using OrderSystem.Common.ValueObjects;

namespace OrderSystem.Models.ValueObjects;

/// <summary>
/// Enum-like value object for a knowledge document's ingestion lifecycle.
/// Pending → Processing → Indexed (or Failed). Using a value object instead of a
/// raw string keeps invalid statuses out at compile time, mirroring
/// <see cref="OrderStatus"/>.
/// </summary>
public sealed class DocumentStatus : StringValueObject
{
    /// <summary>Uploaded and waiting for the ingestion worker to pick it up.</summary>
    public static readonly DocumentStatus Pending = new("Pending");

    /// <summary>The worker is extracting, chunking and embedding the document.</summary>
    public static readonly DocumentStatus Processing = new("Processing");

    /// <summary>Fully chunked and embedded; available for retrieval.</summary>
    public static readonly DocumentStatus Indexed = new("Indexed");

    /// <summary>Ingestion failed; see the document's error message.</summary>
    public static readonly DocumentStatus Failed = new("Failed");

    private static readonly IReadOnlyDictionary<string, DocumentStatus> All =
        new[] { Pending, Processing, Indexed, Failed }
            .ToDictionary(s => s.Value, StringComparer.OrdinalIgnoreCase);

    private DocumentStatus(string value) : base(value)
    {
    }

    /// <summary>Rebuilds a status from its stored string (used by EF conversion).</summary>
    public static DocumentStatus FromValue(string value)
    {
        if (All.TryGetValue(value, out var status))
            return status;
        throw new ArgumentException($"Unknown document status: '{value}'.", nameof(value));
    }
}
