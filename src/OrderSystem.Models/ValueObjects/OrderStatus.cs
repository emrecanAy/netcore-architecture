using OrderSystem.Common.ValueObjects;

namespace OrderSystem.Models.ValueObjects;

/// <summary>
/// Enum-like value object for the order lifecycle. Using a value object instead
/// of a raw string keeps invalid statuses out at compile time and lets the
/// allowed transitions live next to the states.
/// </summary>
public sealed class OrderStatus : StringValueObject
{
    public static readonly OrderStatus Draft = new("Draft");
    public static readonly OrderStatus Placed = new("Placed");
    public static readonly OrderStatus Paid = new("Paid");
    public static readonly OrderStatus Shipped = new("Shipped");
    public static readonly OrderStatus Completed = new("Completed");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    private static readonly IReadOnlyDictionary<string, OrderStatus> All =
        new[] { Draft, Placed, Paid, Shipped, Completed, Cancelled }
            .ToDictionary(s => s.Value, StringComparer.OrdinalIgnoreCase);

    private OrderStatus(string value) : base(value)
    {
    }

    /// <summary>Rebuilds a status from its stored string (used by EF conversion).</summary>
    public static OrderStatus FromValue(string value)
    {
        if (All.TryGetValue(value, out var status))
            return status;
        throw new ArgumentException($"Unknown order status: '{value}'.", nameof(value));
    }
}
