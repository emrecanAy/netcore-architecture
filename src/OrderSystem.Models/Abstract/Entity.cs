namespace OrderSystem.Models.Abstract;

/// <summary>
/// Base for all domain entities. Holds identity, audit fields and soft-delete.
/// State changes only through behavior methods — note the <c>protected set</c>
/// accessors, which enforce encapsulation and guard against an anemic model.
/// </summary>
public abstract class Entity : EntityDomainEvent
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedDate { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; protected set; }

    public bool IsDeleted { get; protected set; }
    public Guid? DeletedByUserId { get; protected set; }
    public DateTime? DeletedDate { get; protected set; }

    /// <summary>Marks the entity as touched; call from behavior methods on mutation.</summary>
    protected void Touch() => UpdatedDate = DateTime.UtcNow;

    public void Delete(Guid? deletedByUserId = null)
    {
        IsDeleted = true;
        DeletedDate = DateTime.UtcNow;
        DeletedByUserId = deletedByUserId;
    }
}
