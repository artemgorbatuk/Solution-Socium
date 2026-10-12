using Services.Shared.Events;
using Services.Socium.Enums;
using Services.Socium.Mapping;
using Services.Socium.Ports;

namespace Services.Socium.Events;

/// <summary>Заявка на вступление зафиксирована в БД (после SaveChanges); для realtime панели и stub.</summary>
public sealed class JoinRequestChangedEvent : IDomainEvent
{
    public required JoinRequestChangedKind Kind { get; init; }
    public required Guid ChatId { get; init; }
    public required Guid JoinRequestId { get; init; }
    public required Guid UserId { get; init; }
    public required int PendingCount { get; init; }
}

/// <summary>Рассылает изменение заявки в группу чата и заявителю.</summary>
public sealed class NotifyJoinRequestChangedOnJoinRequestChangedHandler : IDomainEventHandler<JoinRequestChangedEvent>
{
    private readonly IChatNotifyPort chatNotifyPort;

    public NotifyJoinRequestChangedOnJoinRequestChangedHandler(IChatNotifyPort chatNotifyPort)
    {
        this.chatNotifyPort = chatNotifyPort;
    }

    public async Task HandleAsync(JoinRequestChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var notify = JoinRequestMapper.ToNotifyJoinRequestChanged(domainEvent);
        await chatNotifyPort.NotifyJoinRequestChangedAsync(notify, cancellationToken);
    }
}
