using OrderSystem.Models.Concrete;

namespace OrderSystem.Dto.Mapping;

/// <summary>
/// Hand-written entity-to-DTO mapping. Explicit and allocation-free to reason
/// about, with no runtime mapping configuration to drift out of sync.
/// Currency is stored as an id; callers pass an id→code lookup so the DTO can
/// expose the human-readable ISO code alongside the id.
/// </summary>
public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product, IReadOnlyDictionary<int, string> currencyCodes) => new()
    {
        Id = product.Id,
        CreatedDate = product.CreatedDate,
        UpdatedDate = product.UpdatedDate,
        Name = product.Name,
        Sku = product.Sku.Value,
        Price = product.Price.Amount,
        CurrencyId = product.Price.CurrencyId,
        Currency = currencyCodes.TryGetValue(product.Price.CurrencyId, out var code) ? code : string.Empty,
        StockQuantity = product.StockQuantity,
        IsActive = product.IsActive,
    };

    public static IEnumerable<ProductDto> ToDto(
        this IEnumerable<Product> products,
        IReadOnlyDictionary<int, string> currencyCodes) =>
        products.Select(p => p.ToDto(currencyCodes));
}
