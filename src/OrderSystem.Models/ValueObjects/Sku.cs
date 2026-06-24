using OrderSystem.Common.ValueObjects;

namespace OrderSystem.Models.ValueObjects;

/// <summary>
/// Stock Keeping Unit: a normalized product code. Type-safe wrapper over a
/// string so an SKU can never be confused with an arbitrary string.
/// </summary>
public sealed class Sku : StringValueObject
{
    public Sku(string value) : base(Normalize(value))
    {
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SKU is required.", nameof(value));
        return value.Trim().ToUpperInvariant();
    }
}
