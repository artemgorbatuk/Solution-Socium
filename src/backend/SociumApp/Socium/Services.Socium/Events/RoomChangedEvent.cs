using Services.Shared.Events;
using Services.Socium.Enums;
using Services.Socium.Mapping;
using Services.Socium.Ports;

namespace Services.Socium.Events;

/// <summary>Комната зафиксирована в БД (после SaveChanges); для realtime сайдбара.</summary>
public sealed class RoomChangedEvent : IDomainEvent
{
    public required RoomChangedKind Kind { get; init; }
    public required Guid RoomId { get; init; }
    public string? Name { get; init; }
}

/// <summary>Рассылает изменение комнаты всем клиентам хаба.</summary>
public sealed class NotifyRoomChangedOnRoomChangedHandler : IDomainEventHandler<RoomChangedEvent>
{
    private readonly IChatNotifyPort chatNotifyPort;

    public NotifyRoomChangedOnRoomChangedHandler(IChatNotifyPort chatNotifyPort)
    {
        this.chatNotifyPort = chatNotifyPort;
    }

    public async Task HandleAsync(RoomChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var notify = RoomMapper.ToNotifyRoomChanged(domainEvent);
        await chatNotifyPort.NotifyRoomChangedAsync(notify, cancellationToken);
    }
}
