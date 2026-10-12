using Repositories.Socium.Ef.Api;
using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Models;
using Services.Security.Permissions.Providers;
using Services.Socium.Providers;

namespace Services.Socium.Policies;

public interface IChatPolicy
{
    Task<bool> CanCreateAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default);

    Task<bool> CanReadAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    Task<bool> CanUpdateAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    Task<bool> CanDeleteAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetReadableChatIdsAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Переводит комнату/чат в <see cref="PermissionContext"/> и спрашивает <see cref="IPermissionProvider"/>.
/// </summary>
public class ChatPolicy : IChatPolicy
{
    private readonly IPermissionProvider permissionProvider;
    private readonly IRoleAssignmentProvider roleAssignmentProvider;
    private readonly IUnitOfWorkSocium unitOfWork;

    public ChatPolicy(
        IPermissionProvider permissionProvider,
        IRoleAssignmentProvider roleAssignmentProvider,
        IUnitOfWorkSocium unitOfWork)
    {
        this.permissionProvider = permissionProvider;
        this.roleAssignmentProvider = roleAssignmentProvider;
        this.unitOfWork = unitOfWork;
    }

    public async Task<bool> CanCreateAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForRoomAsync(subjectId, roomId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.ChatCreate,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanReadAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.ChatRead,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanUpdateAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.ChatUpdate,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanDeleteAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.ChatDelete,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<IReadOnlyList<Guid>> GetReadableChatIdsAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default)
    {
        var roomRoles = await roleAssignmentProvider.GetRolesForRoomAsync(subjectId, roomId, cancellationToken);
        if (permissionProvider.HasPermission(
                roomRoles,
                PermissionNames.ChatRead,
                new PermissionContext { SubjectId = subjectId }))
        {
            var chatOptions = new Repositories.Socium.Ef.Options.ChatQueryOptions { RoomId = roomId };
            var chats = await unitOfWork.Chats.GetListAsync(chatOptions, cancellationToken);
            return chats.Select(chat => chat.Id).ToList();
        }

        return await unitOfWork.ChatRoleAssignments.GetChatIdsInRoomAsync(subjectId, roomId, cancellationToken);
    }
}
