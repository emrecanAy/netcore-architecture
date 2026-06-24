using MediatR;
using OrderSystem.Models.Concrete;

namespace OrderSystem.Models.DomainEvents;

/// <summary>Raised when a paid order is shipped.</summary>
public sealed class OrderShippedDomainEvent : INotification
{
    public OrderShippedDomainEvent(Order order) => Order = order;

    public Order Order { get; }
}
