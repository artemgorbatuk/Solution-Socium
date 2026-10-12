namespace Services.Security.Ports;

/// <summary>
/// Порт Security к области Realtime: смена отображаемого имени пользователя.
/// Адаптер в WebApi вызывает <c>IServiceChatNotify</c>.
/// </summary>
public interface IUserNotifyPort
{
    Task NotifyUserChangedAsync(UserNotifyChangedModel change, CancellationToken cancellationToken = default);
}

public class UserNotifyChangedModel
{
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
}
