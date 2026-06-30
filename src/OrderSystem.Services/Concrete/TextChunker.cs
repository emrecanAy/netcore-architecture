using OrderSystem.Common;
using OrderSystem.Services.Abstract;

namespace OrderSystem.Services.Concrete;

/// <summary>
/// Greedy sliding-window chunker. Cuts near the configured size but backtracks to
/// the nearest paragraph or sentence boundary so chunks stay coherent, and carries
/// a configurable overlap between consecutive chunks to preserve context across cuts.
/// Deterministic, so it is straightforward to unit test.
/// </summary>
public class TextChunker : ITextChunker
{
    private readonly int _chunkSize;
    private readonly int _overlap;

    public TextChunker(AppSettings appSettings)
    {
        _chunkSize = Math.Max(1, appSettings.Rag.ChunkSize);
        // Overlap must leave forward progress.
        _overlap = Math.Clamp(appSettings.Rag.ChunkOverlap, 0, _chunkSize - 1);
    }

    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        var normalized = Normalize(text);
        if (normalized.Length == 0)
            return Array.Empty<TextChunk>();

        var chunks = new List<TextChunk>();
        var position = 0;
        var ordinal = 0;

        while (position < normalized.Length)
        {
            var remaining = normalized.Length - position;
            var take = Math.Min(_chunkSize, remaining);
            var end = position + take;

            // If we're cutting mid-text, prefer a natural boundary in the back half.
            if (end < normalized.Length)
                end = FindBoundary(normalized, position, end);

            var content = normalized[position..end].Trim();
            if (content.Length > 0)
            {
                chunks.Add(new TextChunk(ordinal, content, EstimateTokens(content)));
                ordinal++;
            }

            if (end >= normalized.Length)
                break;

            // Step forward, keeping the overlap; always make progress.
            var next = end - _overlap;
            position = next > position ? next : end;
        }

        return chunks;
    }

    private static int FindBoundary(string text, int start, int end)
    {
        var minBoundary = start + (end - start) / 2; // never cut before the back half

        var paragraph = text.LastIndexOf("\n\n", end - 1, end - minBoundary, StringComparison.Ordinal);
        if (paragraph >= minBoundary)
            return paragraph + 2;

        for (var i = end - 1; i >= minBoundary; i--)
        {
            var c = text[i];
            if (c == '\n' || ((c == '.' || c == '!' || c == '?') && i + 1 < text.Length && char.IsWhiteSpace(text[i + 1])))
                return i + 1;
        }

        return end;
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }

    // Rough heuristic: ~4 characters per token.
    private static int EstimateTokens(string content) => (content.Length + 3) / 4;
}
