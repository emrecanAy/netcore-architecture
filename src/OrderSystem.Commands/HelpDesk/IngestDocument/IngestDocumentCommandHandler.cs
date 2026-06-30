using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.HelpDesk.IngestDocument;

/// <summary>
/// Orchestration only — the document rules live in KnowledgeDocument.Create.
/// Behaves as an upsert by file name: re-uploading a file with the same name
/// replaces the previous document (and its chunks + vectors), so the knowledge
/// base never accumulates stale duplicates.
/// </summary>
public class IngestDocumentCommandHandler : IRequestHandler<IngestDocumentCommand, DocumentDto>
{
    private readonly ISQLRepository<KnowledgeDocument> _documents;
    private readonly ISQLRepository<DocumentChunk> _chunks;
    private readonly IVectorStore _vectors;

    public IngestDocumentCommandHandler(
        ISQLRepository<KnowledgeDocument> documents,
        ISQLRepository<DocumentChunk> chunks,
        IVectorStore vectors)
    {
        _documents = documents;
        _chunks = chunks;
        _vectors = vectors;
    }

    public async Task<DocumentDto> Handle(IngestDocumentCommand request, CancellationToken cancellationToken)
    {
        await ReplaceExistingAsync(request.FileName, cancellationToken);

        var document = KnowledgeDocument.Create(
            request.Title,
            request.FileName,
            request.ContentType,
            request.Data);

        await _documents.AddAsync(document, cancellationToken);

        // No SaveChanges here — UnitOfWorkBehavior is the single commit point.
        return document.ToDto();
    }

    /// <summary>
    /// Removes any prior document(s) with the same file name, along with their
    /// chunks and vectors, so the new upload takes their place.
    /// </summary>
    private async Task ReplaceExistingAsync(string fileName, CancellationToken cancellationToken)
    {
        // Case-insensitive match so "Iade.pdf" and "iade.pdf" are the same file.
        var existing = await _documents
            .Where(d => EF.Functions.Collate(d.FileName, "NOCASE") == fileName)
            .ToListAsync(cancellationToken);
        if (existing.Count == 0)
            return;

        var documentIds = existing.Select(d => d.Id).ToList();
        var staleChunks = await _chunks
            .Where(c => documentIds.Contains(c.DocumentId))
            .ToListAsync(cancellationToken);

        await _vectors.DeleteByDocumentAsync(staleChunks.Select(c => c.Id), cancellationToken);

        foreach (var chunk in staleChunks)
            _chunks.Remove(chunk);
        foreach (var document in existing)
            _documents.Remove(document);
    }
}
