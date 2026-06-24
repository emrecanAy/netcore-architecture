using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Dto;
using OrderSystem.Dto.Mapping;
using OrderSystem.Models.Concrete;
using OrderSystem.Repositories;
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
    private readonly ISQLRepository<Currency> _currencies;

    public PlaceOrderCommandHandler(
        ISQLRepository<Product> products,
        ISQLRepository<Order> orders,
        ISQLRepository<Currency> currencies)
    {
        _products = products;
        _orders = orders;
        _currencies = currencies;
    }

    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var currency = await _currencies.FindByCodeAsync(request.Currency, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown currency '{request.Currency}'.");

        var order = new Order(request.CustomerId, currency.Id);

        foreach (var line in request.Items)
        {
            var product = await _products.FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Product {line.ProductId} not found.");

            // Snapshot the price at order time.
            order.AddItem(product.Id, product.Name, product.Price, line.Quantity);
        }

        order.Place();

        await _orders.AddAsync(order, cancellationToken);
        return order.ToDto(new Dictionary<int, string> { [currency.Id] = currency.Code });
    }
}
