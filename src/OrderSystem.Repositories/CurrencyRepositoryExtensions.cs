using Microsoft.EntityFrameworkCore;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Repositories;

/// <summary>
/// Helpers for resolving currencies between their id and ISO code. Currencies
/// are a tiny reference table, so these reads are cheap.
/// </summary>
public static class CurrencyRepositoryExtensions
{
    /// <summary>id → ISO code map, used by DTO mapping to show the readable code.</summary>
    public static async Task<IReadOnlyDictionary<int, string>> GetCodeMapAsync(
        this ISQLRepository<Currency> currencies,
        CancellationToken cancellationToken) =>
        await currencies.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Code, cancellationToken);

    /// <summary>Looks up a currency by its ISO code (case-insensitive).</summary>
    public static Task<Currency?> FindByCodeAsync(
        this ISQLRepository<Currency> currencies,
        string code,
        CancellationToken cancellationToken)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        return currencies.FirstOrDefaultAsync(c => c.Code == normalized, cancellationToken);
    }
}
