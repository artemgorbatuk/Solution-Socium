using Microsoft.Extensions.DependencyInjection;

namespace Services.Shared.Events;

public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

/// <summary>Раздаёт событие всем зарегистрированным <see cref="IDomainEventHandler{TEvent}"/> по очереди с await.</summary>
public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IServiceProvider serviceProvider;

    public DomainEventPublisher(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
