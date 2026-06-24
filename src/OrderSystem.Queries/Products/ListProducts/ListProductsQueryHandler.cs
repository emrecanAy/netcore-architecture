using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Queries.Products.ListProducts;

public class ListProductsQueryHandler : IRequestHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly ISQLRepository<Product> _products;

    public ListProductsQueryHandler(ISQLRepository<Product> products) => _products = products;

    public async Task<IReadOnlyList<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _products.AsNoTracking();
        if (request.OnlyActive)
            query = query.Where(p => p.IsActive);

        var products = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return products.ToDto().ToList();
    }
}
