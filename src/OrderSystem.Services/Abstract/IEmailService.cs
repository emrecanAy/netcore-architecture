namespace OrderSystem.Services.Abstract;

/// <summary>
/// External email integration, hidden behind an interface so handlers and the
/// outbox dispatcher never depend on a concrete provider (PROJECT.md §10).
/// </summary>
public interface IEmailService
{
    Task SendOrderConfirmationAsync(
        Guid orderId,
        Guid customerId,
        decimal total,
        string currency,
        CancellationToken cancellationToken = default);
}
