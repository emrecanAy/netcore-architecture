using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Queries.Orders.GetOrder;

public record GetOrderQuery(Guid Id) : IRequest<OrderDto>;
public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto>
{
    private readonly ISQLRepository<Order> _orders;
    private readonly ISQLRepository<Currency> _currencies;

    public GetOrderQueryHandler(ISQLRepository<Order> orders, ISQLRepository<Currency> currencies)
    {
        _orders = orders;
        _currencies = currencies;
    }

    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders
            .AsNoTracking()
            .Include(o => o.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {request.Id} not found.");

        var currencyCodes = await _currencies.GetCodeMapAsync(cancellationToken);
        return order.ToDto(currencyCodes);
    }
}
