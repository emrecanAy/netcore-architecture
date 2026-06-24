using MediatR;
using OrderSystem.Models.Concrete;

namespace OrderSystem.Models.DomainEvents;

/// <summary>Raised whenever a product's stock is reduced. Carries the delta and remainder.</summary>
public sealed class ProductStockDecreasedDomainEvent : INotification
{
    public ProductStockDecreasedDomainEvent(Product product, int decreasedBy, int remainingStock)
    {
        Product = product;
        DecreasedBy = decreasedBy;
        RemainingStock = remainingStock;
    }

    public Product Product { get; }
    public int DecreasedBy { get; }
    public int RemainingStock { get; }
}
