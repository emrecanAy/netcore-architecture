using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Queries.Products.GetProduct;

public record GetProductQuery(Guid Id) : IRequest<ProductDto>;
public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    private readonly ISQLRepository<Product> _products;
    private readonly ISQLRepository<Currency> _currencies;

    public GetProductQueryHandler(ISQLRepository<Product> products, ISQLRepository<Currency> currencies)
    {
        _products = products;
        _currencies = currencies;
    }

    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product {request.Id} not found.");

        var currencyCodes = await _currencies.GetCodeMapAsync(cancellationToken);
        return product.ToDto(currencyCodes);
    }
}
