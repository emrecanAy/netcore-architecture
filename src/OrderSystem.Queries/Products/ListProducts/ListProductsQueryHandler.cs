using OrderSystem.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Models.Concrete;
using OrderSystem.Dto.Mapping;
using OrderSystem.Dto;
using MediatR;

namespace OrderSystem.Queries.Products.ListProducts;

public record ListProductsQuery(bool OnlyActive = false) : IRequest<IReadOnlyList<ProductDto>>;
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
