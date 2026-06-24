using OrderSystem.Models.Concrete;

namespace OrderSystem.Dto.Mapping;

/// <summary>
/// Hand-written entity-to-DTO mapping. Explicit and allocation-free to reason
/// about, with no runtime mapping configuration to drift out of sync.
/// </summary>
public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product) => new()
    {
        Id = product.Id,
        CreatedDate = product.CreatedDate,
        UpdatedDate = product.UpdatedDate,
        Name = product.Name,
        Sku = product.Sku.Value,
        Price = product.Price.Amount,
        Currency = product.Price.Currency,
        StockQuantity = product.StockQuantity,
        IsActive = product.IsActive,
    };

    public static IEnumerable<ProductDto> ToDto(this IEnumerable<Product> products) =>
        products.Select(p => p.ToDto());
}
