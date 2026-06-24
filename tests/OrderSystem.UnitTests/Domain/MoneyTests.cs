using OrderSystem.Models.ValueObjects;
using Xunit;

namespace OrderSystem.UnitTests.Domain;

public class MoneyTests
{
    private const int Usd = 1;
    private const int Eur = 2;

    [Fact]
    public void Equality_is_by_value()
    {
        Assert.Equal(new Money(10m, Usd), new Money(10m, Usd));
        Assert.NotEqual(new Money(10m, Usd), new Money(10m, Eur));
    }

    [Fact]
    public void Add_same_currency_sums()
    {
        var result = new Money(10m, Usd).Add(new Money(5m, Usd));

        Assert.Equal(new Money(15m, Usd), result);
    }

    [Fact]
    public void Add_different_currency_throws()
    {
        Assert.Throws<InvalidOperationException>(() => new Money(10m, Usd).Add(new Money(5m, Eur)));
    }

    [Fact]
    public void Multiply_scales_amount()
    {
        Assert.Equal(new Money(30m, Usd), new Money(10m, Usd).Multiply(3));
    }

    [Fact]
    public void Negative_amount_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, Usd));
    }

    [Fact]
    public void Invalid_currency_id_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(1m, 0));
    }
}
