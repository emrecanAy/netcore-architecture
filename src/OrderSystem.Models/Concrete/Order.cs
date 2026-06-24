using OrderSystem.Models.Abstract;
using OrderSystem.Models.DomainEvents;
using OrderSystem.Models.ValueObjects;

namespace OrderSystem.Models.Concrete;

/// <summary>
/// Order aggregate root. Owns its items and guards the lifecycle: items can only
/// be edited while the order is a draft, and each state transition is a named
/// behavior that emits a domain event rather than a public setter.
/// </summary>
public class Order : Entity
{
    private readonly List<OrderItem> _items = new();

    public Guid CustomerId { get; protected set; }
    public OrderStatus Status { get; protected set; } = default!;
    public Money TotalAmount { get; protected set; } = default!;

    /// <summary>The order's currency, taken from its monetary total.</summary>
    public int CurrencyId => TotalAmount.CurrencyId;

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // EF Core materialization.
    protected Order()
    {
    }

    public Order(Guid customerId, int currencyId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (currencyId <= 0)
            throw new ArgumentOutOfRangeException(nameof(currencyId), "A valid currency is required.");

        CustomerId = customerId;
        Status = OrderStatus.Draft;
        TotalAmount = Money.Zero(currencyId);
    }

    public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        EnsureDraft();
        if (unitPrice is null)
            throw new ArgumentNullException(nameof(unitPrice));
        if (unitPrice.CurrencyId != CurrencyId)
            throw new InvalidOperationException(
                $"Item currency {unitPrice.CurrencyId} does not match order currency {CurrencyId}.");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
            existing.IncreaseQuantity(quantity);
        else
            _items.Add(new OrderItem(productId, productName, unitPrice, quantity));

        RecalculateTotal();
        Touch();
    }

    public void RemoveItem(Guid productId)
    {
        EnsureDraft();
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
            return;

        _items.Remove(item);
        RecalculateTotal();
        Touch();
    }

    public void Place()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot place an order with no items.");

        Status = OrderStatus.Placed;
        Touch();
        AddDomainEvent(new OrderPlacedDomainEvent(this));
    }

    public void Pay()
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException($"Only a placed order can be paid (current: {Status}).");

        Status = OrderStatus.Paid;
        Touch();
        AddDomainEvent(new OrderPaidDomainEvent(this));
    }

    public void Ship()
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOperationException($"Only a paid order can be shipped (current: {Status}).");

        Status = OrderStatus.Shipped;
        Touch();
        AddDomainEvent(new OrderShippedDomainEvent(this));
    }

    public void Complete()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException($"Only a shipped order can be completed (current: {Status}).");

        Status = OrderStatus.Completed;
        Touch();
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Completed)
            throw new InvalidOperationException($"A {Status} order cannot be cancelled.");
        if (Status == OrderStatus.Cancelled)
            return; // idempotent

        Status = OrderStatus.Cancelled;
        Touch();
        AddDomainEvent(new OrderCancelledDomainEvent(this));
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Items can only change while the order is a draft (current: {Status}).");
    }

    private void RecalculateTotal() =>
        TotalAmount = _items.Aggregate(Money.Zero(CurrencyId), (sum, item) => sum.Add(item.LineTotal));
}
