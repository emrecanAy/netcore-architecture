using OrderSystem.Models.Concrete;

namespace OrderSystem.Dto.Mapping;

public static class OrderMappingExtensions
{
    public static OrderDto ToDto(this Order order, IReadOnlyDictionary<int, string> currencyCodes) => new()
    {
        Id = order.Id,
        CreatedDate = order.CreatedDate,
        UpdatedDate = order.UpdatedDate,
        CustomerId = order.CustomerId,
        Status = order.Status.Value,
        CurrencyId = order.CurrencyId,
        Currency = currencyCodes.TryGetValue(order.CurrencyId, out var code) ? code : string.Empty,
        TotalAmount = order.TotalAmount.Amount,
        Items = order.Items.Select(i => i.ToDto(currencyCodes)).ToList(),
    };

    public static OrderItemDto ToDto(this OrderItem item, IReadOnlyDictionary<int, string> currencyCodes) => new()
    {
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        UnitPrice = item.UnitPrice.Amount,
        CurrencyId = item.UnitPrice.CurrencyId,
        Currency = currencyCodes.TryGetValue(item.UnitPrice.CurrencyId, out var code) ? code : string.Empty,
        Quantity = item.Quantity,
        LineTotal = item.LineTotal.Amount,
    };

    public static IEnumerable<OrderDto> ToDto(
        this IEnumerable<Order> orders,
        IReadOnlyDictionary<int, string> currencyCodes) =>
        orders.Select(o => o.ToDto(currencyCodes));
}
