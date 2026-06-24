using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Queries.Products.ListProducts;

public record ListProductsQuery(bool OnlyActive = false) : IRequest<IReadOnlyList<ProductDto>>;
public class ListProductsQueryHandler : IRequestHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly ISQLRepository<Product> _products;
    private readonly ISQLRepository<Currency> _currencies;

    public ListProductsQueryHandler(ISQLRepository<Product> products, ISQLRepository<Currency> currencies)
    {
        _products = products;
        _currencies = currencies;
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _products.AsNoTracking();
        if (request.OnlyActive)
            query = query.Where(p => p.IsActive);

        var products = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        var currencyCodes = await _currencies.GetCodeMapAsync(cancellationToken);
        return products.ToDto(currencyCodes).ToList();
    }
}
