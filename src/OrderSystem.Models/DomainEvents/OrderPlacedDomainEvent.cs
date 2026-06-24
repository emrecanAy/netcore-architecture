using MediatR;
using OrderSystem.Models.Concrete;

namespace OrderSystem.Models.DomainEvents;

/// <summary>Raised when a draft order is placed. Drives stock reduction and confirmation email.</summary>
public sealed class OrderPlacedDomainEvent : INotification
{
    public OrderPlacedDomainEvent(Order order) => Order = order;

    public Order Order { get; }
}
