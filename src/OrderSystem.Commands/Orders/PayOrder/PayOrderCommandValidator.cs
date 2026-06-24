using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.Orders.PayOrder;

public class PayOrderCommandValidator : AbstractValidator<PayOrderCommand>
{
    public PayOrderCommandValidator(ISQLRepository<Order> orders)
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await orders.AnyAsync(o => o.Id == id, ct))
            .WithMessage("Order not found.");
    }
}
