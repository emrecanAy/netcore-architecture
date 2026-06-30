using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.HelpDesk.DeleteDocument;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly ISQLRepository<KnowledgeDocument> _documents;
    private readonly ISQLRepository<DocumentChunk> _chunks;
    private readonly IVectorStore _vectors;

    public DeleteDocumentCommandHandler(
        ISQLRepository<KnowledgeDocument> documents,
        ISQLRepository<DocumentChunk> chunks,
        IVectorStore vectors)
    {
        _documents = documents;
        _chunks = chunks;
        _vectors = vectors;
    }

    public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _documents.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.Id} not found.");

        var chunks = await _chunks.Where(c => c.DocumentId == request.Id).ToListAsync(cancellationToken);

        await _vectors.DeleteByDocumentAsync(chunks.Select(c => c.Id), cancellationToken);

        foreach (var chunk in chunks)
            _chunks.Remove(chunk);
        _documents.Remove(document);

        // No SaveChanges here — UnitOfWorkBehavior is the single commit point.
    }
}
