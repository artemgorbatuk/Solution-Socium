using Repositories.Security.Ef.Api;
using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Models;
using Services.Security.Permissions.Providers;

namespace Services.Security.Policies;

public interface IUserPolicy
{
    Task<bool> CanReadAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<bool> CanGrantAppAdminAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<bool> CanRevokeAppAdminAsync(Guid subjectId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Права на список пользователей и роли приложения — по ролям приложения субъекта и каталогу.
/// </summary>
public class UserPolicy : IUserPolicy
{
    private readonly IPermissionProvider permissionProvider;
    private readonly IUnitOfWorkSecurity unitOfWork;

    public UserPolicy(IPermissionProvider permissionProvider, IUnitOfWorkSecurity unitOfWork)
    {
        this.permissionProvider = permissionProvider;
        this.unitOfWork = unitOfWork;
    }

    public Task<bool> CanReadAsync(Guid subjectId, CancellationToken cancellationToken = default)
        => HasAppPermissionAsync(subjectId, PermissionNames.UserRead, cancellationToken);

    public Task<bool> CanGrantAppAdminAsync(Guid subjectId, CancellationToken cancellationToken = default)
        => HasAppPermissionAsync(subjectId, PermissionNames.AppAdminGrant, cancellationToken);

    public Task<bool> CanRevokeAppAdminAsync(Guid subjectId, CancellationToken cancellationToken = default)
        => HasAppPermissionAsync(subjectId, PermissionNames.AppAdminRevoke, cancellationToken);

    private async Task<bool> HasAppPermissionAsync(Guid subjectId, string permission, CancellationToken cancellationToken)
    {
        var roles = await unitOfWork.AppRoleAssignments.GetRoleNamesAsync(subjectId, cancellationToken);
        return permissionProvider.HasPermission(roles, permission, new PermissionContext { SubjectId = subjectId });
    }
}
