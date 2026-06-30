using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;
using OrderSystem.Repositories.Abstract;
using OrderSystem.Repositories.Concrete;
using OrderSystem.Services.Abstract;

namespace OrderSystem.Services.Rag;

/// <summary>
/// Asynchronously ingests uploaded documents (mirrors the OutboxDispatcher pattern):
/// the upload only stores bytes and marks the document Pending; this worker polls
/// pending rows and does the heavy lifting — extract text, chunk it, embed the
/// chunks via the AI provider, persist the chunks and index their vectors — so the
/// upload request stays fast and failures don't block the API.
/// </summary>
public class DocumentIngestionWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private const int BatchSize = 5;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentIngestionWorker> _logger;

    public DocumentIngestionWorker(IServiceScopeFactory scopeFactory, ILogger<DocumentIngestionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Document ingestion batch failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.KnowledgeDocuments
            .Where(d => d.Status == DocumentStatus.Pending)
            .OrderBy(d => d.CreatedDate)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        var extractor = scope.ServiceProvider.GetRequiredService<IDocumentTextExtractor>();
        var chunker = scope.ServiceProvider.GetRequiredService<ITextChunker>();
        var embeddings = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var vectors = scope.ServiceProvider.GetRequiredService<IVectorStore>();
        var chunks = scope.ServiceProvider.GetRequiredService<ISQLRepository<DocumentChunk>>();

        foreach (var document in pending)
        {
            try
            {
                await IngestAsync(document, extractor, chunker, embeddings, vectors, chunks, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest document {DocumentId} ({FileName}).", document.Id, document.FileName);
                document.MarkFailed(ex.Message);
            }

            // Persist each document's outcome independently so one failure doesn't
            // discard the rest of the batch.
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task IngestAsync(
        KnowledgeDocument document,
        IDocumentTextExtractor extractor,
        ITextChunker chunker,
        IEmbeddingService embeddings,
        IVectorStore vectors,
        ISQLRepository<DocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        document.MarkProcessing();

        var data = document.RawData
            ?? throw new InvalidOperationException("Document has no stored content to ingest.");

        var text = extractor.Extract(document.ContentType, document.FileName, data);
        var pieces = chunker.Chunk(text);
        if (pieces.Count == 0)
        {
            document.MarkFailed("No extractable text was found in the document.");
            return;
        }

        var vectorList = await embeddings.EmbedBatchAsync(
            pieces.Select(p => p.Content).ToList(), cancellationToken);

        for (var i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            var chunk = new DocumentChunk(document.Id, piece.Ordinal, piece.Content, piece.TokenEstimate);

            // Index the vector first; an orphaned vector (if the later save fails) is
            // harmless because retrieval loads chunk text from the relational table.
            await vectors.UpsertAsync(chunk.Id, vectorList[i], cancellationToken);
            await chunks.AddAsync(chunk, cancellationToken);
        }

        document.MarkIndexed(pieces.Count);
    }
}
