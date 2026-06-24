namespace OrderSystem.Common.ValueObjects;

/// <summary>
/// Base for enum-like value objects backed by a single string value
/// (e.g. status codes). Gives type-safety over raw strings while keeping
/// value equality. Derived types expose a fixed set of static instances.
/// </summary>
public abstract class StringValueObject : ValueObject
{
    public string Value { get; }

    protected StringValueObject(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty.", nameof(value));

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
