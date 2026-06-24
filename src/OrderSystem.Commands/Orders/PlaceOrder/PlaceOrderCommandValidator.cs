using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.Orders.PlaceOrder;

public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator(ISQLRepository<Product> products)
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Items).NotEmpty().WithMessage("An order must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });

        RuleFor(x => x.Items)
            .MustAsync(async (items, ct) =>
            {
                var ids = items.Select(i => i.ProductId).Distinct().ToList();
                var activeCount = await products.CountAsync(p => ids.Contains(p.Id) && p.IsActive, ct);
                return activeCount == ids.Count;
            })
            .WithMessage("One or more products do not exist or are inactive.")
            .When(x => x.Items is { Count: > 0 });
    }
}
