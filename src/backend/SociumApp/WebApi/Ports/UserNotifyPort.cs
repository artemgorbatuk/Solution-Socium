using Services.Realtime.Api;
using Services.Realtime.Models;
using Services.Security.Ports;

namespace WebApi.Ports;

/// <summary>
/// Адаптер порта Security к сервису уведомлений области Realtime.
/// </summary>
public class UserNotifyPort : IUserNotifyPort
{
    private readonly IServiceChatNotify serviceChatNotify;

    public UserNotifyPort(IServiceChatNotify serviceChatNotify)
    {
        this.serviceChatNotify = serviceChatNotify;
    }

    public Task NotifyUserChangedAsync(UserNotifyChangedModel change, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyUserChangedAsync(
            new UserChangedNotifyModel
            {
                UserId = change.UserId,
                Name = change.Name,
            },
            cancellationToken);

    public Task NotifyUserAccessChangedAsync(Guid userId, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyUserAccessChangedAsync(
            new UserAccessChangedNotifyModel { UserId = userId },
            cancellationToken);
}
