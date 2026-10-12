namespace Services.Shared.Events;

/// <summary>Обработчик domain event; выполняется синхронно в рамках Publish до commit сценария.</summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
