namespace OrderSystem.Dto;

public class DocumentDto : BaseResponseDto
{
    public string Title { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int ChunkCount { get; set; }
    public string? ErrorMessage { get; set; }
}
