using Datasource.Security.Ef.Models;
using Repositories.Security.Ef.Options;
using Services.Security.Models;

namespace Services.Security.Mapping;

public static class AppRoleMapper
{
    public static void Apply(AppRoleAssignment model, Guid userId, string roleName)
    {
        model.UserId = userId;
        model.RoleName = roleName;
    }

    public static AppRoleAssignmentQueryOptions ToQueryOptions(Guid? userId = null, string? roleName = null)
    {
        return new AppRoleAssignmentQueryOptions
        {
            UserId = userId,
            RoleName = roleName,
        };
    }

    public static AppRoleCreateResponse ToCreateResponse(AppRoleAssignment model)
    {
        return new AppRoleCreateResponse
        {
            UserId = model.UserId,
            RoleName = model.RoleName,
        };
    }

    public static AppRoleDeleteResponse ToDeleteResponse()
    {
        return new AppRoleDeleteResponse
        {
            IsDeleted = true,
        };
    }
}
