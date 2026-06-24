using MediatR;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.Products.CreateProduct;

/// <summary>Orchestration only — the business rules live in the Product constructor.</summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly ISQLRepository<Product> _products;

    public CreateProductCommandHandler(ISQLRepository<Product> products) => _products = products;

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(
            request.Name,
            new Sku(request.Sku),
            new Money(request.Price, request.Currency),
            request.InitialStock);

        await _products.AddAsync(product, cancellationToken);

        // No SaveChanges here — UnitOfWorkBehavior is the single commit point.
        return product.ToDto();
    }
}
