using Microsoft.Extensions.Logging;
using OrderSystem.Services.Abstract;

namespace OrderSystem.Services.Concrete;

/// <summary>
/// Stub email provider. Logs instead of sending; swap for a real provider
/// (SMTP/Graph/SendGrid) without touching callers.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger) => _logger = logger;

    public Task SendOrderConfirmationAsync(
        Guid orderId,
        Guid customerId,
        decimal total,
        string currency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL] Order confirmation sent for order {OrderId} to customer {CustomerId} — total {Total} {Currency}.",
            orderId, customerId, total, currency);
        return Task.CompletedTask;
    }
}
