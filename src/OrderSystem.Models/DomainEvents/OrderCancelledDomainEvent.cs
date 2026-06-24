using MediatR;
using OrderSystem.Models.Concrete;

namespace OrderSystem.Models.DomainEvents;

/// <summary>Raised when an order is cancelled.</summary>
public sealed class OrderCancelledDomainEvent : INotification
{
    public OrderCancelledDomainEvent(Order order) => Order = order;

    public Order Order { get; }
}
