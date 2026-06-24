using OrderSystem.Models.Concrete;
using OrderSystem.Models.DomainEvents;
using OrderSystem.Models.ValueObjects;
using Xunit;

namespace OrderSystem.UnitTests.Domain;

public class OrderTests
{
    private static readonly Guid Customer = Guid.NewGuid();
    private const int Usd = 1;
    private const int Eur = 2;

    private static Order DraftWithItem()
    {
        var order = new Order(Customer, Usd);
        order.AddItem(Guid.NewGuid(), "Keyboard", new Money(100m, Usd), 2);
        return order;
    }

    [Fact]
    public void AddItem_accumulates_total()
    {
        var order = DraftWithItem();

        Assert.Equal(200m, order.TotalAmount.Amount);
        Assert.Single(order.Items);
    }

    [Fact]
    public void Place_requires_items()
    {
        var order = new Order(Customer, Usd);

        Assert.Throws<InvalidOperationException>(() => order.Place());
    }

    [Fact]
    public void Place_sets_status_and_raises_event()
    {
        var order = DraftWithItem();

        order.Place();

        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Contains(order.DomainEvents, e => e is OrderPlacedDomainEvent);
    }

    [Fact]
    public void Cannot_add_item_after_placed()
    {
        var order = DraftWithItem();
        order.Place();

        Assert.Throws<InvalidOperationException>(() =>
            order.AddItem(Guid.NewGuid(), "Mouse", new Money(10m, Usd), 1));
    }

    [Fact]
    public void Lifecycle_place_pay_ship_complete()
    {
        var order = DraftWithItem();

        order.Place();
        order.Pay();
        order.Ship();
        order.Complete();

        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public void Cannot_cancel_a_shipped_order()
    {
        var order = DraftWithItem();
        order.Place();
        order.Pay();
        order.Ship();

        Assert.Throws<InvalidOperationException>(() => order.Cancel());
    }

    [Fact]
    public void AddItem_rejects_mismatched_currency()
    {
        var order = new Order(Customer, Usd);

        Assert.Throws<InvalidOperationException>(() =>
            order.AddItem(Guid.NewGuid(), "Keyboard", new Money(100m, Eur), 1));
    }
}
