using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Queries.HelpDesk.GetDocument;

public record GetDocumentQuery(Guid Id) : IRequest<DocumentDto>;

public class GetDocumentQueryHandler : IRequestHandler<GetDocumentQuery, DocumentDto>
{
    private readonly ISQLRepository<KnowledgeDocument> _documents;

    public GetDocumentQueryHandler(ISQLRepository<KnowledgeDocument> documents) => _documents = documents;

    public async Task<DocumentDto> Handle(GetDocumentQuery request, CancellationToken cancellationToken)
    {
        var document = await _documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.Id} not found.");

        return document.ToDto();
    }
}
