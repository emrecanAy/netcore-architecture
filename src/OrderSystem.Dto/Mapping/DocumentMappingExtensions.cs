using OrderSystem.Models.Concrete;

namespace OrderSystem.Dto.Mapping;

/// <summary>
/// Hand-written entity-to-DTO mapping for knowledge documents, matching the
/// explicit, allocation-light style used elsewhere.
/// </summary>
public static class DocumentMappingExtensions
{
    public static DocumentDto ToDto(this KnowledgeDocument document) => new()
    {
        Id = document.Id,
        CreatedDate = document.CreatedDate,
        UpdatedDate = document.UpdatedDate,
        Title = document.Title,
        FileName = document.FileName,
        ContentType = document.ContentType,
        Status = document.Status.Value,
        ChunkCount = document.ChunkCount,
        ErrorMessage = document.ErrorMessage,
    };

    public static IEnumerable<DocumentDto> ToDto(this IEnumerable<KnowledgeDocument> documents) =>
        documents.Select(d => d.ToDto());
}
