using MediatR;

namespace OrderSystem.Commands.HelpDesk.DeleteDocument;

/// <summary>Removes a document from the knowledge base along with its chunks and vectors.</summary>
public record DeleteDocumentCommand(Guid Id) : IRequest;
