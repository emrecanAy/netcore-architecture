using OrderSystem.Models.ValueObjects;
using Xunit;

namespace OrderSystem.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Equality_is_by_value()
    {
        Assert.Equal(new Money(10m, "USD"), new Money(10m, "USD"));
        Assert.NotEqual(new Money(10m, "USD"), new Money(10m, "EUR"));
    }

    [Fact]
    public void Add_same_currency_sums()
    {
        var result = new Money(10m, "USD").Add(new Money(5m, "USD"));

        Assert.Equal(new Money(15m, "USD"), result);
    }

    [Fact]
    public void Add_different_currency_throws()
    {
        Assert.Throws<InvalidOperationException>(() => new Money(10m, "USD").Add(new Money(5m, "EUR")));
    }

    [Fact]
    public void Multiply_scales_amount()
    {
        Assert.Equal(new Money(30m, "USD"), new Money(10m, "USD").Multiply(3));
    }

    [Fact]
    public void Negative_amount_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, "USD"));
    }
}
