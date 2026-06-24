namespace OrderSystem.Repositories.Outbox;

/// <summary>
/// Transactional outbox row. Domain events are serialized here in the same
/// transaction that persists the state change (see PROJECT.md §15.4), then a
/// background dispatcher publishes them reliably and marks them processed.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Assembly-qualified type name of the serialized notification.</summary>
    public string Type { get; set; } = default!;

    /// <summary>JSON payload of the notification.</summary>
    public string Content { get; set; } = default!;

    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }
}
