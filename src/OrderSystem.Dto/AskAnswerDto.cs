namespace OrderSystem.Dto;

/// <summary>
/// Answer to a help-desk question. When <see cref="HasAnswer"/> is false the
/// question fell outside the knowledge base and no model answer was generated.
/// </summary>
public class AskAnswerDto
{
    public string Answer { get; set; } = default!;
    public bool HasAnswer { get; set; }
    public IReadOnlyList<SourceDto> Sources { get; set; } = Array.Empty<SourceDto>();
}

/// <summary>A retrieved chunk cited as support for an answer.</summary>
public class SourceDto
{
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = default!;
    public int ChunkOrdinal { get; set; }
    public string Excerpt { get; set; } = default!;
    public double Score { get; set; }
}
