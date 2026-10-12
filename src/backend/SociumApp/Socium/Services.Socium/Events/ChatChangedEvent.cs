using Services.Shared.Events;
using Services.Socium.Enums;
using Services.Socium.Mapping;
using Services.Socium.Ports;

namespace Services.Socium.Events;

/// <summary>Чат зафиксирован в БД (после SaveChanges); для realtime списка чатов комнаты.</summary>
public sealed class ChatChangedEvent : IDomainEvent
{
    public required ChatChangedKind Kind { get; init; }
    public required Guid ChatId { get; init; }
    public required Guid RoomId { get; init; }
    public string? Name { get; init; }
}

/// <summary>Рассылает изменение чата всем клиентам хаба.</summary>
public sealed class NotifyChatChangedOnChatChangedHandler : IDomainEventHandler<ChatChangedEvent>
{
    private readonly IChatNotifyPort chatNotifyPort;

    public NotifyChatChangedOnChatChangedHandler(IChatNotifyPort chatNotifyPort)
    {
        this.chatNotifyPort = chatNotifyPort;
    }

    public async Task HandleAsync(ChatChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var notify = ChatMapper.ToNotifyChatChanged(domainEvent);
        await chatNotifyPort.NotifyChatChangedAsync(notify, cancellationToken);
    }
}
