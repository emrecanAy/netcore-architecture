using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using OrderSystem.Models.Abstract;
using OrderSystem.Repositories.Abstract;
using OrderSystem.Repositories.Outbox;

namespace OrderSystem.Services.Behaviors;

/// <summary>
/// The single commit point. After the handler runs it dispatches accumulated
/// domain events (cascading until none remain), relays each to the transactional
/// outbox, then persists everything in one SaveChanges inside one transaction
/// (PROJECT.md §6.3 + improvements §15.4/§15.5). Handlers never call SaveChanges.
/// </summary>
public class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ISQLRepository<OutboxMessage> _outbox;

    public UnitOfWorkBehavior(IUnitOfWork unitOfWork, IMediator mediator, ISQLRepository<OutboxMessage> outbox)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _outbox = outbox;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // Read-only requests (queries) touch nothing — skip the transaction entirely.
        if (!_unitOfWork.HasChanges())
            return response;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        await DispatchDomainEventsAsync(cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return response;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Cascade: a handler may mutate another aggregate and raise new events.
        // Loop until the change set is quiescent.
        while (true)
        {
            var entities = _unitOfWork.GetChanges<EntityDomainEvent>()
                .Where(e => e.DomainEvents.Count > 0)
                .ToList();

            if (entities.Count == 0)
                break;

            var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();
            entities.ForEach(e => e.ClearDomainEvents()); // prevent re-dispatch

            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);   // in-process consistency handlers
                await _outbox.AddAsync(ToOutboxMessage(domainEvent), cancellationToken); // reliable external relay
            }
        }
    }

    private static OutboxMessage ToOutboxMessage(INotification domainEvent) => new()
    {
        Type = domainEvent.GetType().Name,
        Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
    };
}
