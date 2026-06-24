using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderSystem.Models.DomainEvents;
using OrderSystem.Repositories.Concrete;
using OrderSystem.Services.Abstract;

namespace OrderSystem.Services.Outbox;

/// <summary>
/// Reliably relays committed domain events to the outside world (PROJECT.md §15.4).
/// Runs after persistence, so external side-effects (e.g. confirmation email) never
/// fire for a transaction that rolled back. Polls unprocessed outbox rows, performs
/// the integration, and marks them processed.
/// </summary>
public class OutboxDispatcher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Outbox dispatch batch failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        foreach (var message in pending)
        {
            try
            {
                await DeliverAsync(message.Type, message.Content, email, cancellationToken);
                message.ProcessedOn = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                _logger.LogError(ex, "Failed to dispatch outbox message {MessageId} ({Type}).", message.Id, message.Type);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DeliverAsync(string type, string content, IEmailService email, CancellationToken cancellationToken)
    {
        // Map integration messages to external side-effects by type.
        if (type == nameof(OrderPlacedDomainEvent))
        {
            using var doc = JsonDocument.Parse(content);
            var order = doc.RootElement.GetProperty("Order");
            var orderId = order.GetProperty("Id").GetGuid();
            var customerId = order.GetProperty("CustomerId").GetGuid();
            var total = order.GetProperty("TotalAmount");
            var amount = total.GetProperty("Amount").GetDecimal();
            var currency = total.GetProperty("Currency").GetString() ?? "";

            await email.SendOrderConfirmationAsync(orderId, customerId, amount, currency, cancellationToken);
        }
    }
}
