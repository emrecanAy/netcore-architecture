using MediatR;
using OrderSystem.Dto;

namespace OrderSystem.Commands.Orders.PlaceOrder;

public record PlaceOrderItem(Guid ProductId, int Quantity);

public record PlaceOrderCommand(
    Guid CustomerId,
    string Currency,
    IReadOnlyList<PlaceOrderItem> Items) : IRequest<OrderDto>;
