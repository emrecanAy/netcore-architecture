using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSystem.Commands.HelpDesk.DeleteDocument;
using OrderSystem.Commands.HelpDesk.IngestDocument;
using OrderSystem.Dto;
using OrderSystem.Queries.HelpDesk.AskQuestion;
using OrderSystem.Queries.HelpDesk.GetDocument;
using OrderSystem.Queries.HelpDesk.ListDocuments;

namespace OrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelpDeskController : ControllerBase
{
    private readonly IMediator _mediator;

    public HelpDeskController(IMediator mediator) => _mediator = mediator;

    /// <summary>Uploads a document into the knowledge base for asynchronous ingestion.</summary>
    [HttpPost("documents")]
    public async Task<ActionResult<DocumentDto>> Upload(IFormFile file, [FromForm] string? title = null)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "A non-empty file is required." });

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        var documentTitle = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(file.FileName)
            : title;

        var command = new IngestDocumentCommand(documentTitle, file.FileName, file.ContentType, stream.ToArray());
        var document = await _mediator.Send(command);

        // Ingestion is asynchronous: the document is accepted as Pending.
        return AcceptedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    [HttpGet("documents")]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> List() =>
        Ok(await _mediator.Send(new ListDocumentsQuery()));

    [HttpGet("documents/{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id) =>
        Ok(await _mediator.Send(new GetDocumentQuery(id)));

    [HttpDelete("documents/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteDocumentCommand(id));
        return NoContent();
    }

    /// <summary>Answers a question strictly from the indexed knowledge base.</summary>
    [HttpPost("ask")]
    public async Task<ActionResult<AskAnswerDto>> Ask(AskQuestionQuery query) =>
        Ok(await _mediator.Send(query));
}
