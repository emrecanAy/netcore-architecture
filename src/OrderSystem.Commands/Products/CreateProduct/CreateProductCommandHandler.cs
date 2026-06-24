using MediatR;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;
using OrderSystem.Repositories;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.Products.CreateProduct;

/// <summary>Orchestration only — the business rules live in the Product constructor.</summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly ISQLRepository<Product> _products;
    private readonly ISQLRepository<Currency> _currencies;

    public CreateProductCommandHandler(ISQLRepository<Product> products, ISQLRepository<Currency> currencies)
    {
        _products = products;
        _currencies = currencies;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var currency = await _currencies.FindByCodeAsync(request.Currency, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown currency '{request.Currency}'.");

        var product = new Product(
            request.Name,
            new Sku(request.Sku),
            new Money(request.Price, currency.Id),
            request.InitialStock);

        await _products.AddAsync(product, cancellationToken);

        // No SaveChanges here — UnitOfWorkBehavior is the single commit point.
        return product.ToDto(new Dictionary<int, string> { [currency.Id] = currency.Code });
    }
}
