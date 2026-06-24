using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;
using OrderSystem.Repositories;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Queries.Orders.ListOrders;

/// <summary>
/// Status arrives as a string from the query string (OrderStatus is a value
/// object and can't be model-bound directly). It's converted via
/// OrderStatus.FromValue, which throws on an unknown value → HTTP 400.
/// </summary>
public record ListOrdersQuery(string? Status = null) : IRequest<IReadOnlyList<OrderDto>>;

public class ListOrdersQueryHandler : IRequestHandler<ListOrdersQuery, IReadOnlyList<OrderDto>>
{
    private readonly ISQLRepository<Order> _orders;
    private readonly ISQLRepository<Currency> _currencies;

    public ListOrdersQueryHandler(ISQLRepository<Order> orders, ISQLRepository<Currency> currencies)
    {
        _orders = orders;
        _currencies = currencies;
    }

    public async Task<IReadOnlyList<OrderDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _orders.AsNoTracking().Include(o => o.Items).AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = OrderStatus.FromValue(request.Status);
            query = query.Where(o => o.Status == status);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync(cancellationToken);

        var currencyCodes = await _currencies.GetCodeMapAsync(cancellationToken);
        return orders.ToDto(currencyCodes).ToList();
    }
}
