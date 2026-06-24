using OrderSystem.Models.Abstract;
using OrderSystem.Models.DomainEvents;
using OrderSystem.Models.ValueObjects;

namespace OrderSystem.Models.Concrete;

/// <summary>
/// Product aggregate root. Business rules (pricing, stock, lifecycle) live here
/// as behavior methods; callers orchestrate, they never reach in and mutate state.
/// </summary>
public class Product : Entity
{
    public string Name { get; protected set; } = default!;
    public Sku Sku { get; protected set; } = default!;
    public Money Price { get; protected set; } = default!;
    public int StockQuantity { get; protected set; }
    public bool IsActive { get; protected set; }

    // EF Core materialization.
    protected Product()
    {
    }

    public Product(string name, Sku sku, Money price, int initialStock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (initialStock < 0)
            throw new ArgumentOutOfRangeException(nameof(initialStock), "Initial stock cannot be negative.");

        Name = name.Trim();
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        Price = price ?? throw new ArgumentNullException(nameof(price));
        StockQuantity = initialStock;
        IsActive = true;
    }

    public void ChangePrice(Money newPrice)
    {
        Price = newPrice ?? throw new ArgumentNullException(nameof(newPrice));
        Touch();
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        StockQuantity += quantity;
        Touch();
    }

    /// <summary>
    /// Reduces stock, raising a stock-decreased event and, if it hits zero,
    /// an out-of-stock event. These events drive cross-aggregate reactions.
    /// </summary>
    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (quantity > StockQuantity)
            throw new InvalidOperationException(
                $"Insufficient stock for '{Name}': requested {quantity}, available {StockQuantity}.");

        StockQuantity -= quantity;
        Touch();

        AddDomainEvent(new ProductStockDecreasedDomainEvent(this, quantity, StockQuantity));

        if (StockQuantity == 0)
            AddDomainEvent(new ProductOutOfStockDomainEvent(this));
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
