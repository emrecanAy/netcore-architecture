using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.Orders.PayOrder;

public record PayOrderCommand(Guid OrderId) : IRequest<OrderDto>;
public class PayOrderCommandHandler : IRequestHandler<PayOrderCommand, OrderDto>
{
    private readonly ISQLRepository<Order> _orders;

    public PayOrderCommandHandler(ISQLRepository<Order> orders) => _orders = orders;

    public async Task<OrderDto> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {request.OrderId} not found.");

        order.Pay();

        return order.ToDto();
    }
}
