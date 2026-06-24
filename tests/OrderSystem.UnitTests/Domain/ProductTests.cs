using OrderSystem.Models.Concrete;
using OrderSystem.Models.DomainEvents;
using OrderSystem.Models.ValueObjects;
using Xunit;

namespace OrderSystem.UnitTests.Domain;

public class ProductTests
{
    private static Product NewProduct(int stock = 10) =>
        new("Keyboard", new Sku("kb-1"), new Money(100m, "usd"), stock);

    [Fact]
    public void Create_normalizes_sku_and_currency()
    {
        var product = NewProduct();

        Assert.Equal("KB-1", product.Sku.Value);
        Assert.Equal("USD", product.Price.Currency);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void DecreaseStock_reduces_quantity_and_raises_event()
    {
        var product = NewProduct(10);

        product.DecreaseStock(3);

        Assert.Equal(7, product.StockQuantity);
        Assert.Contains(product.DomainEvents, e => e is ProductStockDecreasedDomainEvent);
        Assert.DoesNotContain(product.DomainEvents, e => e is ProductOutOfStockDomainEvent);
    }

    [Fact]
    public void DecreaseStock_to_zero_raises_out_of_stock_event()
    {
        var product = NewProduct(3);

        product.DecreaseStock(3);

        Assert.Equal(0, product.StockQuantity);
        Assert.Contains(product.DomainEvents, e => e is ProductOutOfStockDomainEvent);
    }

    [Fact]
    public void DecreaseStock_beyond_available_throws()
    {
        var product = NewProduct(2);

        Assert.Throws<InvalidOperationException>(() => product.DecreaseStock(3));
    }
}
