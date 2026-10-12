using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Models;
using Services.Security.Permissions.Providers;
using Services.Socium.Providers;

namespace Services.Socium.Policies;

public interface IMessagePolicy
{
    Task<bool> CanCreateAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    Task<bool> CanReadAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    Task<bool> CanUpdateAsync(
        Guid subjectId,
        Guid roomId,
        Guid chatId,
        Guid senderUserId,
        CancellationToken cancellationToken = default);

    Task<bool> CanDeleteAsync(
        Guid subjectId,
        Guid roomId,
        Guid chatId,
        Guid senderUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Роли субъекта для чата один раз; дальше <c>can*</c> по ленте — только в памяти (ТТ17).
    /// </summary>
    Task<IReadOnlyList<string>> GetRolesForChatAsync(
        Guid subjectId,
        Guid roomId,
        Guid chatId,
        CancellationToken cancellationToken = default);

    bool CanUpdate(IReadOnlyList<string> roles, Guid subjectId, Guid senderUserId);

    bool CanDelete(IReadOnlyList<string> roles, Guid subjectId, Guid senderUserId);
}

/// <summary>
/// Переводит сообщение в <see cref="PermissionContext"/> (<c>SenderId</c>) и спрашивает <see cref="IPermissionProvider"/>.
/// </summary>
public class MessagePolicy : IMessagePolicy
{
    private readonly IPermissionProvider permissionProvider;
    private readonly IRoleAssignmentProvider roleAssignmentProvider;

    public MessagePolicy(
        IPermissionProvider permissionProvider,
        IRoleAssignmentProvider roleAssignmentProvider)
    {
        this.permissionProvider = permissionProvider;
        this.roleAssignmentProvider = roleAssignmentProvider;
    }

    public async Task<bool> CanCreateAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.MessageCreate,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanReadAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.MessageRead,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanUpdateAsync(
        Guid subjectId,
        Guid roomId,
        Guid chatId,
        Guid senderUserId,
        CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return CanUpdate(roles, subjectId, senderUserId);
    }

    public async Task<bool> CanDeleteAsync(
        Guid subjectId,
        Guid roomId,
        Guid chatId,
        Guid senderUserId,
        CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return CanDelete(roles, subjectId, senderUserId);
    }

    public Task<IReadOnlyList<string>> GetRolesForChatAsync(
        Guid subjectId,
        Guid roomId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        return roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
    }

    public bool CanUpdate(IReadOnlyList<string> roles, Guid subjectId, Guid senderUserId)
    {
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.MessageUpdate,
            new PermissionContext { SubjectId = subjectId, SenderId = senderUserId });
    }

    public bool CanDelete(IReadOnlyList<string> roles, Guid subjectId, Guid senderUserId)
    {
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.MessageDelete,
            new PermissionContext { SubjectId = subjectId, SenderId = senderUserId });
    }
}
