namespace OrderSystem.Repositories.Abstract;

/// <summary>
/// Storage and nearest-neighbour search for chunk embeddings, keyed by the
/// chunk's id. Kept as an abstraction so the backing index (sqlite-vec today)
/// can be swapped without touching ingestion or retrieval.
/// </summary>
public interface IVectorStore
{
    /// <summary>Stores (or replaces) the embedding for a chunk.</summary>
    Task UpsertAsync(Guid chunkId, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <paramref name="k"/> closest chunks to <paramref name="query"/>,
    /// nearest first, each with a similarity score in [0, 1] (1 = identical).
    /// </summary>
    Task<IReadOnlyList<VectorMatch>> SearchAsync(
        ReadOnlyMemory<float> query,
        int k,
        CancellationToken cancellationToken = default);

    /// <summary>Removes every embedding belonging to a document.</summary>
    Task DeleteByDocumentAsync(IEnumerable<Guid> chunkIds, CancellationToken cancellationToken = default);
}

/// <summary>A retrieval hit: the chunk id and its similarity score.</summary>
public readonly record struct VectorMatch(Guid ChunkId, double Score);
