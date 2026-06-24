using MediatR;
using OrderSystem.Models.Concrete;

namespace OrderSystem.Models.DomainEvents;

/// <summary>Raised when a product's stock reaches zero, so the rest of the domain can react.</summary>
public sealed class ProductOutOfStockDomainEvent : INotification
{
    public ProductOutOfStockDomainEvent(Product product) => Product = product;

    public Product Product { get; }
}
