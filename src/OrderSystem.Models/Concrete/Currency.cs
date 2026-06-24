namespace OrderSystem.Models.Concrete;

/// <summary>
/// Currency reference (master) data. A small managed lookup table other entities
/// reference by <see cref="Id"/>. Identified by a surrogate int id (classic
/// lookup pattern) with the ISO 4217 <see cref="Code"/> as a natural alternate key.
/// Not an <c>Entity</c>: reference data needs no audit/soft-delete/domain events.
/// </summary>
public class Currency
{
    public int Id { get; protected set; }
    public string Code { get; protected set; } = default!;
    public string Name { get; protected set; } = default!;

    // EF Core materialization.
    protected Currency()
    {
    }

    public Currency(int id, string code, string name)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Currency id must be positive.");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Currency name is required.", nameof(name));

        Id = id;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
    }
}
