using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.DomainEvents;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Commands.DomainEventHandlers;

/// <summary>
/// Cross-aggregate reaction: when an order is placed, reduce stock on each
/// ordered product. DecreaseStock raises further domain events, which the
/// UnitOfWorkBehavior cascade picks up — all within the same transaction.
/// </summary>
public class OnOrderPlacedDecreaseProductStockDomainEventHandler
    : INotificationHandler<OrderPlacedDomainEvent>
{
    private readonly ISQLRepository<Product> _products;

    public OnOrderPlacedDecreaseProductStockDomainEventHandler(ISQLRepository<Product> products) =>
        _products = products;

    public async Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        foreach (var item in notification.Order.Items)
        {
            var product = await _products.FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);
            product?.DecreaseStock(item.Quantity);
        }
    }
}
