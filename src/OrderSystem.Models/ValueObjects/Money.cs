using OrderSystem.Common.ValueObjects;

namespace OrderSystem.Models.ValueObjects;

/// <summary>
/// Composite value object: an amount paired with a currency, referenced by the
/// Currency entity's id (not a raw code string). Immutable; arithmetic returns
/// new instances and refuses to mix currencies.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public int CurrencyId { get; }

    public Money(decimal amount, int currencyId)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        if (currencyId <= 0)
            throw new ArgumentOutOfRangeException(nameof(currencyId), "A valid currency is required.");

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        CurrencyId = currencyId;
    }

    public static Money Zero(int currencyId) => new(0m, currencyId);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, CurrencyId);
    }

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        return new Money(Amount * quantity, CurrencyId);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (CurrencyId != other.CurrencyId)
            throw new InvalidOperationException(
                $"Cannot operate on different currencies: {CurrencyId} vs {other.CurrencyId}.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return CurrencyId;
    }

    public override string ToString() => $"{Amount:0.00} (currency #{CurrencyId})";
}
