using MediatR;
using OrderSystem.Dto;

namespace OrderSystem.Queries.Orders.GetOrder;

public record GetOrderQuery(Guid Id) : IRequest<OrderDto>;
