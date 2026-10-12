using Repositories.Socium.Ef.Api;
using Services.Socium.Ports;

namespace Services.Socium.Providers;

public interface IRoleAssignmentProvider
{
    Task<IReadOnlyList<string>> GetRoomRolesAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetChatRolesAsync(Guid userId, Guid chatId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Роли приложения + комнаты (без чата). Кэш на запрос.
    /// </summary>
    Task<IReadOnlyList<string>> GetRolesForRoomAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Роли приложения + комнаты + чата для ленты/проверок в одном чате. Результат кэшируется в scoped-словаре.
    /// </summary>
    Task<IReadOnlyList<string>> GetRolesForChatAsync(Guid userId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Чтение назначений Socium + app-ролей через порт. Один scoped-словарь на HTTP-запрос (ТТ17).
/// Роли чата — только из <c>ChatRoleAssignments</c>.
/// </summary>
public class RoleAssignmentProvider : IRoleAssignmentProvider
{
    private readonly IUnitOfWorkSocium unitOfWork;
    private readonly IUserRolePort userRolePort;
    private readonly Dictionary<string, IReadOnlyList<string>> cache = new(StringComparer.Ordinal);

    public RoleAssignmentProvider(IUnitOfWorkSocium unitOfWork, IUserRolePort userRolePort)
    {
        this.unitOfWork = unitOfWork;
        this.userRolePort = userRolePort;
    }

    public Task<IReadOnlyList<string>> GetRoomRolesAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default)
    {
        return GetOrLoadAsync(
            $"room:{userId:N}:{roomId:N}",
            () => unitOfWork.RoomRoleAssignments.GetRoleNamesAsync(userId, roomId, cancellationToken));
    }

    public Task<IReadOnlyList<string>> GetChatRolesAsync(Guid userId, Guid chatId, CancellationToken cancellationToken = default)
    {
        return GetOrLoadAsync(
            $"chat:{userId:N}:{chatId:N}",
            () => unitOfWork.ChatRoleAssignments.GetRoleNamesAsync(userId, chatId, cancellationToken));
    }

    public async Task<IReadOnlyList<string>> GetRolesForRoomAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default)
    {
        return await GetOrLoadAsync(
            $"roomctx:{userId:N}:{roomId:N}",
            async () =>
            {
                var appRoles = await userRolePort.GetAppRoleNamesAsync(userId, cancellationToken);
                var roomRoles = await GetRoomRolesAsync(userId, roomId, cancellationToken);
                return (IReadOnlyList<string>)appRoles.Concat(roomRoles).Distinct(StringComparer.Ordinal).ToList();
            });
    }

    public async Task<IReadOnlyList<string>> GetRolesForChatAsync(Guid userId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        return await GetOrLoadAsync(
            $"ctx:{userId:N}:{roomId:N}:{chatId:N}",
            async () =>
            {
                var appRoles = await userRolePort.GetAppRoleNamesAsync(userId, cancellationToken);
                var roomRoles = await GetRoomRolesAsync(userId, roomId, cancellationToken);
                var chatRoles = await GetChatRolesAsync(userId, chatId, cancellationToken);
                return (IReadOnlyList<string>)appRoles.Concat(roomRoles).Concat(chatRoles).Distinct(StringComparer.Ordinal).ToList();
            });
    }

    private async Task<IReadOnlyList<string>> GetOrLoadAsync(string key, Func<Task<IReadOnlyList<string>>> loader)
    {
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var roles = await loader();
        cache[key] = roles;
        return roles;
    }
}
