using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Models;
using Services.Security.Permissions.Providers;
using Services.Socium.Ports;

namespace Services.Socium.Policies;

public interface IRoleAssignmentPolicy
{
    Task<bool> CanReadAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<bool> CanManageAsync(Guid subjectId, CancellationToken cancellationToken = default);
}

/// <summary>Доступы пользователей видит и меняет только роль приложения (<c>UserRead</c>, <c>UserAccessManage</c>).</summary>
public class RoleAssignmentPolicy : IRoleAssignmentPolicy
{
    private readonly IPermissionProvider permissionProvider;
    private readonly IUserRolePort userRolePort;

    public RoleAssignmentPolicy(IPermissionProvider permissionProvider, IUserRolePort userRolePort)
    {
        this.permissionProvider = permissionProvider;
        this.userRolePort = userRolePort;
    }

    public Task<bool> CanReadAsync(Guid subjectId, CancellationToken cancellationToken = default)
        => HasAppPermissionAsync(subjectId, PermissionNames.UserRead, cancellationToken);

    public Task<bool> CanManageAsync(Guid subjectId, CancellationToken cancellationToken = default)
        => HasAppPermissionAsync(subjectId, PermissionNames.UserAccessManage, cancellationToken);

    private async Task<bool> HasAppPermissionAsync(Guid subjectId, string permission, CancellationToken cancellationToken)
    {
        var roles = await userRolePort.GetAppRoleNamesAsync(subjectId, cancellationToken);
        return permissionProvider.HasPermission(roles, permission, new PermissionContext { SubjectId = subjectId });
    }
}
