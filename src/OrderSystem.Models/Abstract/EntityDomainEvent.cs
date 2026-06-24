using System.Text.Json.Serialization;
using MediatR;

namespace OrderSystem.Models.Abstract;

/// <summary>
/// Gives an entity the ability to <b>accumulate</b> domain events instead of
/// publishing them immediately. The pipeline (UnitOfWorkBehavior) collects and
/// dispatches them right before persistence — see PROJECT.md §6.
/// </summary>
public abstract class EntityDomainEvent
{
    private List<INotification>? _domainEvents;

    [JsonIgnore]
    public IReadOnlyList<INotification> DomainEvents =>
        _domainEvents ?? (IReadOnlyList<INotification>)Array.Empty<INotification>();

    public void AddDomainEvent(INotification domainEvent)
    {
        _domainEvents ??= new List<INotification>();
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(INotification domainEvent) => _domainEvents?.Remove(domainEvent);

    public void ClearDomainEvents() => _domainEvents?.Clear();
}
