using MediatR;
using OrderSystem.Dto;

namespace OrderSystem.Commands.Orders.PayOrder;

public record PayOrderCommand(Guid OrderId) : IRequest<OrderDto>;
