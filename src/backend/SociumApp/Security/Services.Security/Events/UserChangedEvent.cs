using Services.Security.Mapping;
using Services.Security.Ports;
using Services.Shared.Events;

namespace Services.Security.Events;

/// <summary>Профиль пользователя зафиксирован в БД (после SaveChanges); для realtime UI.</summary>
public sealed class UserChangedEvent : IDomainEvent
{
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
}

/// <summary>Рассылает смену имени всем клиентам хаба.</summary>
public sealed class NotifyUserChangedOnUserChangedHandler : IDomainEventHandler<UserChangedEvent>
{
    private readonly IUserNotifyPort userNotifyPort;

    public NotifyUserChangedOnUserChangedHandler(IUserNotifyPort userNotifyPort)
    {
        this.userNotifyPort = userNotifyPort;
    }

    public async Task HandleAsync(UserChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var notify = UserMapper.ToNotifyUserChanged(domainEvent);
        await userNotifyPort.NotifyUserChangedAsync(notify, cancellationToken);
    }
}
