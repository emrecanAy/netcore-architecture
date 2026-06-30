using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Queries.HelpDesk.ListDocuments;

public record ListDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>;

public class ListDocumentsQueryHandler : IRequestHandler<ListDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    private readonly ISQLRepository<KnowledgeDocument> _documents;

    public ListDocumentsQueryHandler(ISQLRepository<KnowledgeDocument> documents) => _documents = documents;

    public async Task<IReadOnlyList<DocumentDto>> Handle(ListDocumentsQuery request, CancellationToken cancellationToken)
    {
        // RawData is cleared once a document is indexed, so listing stays light for
        // the common case; only still-pending/failed uploads carry their bytes.
        var documents = await _documents.AsNoTracking()
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync(cancellationToken);

        return documents.ToDto().ToList();
    }
}
