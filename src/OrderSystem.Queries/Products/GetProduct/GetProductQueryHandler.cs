using OrderSystem.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Models.Concrete;
using OrderSystem.Dto.Mapping;
using OrderSystem.Dto;
using MediatR;

namespace OrderSystem.Queries.Products.GetProduct;

public record GetProductQuery(Guid Id) : IRequest<ProductDto>;
public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    private readonly ISQLRepository<Product> _products;

    public GetProductQueryHandler(ISQLRepository<Product> products) => _products = products;

    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product {request.Id} not found.");

        return product.ToDto();
    }
}
