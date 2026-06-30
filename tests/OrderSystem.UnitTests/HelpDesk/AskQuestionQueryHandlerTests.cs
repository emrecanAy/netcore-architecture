using Moq;
using OrderSystem.Common;
using OrderSystem.Models.Concrete;
using OrderSystem.Queries.HelpDesk.AskQuestion;
using OrderSystem.Repositories.Abstract;
using OrderSystem.Services.Abstract;
using Xunit;

namespace OrderSystem.UnitTests.HelpDesk;

public class AskQuestionQueryHandlerTests
{
    private static AppSettings Settings() =>
        new() { Rag = new RagSettings { TopK = 3, MinScore = 0.5 } };

    private static Mock<IEmbeddingService> Embeddings()
    {
        var mock = new Mock<IEmbeddingService>();
        mock.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f });
        return mock;
    }

    [Fact]
    public async Task Returns_no_answer_and_skips_model_when_nothing_relevant()
    {
        var vectors = new Mock<IVectorStore>();
        vectors.Setup(v => v.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<VectorMatch>());

        var chat = new Mock<IChatCompletionService>();

        var handler = new AskQuestionQueryHandler(
            Embeddings().Object,
            vectors.Object,
            chat.Object,
            TestRepository.Build(Array.Empty<DocumentChunk>()).Object,
            TestRepository.Build(Array.Empty<KnowledgeDocument>()).Object,
            Settings());

        var result = await handler.Handle(new AskQuestionQuery("anything"), CancellationToken.None);

        Assert.False(result.HasAnswer);
        Assert.Empty(result.Sources);
        chat.Verify(
            c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Ignores_matches_below_the_score_threshold()
    {
        var chunk = new DocumentChunk(Guid.NewGuid(), 0, "Irrelevant content", 4);
        var vectors = new Mock<IVectorStore>();
        vectors.Setup(v => v.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new VectorMatch(chunk.Id, 0.2) }); // below MinScore 0.5

        var chat = new Mock<IChatCompletionService>();

        var handler = new AskQuestionQueryHandler(
            Embeddings().Object,
            vectors.Object,
            chat.Object,
            TestRepository.Build(new[] { chunk }).Object,
            TestRepository.Build(Array.Empty<KnowledgeDocument>()).Object,
            Settings());

        var result = await handler.Handle(new AskQuestionQuery("anything"), CancellationToken.None);

        Assert.False(result.HasAnswer);
        chat.Verify(
            c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Grounds_answer_on_retrieved_chunks()
    {
        var document = KnowledgeDocument.Create("Returns Policy", "returns.md", "text/markdown", new byte[] { 1 });
        var chunk = new DocumentChunk(document.Id, 0, "Refunds are issued within 14 days.", 8);

        var vectors = new Mock<IVectorStore>();
        vectors.Setup(v => v.SearchAsync(It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new VectorMatch(chunk.Id, 0.92) });

        var chat = new Mock<IChatCompletionService>();
        chat.Setup(c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Refunds within 14 days. [Source 1]");

        var handler = new AskQuestionQueryHandler(
            Embeddings().Object,
            vectors.Object,
            chat.Object,
            TestRepository.Build(new[] { chunk }).Object,
            TestRepository.Build(new[] { document }).Object,
            Settings());

        var result = await handler.Handle(new AskQuestionQuery("What is the refund window?"), CancellationToken.None);

        Assert.True(result.HasAnswer);
        Assert.Equal("Refunds within 14 days. [Source 1]", result.Answer);
        var source = Assert.Single(result.Sources);
        Assert.Equal(document.Id, source.DocumentId);
        Assert.Equal("Returns Policy", source.DocumentTitle);
        chat.Verify(
            c => c.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
