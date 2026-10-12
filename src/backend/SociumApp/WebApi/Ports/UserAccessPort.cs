using Services.Security.Ports;
using Services.Shared.Enums;
using Services.Socium.Api;
using Services.Socium.Mapping;

namespace WebApi.Ports;

/// <summary>
/// Адаптер порта Security к доступам области Socium. Сбой Socium — исключение:
/// удаление пользователя не должно молча пропустить проверку «последний админ».
/// </summary>
public class UserAccessPort : IUserAccessPort
{
    private readonly IServiceProvider serviceProvider;

    // IServiceRoleAssignment → IUserPort → IServiceUser → IUserAccessPort: сервис берётся при вызове, иначе цикл в DI.
    public UserAccessPort(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task<IReadOnlyList<string>> GetLastAdminPlacesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var request = RoleAssignmentMapper.ToLastAdminListRequest(userId);
        var responseInfo = await GetService().DisplayLastAdminListAsync(request, cancellationToken);
        ThrowIfNot(MessageType.LOADED, responseInfo.MessageInfo.MessageType, responseInfo.MessageInfo.MessageText);
        return [.. responseInfo.Response!.Places];
    }

    public async Task DeleteAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var request = RoleAssignmentMapper.ToDeleteByUserRequest(userId);
        var responseInfo = await GetService().DeleteByUserAsync(request, cancellationToken);
        ThrowIfNot(MessageType.SAVED, responseInfo.MessageInfo.MessageType, responseInfo.MessageInfo.MessageText);
    }

    private IServiceRoleAssignment GetService() => serviceProvider.GetRequiredService<IServiceRoleAssignment>();

    private static void ThrowIfNot(int expected, int messageType, string messageText)
    {
        if (messageType != expected)
        {
            throw new InvalidOperationException(messageText);
        }
    }
}
