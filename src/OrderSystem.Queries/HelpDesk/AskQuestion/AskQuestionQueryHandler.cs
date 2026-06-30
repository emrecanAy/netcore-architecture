using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Common;
using OrderSystem.Dto;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;
using OrderSystem.Services.Abstract;

namespace OrderSystem.Queries.HelpDesk.AskQuestion;

public record AskQuestionQuery(string Question, int? TopK = null) : IRequest<AskAnswerDto>;

/// <summary>
/// The retrieval-augmented question handler. It embeds the question, retrieves the
/// nearest chunks, and only then asks the model to answer — strictly from that
/// retrieved context. If nothing clears the relevance threshold the model is never
/// called, so the help-desk can only answer from our own documents.
/// </summary>
public class AskQuestionQueryHandler : IRequestHandler<AskQuestionQuery, AskAnswerDto>
{
    private const string SystemPrompt =
        "You are a help-desk assistant. Answer the user's question using ONLY the numbered context " +
        "passages provided. Do not use outside knowledge or make assumptions. If the answer is not " +
        "contained in the context, reply that the information is not available in the knowledge base. " +
        "Cite the sources you used as [Source N]. Respond in the same language as the question.";

    private const int ExcerptLength = 240;

    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectors;
    private readonly IChatCompletionService _chat;
    private readonly ISQLRepository<DocumentChunk> _chunks;
    private readonly ISQLRepository<KnowledgeDocument> _documents;
    private readonly RagSettings _settings;

    public AskQuestionQueryHandler(
        IEmbeddingService embeddings,
        IVectorStore vectors,
        IChatCompletionService chat,
        ISQLRepository<DocumentChunk> chunks,
        ISQLRepository<KnowledgeDocument> documents,
        AppSettings appSettings)
    {
        _embeddings = embeddings;
        _vectors = vectors;
        _chat = chat;
        _chunks = chunks;
        _documents = documents;
        _settings = appSettings.Rag;
    }

    public async Task<AskAnswerDto> Handle(AskQuestionQuery request, CancellationToken cancellationToken)
    {
        var k = request.TopK is > 0 ? request.TopK.Value : _settings.TopK;

        var queryVector = await _embeddings.EmbedAsync(request.Question, cancellationToken);
        var matches = await _vectors.SearchAsync(queryVector, k, cancellationToken);

        var relevant = matches.Where(m => m.Score >= _settings.MinScore).ToList();
        if (relevant.Count == 0)
            return NotFound();

        var scoreById = relevant.ToDictionary(m => m.ChunkId, m => m.Score);
        var ids = scoreById.Keys.ToList();

        var chunks = await _chunks.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
        if (chunks.Count == 0)
            return NotFound();

        var docIds = chunks.Select(c => c.DocumentId).Distinct().ToList();
        var titles = await _documents.AsNoTracking()
            .Where(d => docIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Title, cancellationToken);

        // Best matches first; this is also the citation order shown to the model.
        var ordered = chunks.OrderByDescending(c => scoreById[c.Id]).ToList();

        var answer = await _chat.CompleteAsync(SystemPrompt, BuildUserPrompt(request.Question, ordered, titles), cancellationToken);

        return new AskAnswerDto
        {
            Answer = answer,
            HasAnswer = true,
            Sources = BuildSources(ordered, titles, scoreById),
        };
    }

    private static AskAnswerDto NotFound() => new()
    {
        HasAnswer = false,
        Answer = "I couldn't find anything about that in the knowledge base.",
        Sources = Array.Empty<SourceDto>(),
    };

    private static string BuildUserPrompt(
        string question,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyDictionary<Guid, string> titles)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Context:");
        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var title = titles.TryGetValue(chunk.DocumentId, out var t) ? t : "Untitled";
            builder.AppendLine($"[Source {i + 1}: {title}]");
            builder.AppendLine(chunk.Content);
            builder.AppendLine();
        }

        builder.AppendLine($"Question: {question}");
        return builder.ToString();
    }

    private static IReadOnlyList<SourceDto> BuildSources(
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyDictionary<Guid, string> titles,
        IReadOnlyDictionary<Guid, double> scoreById) =>
        chunks.Select(chunk => new SourceDto
        {
            DocumentId = chunk.DocumentId,
            DocumentTitle = titles.TryGetValue(chunk.DocumentId, out var t) ? t : "Untitled",
            ChunkOrdinal = chunk.Ordinal,
            Excerpt = chunk.Content.Length <= ExcerptLength ? chunk.Content : chunk.Content[..ExcerptLength] + "…",
            Score = scoreById[chunk.Id],
        }).ToList();
}
