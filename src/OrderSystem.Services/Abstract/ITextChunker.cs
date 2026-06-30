namespace OrderSystem.Services.Abstract;

/// <summary>
/// Splits extracted text into overlapping chunks sized for embedding and retrieval.
/// </summary>
public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text);
}

/// <summary>A single chunk produced by <see cref="ITextChunker"/>.</summary>
/// <param name="Ordinal">Zero-based position within the source document.</param>
/// <param name="Content">The chunk text.</param>
/// <param name="TokenEstimate">Rough token count for context budgeting.</param>
public readonly record struct TextChunk(int Ordinal, string Content, int TokenEstimate);
