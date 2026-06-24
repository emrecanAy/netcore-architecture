using OrderSystem.Models.Abstract;
using OrderSystem.Models.ValueObjects;

namespace OrderSystem.Models.Concrete;

/// <summary>
/// A line on an order. Child of the <see cref="Order"/> aggregate — created and
/// mutated only through the order, never on its own. Captures a price snapshot
/// so later product price changes don't rewrite history.
/// </summary>
public class OrderItem : Entity
{
    public Guid OrderId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public string ProductName { get; protected set; } = default!;
    public Money UnitPrice { get; protected set; } = default!;
    public int Quantity { get; protected set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    // EF Core materialization.
    protected OrderItem()
    {
    }

    internal OrderItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required.", nameof(productId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("ProductName is required.", nameof(productName));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        ProductId = productId;
        ProductName = productName.Trim();
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        Quantity = quantity;
    }

    internal void IncreaseQuantity(int by)
    {
        if (by <= 0)
            throw new ArgumentOutOfRangeException(nameof(by), "Quantity must be positive.");
        Quantity += by;
        Touch();
    }
}
