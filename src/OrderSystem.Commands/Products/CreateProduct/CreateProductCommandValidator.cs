using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;
using OrderSystem.Repositories;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.Products.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(ISQLRepository<Product> products, ISQLRepository<Currency> currencies)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Currency)
            .NotEmpty().Length(3)
            .MustAsync(async (code, ct) => await currencies.FindByCodeAsync(code, ct) is not null)
            .WithMessage("Unknown currency.");

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MustAsync(async (sku, ct) =>
            {
                var normalized = new Sku(sku);
                return !await products.AnyAsync(p => p.Sku == normalized, ct);
            })
            .WithMessage("A product with this SKU already exists.");
    }
}
