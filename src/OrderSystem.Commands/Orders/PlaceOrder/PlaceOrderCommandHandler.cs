using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.Orders.PlaceOrder;

/// <summary>
/// Builds an order from current product prices and places it. Placing raises
/// OrderPlacedDomainEvent, which the domain-event handler turns into stock
/// reductions — the cascade runs inside the UnitOfWorkBehavior transaction.
/// </summary>
public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    private readonly ISQLRepository<Product> _products;
    private readonly ISQLRepository<Order> _orders;

    public PlaceOrderCommandHandler(ISQLRepository<Product> products, ISQLRepository<Order> orders)
    {
        _products = products;
        _orders = orders;
    }

    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(request.CustomerId, request.Currency);

        foreach (var line in request.Items)
        {
            var product = await _products.FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Product {line.ProductId} not found.");

            // Snapshot the price at order time.
            order.AddItem(product.Id, product.Name, product.Price, line.Quantity);
        }

        order.Place();

        await _orders.AddAsync(order, cancellationToken);
        return order.ToDto();
    }
}
