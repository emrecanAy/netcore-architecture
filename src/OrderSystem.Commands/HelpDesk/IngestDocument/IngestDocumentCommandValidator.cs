using FluentValidation;
using OrderSystem.Common;
using OrderSystem.Services.Abstract;

namespace OrderSystem.Commands.HelpDesk.IngestDocument;

public class IngestDocumentCommandValidator : AbstractValidator<IngestDocumentCommand>
{
    public IngestDocumentCommandValidator(IDocumentTextExtractor extractor, AppSettings appSettings)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);

        RuleFor(x => x.Data)
            .NotEmpty().WithMessage("The uploaded document is empty.")
            .Must(data => data.LongLength <= appSettings.Rag.MaxUploadBytes)
            .WithMessage($"The document exceeds the {appSettings.Rag.MaxUploadBytes} byte upload limit.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must((command, contentType) => extractor.CanExtract(contentType, command.FileName))
            .WithMessage("Unsupported document type. Allowed: PDF, Word (.docx), text and markdown.");
    }
}
