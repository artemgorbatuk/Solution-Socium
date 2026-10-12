using Services.Security.Ports;
using Services.Shared.Events;

namespace Services.Security.Events;

/// <summary>Пользователь мягко удалён и его роли приложения сняты (после SaveChanges).</summary>
public sealed class UserDeletedEvent : IDomainEvent
{
    public required Guid UserId { get; init; }
}

/// <summary>Снимает роли удалённого пользователя в комнатах и чатах.</summary>
public sealed class RemoveAccessOnUserDeletedHandler : IDomainEventHandler<UserDeletedEvent>
{
    private readonly IUserAccessPort userAccessPort;

    public RemoveAccessOnUserDeletedHandler(IUserAccessPort userAccessPort)
    {
        this.userAccessPort = userAccessPort;
    }

    public async Task HandleAsync(UserDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await userAccessPort.DeleteAllAsync(domainEvent.UserId, cancellationToken);
    }
}

/// <summary>Сообщает клиентам хаба, что доступ пользователя изменился.</summary>
public sealed class NotifyUserAccessChangedOnUserDeletedHandler : IDomainEventHandler<UserDeletedEvent>
{
    private readonly IUserNotifyPort userNotifyPort;

    public NotifyUserAccessChangedOnUserDeletedHandler(IUserNotifyPort userNotifyPort)
    {
        this.userNotifyPort = userNotifyPort;
    }

    public async Task HandleAsync(UserDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await userNotifyPort.NotifyUserAccessChangedAsync(domainEvent.UserId, cancellationToken);
    }
}
