using OrderSystem.Common;
using OrderSystem.Services.Concrete;
using Xunit;

namespace OrderSystem.UnitTests.HelpDesk;

public class TextChunkerTests
{
    private static TextChunker Chunker(int size = 60, int overlap = 15) =>
        new(new AppSettings { Rag = new RagSettings { ChunkSize = size, ChunkOverlap = overlap } });

    [Fact]
    public void Empty_text_yields_no_chunks()
    {
        Assert.Empty(Chunker().Chunk("   "));
    }

    [Fact]
    public void Short_text_yields_a_single_chunk()
    {
        var chunks = Chunker().Chunk("A short knowledge base note.");

        Assert.Single(chunks);
        Assert.Equal(0, chunks[0].Ordinal);
        Assert.Equal("A short knowledge base note.", chunks[0].Content);
    }

    [Fact]
    public void Long_text_splits_into_ordered_chunks()
    {
        var text = string.Join(" ", Enumerable.Range(0, 60).Select(i => $"word{i}"));

        var chunks = Chunker(size: 60, overlap: 15).Chunk(text);

        Assert.True(chunks.Count > 1);
        for (var i = 0; i < chunks.Count; i++)
            Assert.Equal(i, chunks[i].Ordinal);
    }

    [Fact]
    public void Chunking_is_deterministic()
    {
        var text = string.Join(" ", Enumerable.Range(0, 80).Select(i => $"token{i}"));

        var first = Chunker().Chunk(text).Select(c => c.Content).ToArray();
        var second = Chunker().Chunk(text).Select(c => c.Content).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Token_estimate_is_populated()
    {
        var chunks = Chunker().Chunk("Some content that should produce a positive token estimate.");

        Assert.All(chunks, c => Assert.True(c.TokenEstimate > 0));
    }
}
