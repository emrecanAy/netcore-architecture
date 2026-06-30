using MediatR;
using OrderSystem.Dto;

namespace OrderSystem.Commands.HelpDesk.IngestDocument;

/// <summary>
/// Uploads a document into the help-desk knowledge base. The command only stores
/// the source bytes and marks the document Pending; the ingestion worker extracts,
/// chunks and embeds it asynchronously.
/// </summary>
public record IngestDocumentCommand(string Title, string FileName, string ContentType, byte[] Data)
    : IRequest<DocumentDto>;
