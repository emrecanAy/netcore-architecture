using OrderSystem.Models.Concrete;

namespace OrderSystem.Dto.Mapping;

public static class OrderMappingExtensions
{
    public static OrderDto ToDto(this Order order) => new()
    {
        Id = order.Id,
        CreatedDate = order.CreatedDate,
        UpdatedDate = order.UpdatedDate,
        CustomerId = order.CustomerId,
        Status = order.Status.Value,
        Currency = order.Currency,
        TotalAmount = order.TotalAmount.Amount,
        Items = order.Items.Select(i => i.ToDto()).ToList(),
    };

    public static OrderItemDto ToDto(this OrderItem item) => new()
    {
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        UnitPrice = item.UnitPrice.Amount,
        Currency = item.UnitPrice.Currency,
        Quantity = item.Quantity,
        LineTotal = item.LineTotal.Amount,
    };
}
