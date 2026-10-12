using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Models;
using Services.Security.Permissions.Providers;
using Services.Socium.Providers;

namespace Services.Socium.Policies;

public interface IParticipantPolicy
{
    Task<bool> CanReadAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    Task<bool> CanGrantAdminAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    Task<bool> CanRevokeAdminAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    Task<bool> CanGrantParticipantAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    Task<bool> CanRevokeParticipantAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);

    /// <summary>Выход: субъект имеет роль участника в этом чате (без отдельного разрешения).</summary>
    Task<bool> CanLeaveAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Права на список участников и смену админ-роли; выход — по наличию <c>ChatParticipant</c>.
/// </summary>
public class ParticipantPolicy : IParticipantPolicy
{
    private readonly IPermissionProvider permissionProvider;
    private readonly IRoleAssignmentProvider roleAssignmentProvider;

    public ParticipantPolicy(
        IPermissionProvider permissionProvider,
        IRoleAssignmentProvider roleAssignmentProvider)
    {
        this.permissionProvider = permissionProvider;
        this.roleAssignmentProvider = roleAssignmentProvider;
    }

    public async Task<bool> CanReadAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.ParticipantRead,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanGrantAdminAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.ChatAdminGrant,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanRevokeAdminAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.ChatAdminRevoke,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanGrantParticipantAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.ChatParticipantGrant,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanRevokeParticipantAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.ChatParticipantRevoke,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanLeaveAsync(Guid subjectId, Guid roomId, Guid chatId, CancellationToken cancellationToken = default)
    {
        var roles = await roleAssignmentProvider.GetRolesForChatAsync(subjectId, roomId, chatId, cancellationToken);
        return roles.Contains(RoleNames.ChatParticipant, StringComparer.Ordinal);
    }
}
