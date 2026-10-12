using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Models;
using Services.Security.Permissions.Providers;
using Services.Socium.Ports;
using Services.Socium.Providers;
using Repositories.Socium.Ef.Api;

namespace Services.Socium.Policies;

public interface IRoomPolicy
{
    Task<bool> CanCreateAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<bool> CanReadAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default);

    Task<bool> CanUpdateAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default);

    Task<bool> CanDeleteAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetReadableRoomIdsAsync(Guid subjectId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Переводит комнату/контекст в <see cref="PermissionContext"/> и спрашивает <see cref="IPermissionProvider"/>.
/// Имён ролей в решениях can* не дублирует — только через назначения и каталог.
/// </summary>
public class RoomPolicy : IRoomPolicy
{
    private readonly IPermissionProvider permissionProvider;
    private readonly IRoleAssignmentProvider roleAssignmentProvider;
    private readonly IUserRolePort userRolePort;
    private readonly IUnitOfWorkSocium unitOfWork;

    public RoomPolicy(
        IPermissionProvider permissionProvider,
        IRoleAssignmentProvider roleAssignmentProvider,
        IUserRolePort userRolePort,
        IUnitOfWorkSocium unitOfWork)
    {
        this.permissionProvider = permissionProvider;
        this.roleAssignmentProvider = roleAssignmentProvider;
        this.userRolePort = userRolePort;
        this.unitOfWork = unitOfWork;
    }

    public async Task<bool> CanCreateAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        var roles = await userRolePort.GetAppRoleNamesAsync(subjectId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.RoomCreate,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanReadAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesForRoomAsync(subjectId, roomId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.RoomRead,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanUpdateAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesForRoomAsync(subjectId, roomId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.RoomUpdate,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<bool> CanDeleteAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken = default)
    {
        var roles = await GetRolesForRoomAsync(subjectId, roomId, cancellationToken);
        return permissionProvider.HasPermission(
            roles,
            PermissionNames.RoomDelete,
            new PermissionContext { SubjectId = subjectId });
    }

    public async Task<IReadOnlyList<Guid>> GetReadableRoomIdsAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        var appRoles = await userRolePort.GetAppRoleNamesAsync(subjectId, cancellationToken);
        if (permissionProvider.HasPermission(
                appRoles,
                PermissionNames.RoomRead,
                new PermissionContext { SubjectId = subjectId }))
        {
            var all = await unitOfWork.Rooms.GetListAsync(cancellationToken: cancellationToken);
            return all.Select(room => room.Id).ToList();
        }

        var roomAdminIds = await unitOfWork.RoomRoleAssignments.GetRoomIdsAsync(subjectId, cancellationToken);
        var chatRoomIds = await unitOfWork.ChatRoleAssignments.GetRoomIdsAsync(subjectId, cancellationToken);
        return roomAdminIds.Concat(chatRoomIds).Distinct().ToList();
    }

    private async Task<IReadOnlyList<string>> GetRolesForRoomAsync(Guid subjectId, Guid roomId, CancellationToken cancellationToken)
    {
        var appRoles = await userRolePort.GetAppRoleNamesAsync(subjectId, cancellationToken);
        var roomRoles = await roleAssignmentProvider.GetRoomRolesAsync(subjectId, roomId, cancellationToken);
        var chatRolesInRoom = await unitOfWork.ChatRoleAssignments.GetRoleNamesInRoomAsync(subjectId, roomId, cancellationToken);
        return appRoles.Concat(roomRoles).Concat(chatRolesInRoom).Distinct(StringComparer.Ordinal).ToList();
    }
}
