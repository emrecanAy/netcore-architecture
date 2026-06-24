using MediatR;
using Microsoft.Extensions.Logging;
using OrderSystem.Models.DomainEvents;

namespace OrderSystem.Commands.DomainEventHandlers;

/// <summary>
/// Reacts to a product running out of stock. A real system might notify
/// purchasing or deactivate the product; here it logs the signal.
/// </summary>
public class OnProductOutOfStockDomainEventHandler : INotificationHandler<ProductOutOfStockDomainEvent>
{
    private readonly ILogger<OnProductOutOfStockDomainEventHandler> _logger;

    public OnProductOutOfStockDomainEventHandler(ILogger<OnProductOutOfStockDomainEventHandler> logger) =>
        _logger = logger;

    public Task Handle(ProductOutOfStockDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Product {ProductId} ('{Name}') is now out of stock.",
            notification.Product.Id, notification.Product.Name);
        return Task.CompletedTask;
    }
}
