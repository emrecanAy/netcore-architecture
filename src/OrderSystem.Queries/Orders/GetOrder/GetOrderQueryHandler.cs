using OrderSystem.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Models.Concrete;
using OrderSystem.Dto.Mapping;
using OrderSystem.Dto;
using MediatR;

namespace OrderSystem.Queries.Orders.GetOrder;

public record GetOrderQuery(Guid Id) : IRequest<OrderDto>;
public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto>
{
    private readonly ISQLRepository<Order> _orders;

    public GetOrderQueryHandler(ISQLRepository<Order> orders) => _orders = orders;

    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders
            .AsNoTracking()
            .Include(o => o.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {request.Id} not found.");

        return order.ToDto();
    }
}
