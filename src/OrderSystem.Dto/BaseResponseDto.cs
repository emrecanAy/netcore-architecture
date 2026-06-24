namespace OrderSystem.Dto;

/// <summary>Common envelope fields returned for every persisted entity.</summary>
public abstract class BaseResponseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
