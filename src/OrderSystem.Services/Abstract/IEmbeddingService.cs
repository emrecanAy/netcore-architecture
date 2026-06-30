namespace OrderSystem.Services.Abstract;

/// <summary>
/// Turns text into embedding vectors. One implementation per provider; the rest
/// of the system depends only on this abstraction.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Embeds a single piece of text (e.g. a user's question).</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Embeds many texts (e.g. a document's chunks) in as few calls as possible.</summary>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
