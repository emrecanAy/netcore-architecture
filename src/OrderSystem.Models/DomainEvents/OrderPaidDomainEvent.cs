using MediatR;
using OrderSystem.Models.Concrete;

namespace OrderSystem.Models.DomainEvents;

/// <summary>Raised when an order is paid.</summary>
public sealed class OrderPaidDomainEvent : INotification
{
    public OrderPaidDomainEvent(Order order) => Order = order;

    public Order Order { get; }
}
