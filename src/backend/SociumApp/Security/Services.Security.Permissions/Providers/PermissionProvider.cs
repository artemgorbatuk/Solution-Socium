using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Enums;
using Services.Security.Permissions.Models;

namespace Services.Security.Permissions.Providers;

public interface IPermissionProvider
{
    IReadOnlyList<RoleGrant> GetGrants(IEnumerable<string> roleNames);

    bool HasPermission(IEnumerable<string> roleNames, string permission, PermissionContext context);
}

/// <summary>
/// Проверка выдач ролей по каталогу. Без БД; регистрируется как singleton.
/// </summary>
public sealed class PermissionProvider : IPermissionProvider
{
    public IReadOnlyList<RoleGrant> GetGrants(IEnumerable<string> roleNames)
    {
        ArgumentNullException.ThrowIfNull(roleNames);

        var grants = new List<RoleGrant>();
        foreach (var roleName in roleNames.Distinct(StringComparer.Ordinal))
        {
            if (!RoleCatalog.TryGet(roleName, out var definition) || definition is null)
            {
                continue;
            }

            grants.AddRange(definition.Grants);
        }

        return grants;
    }

    public bool HasPermission(IEnumerable<string> roleNames, string permission, PermissionContext context)
    {
        ArgumentNullException.ThrowIfNull(roleNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        ArgumentNullException.ThrowIfNull(context);

        var matching = GetGrants(roleNames)
            .Where(grant => string.Equals(grant.Permission, permission, StringComparison.Ordinal))
            .ToList();

        if (matching.Count == 0)
        {
            return false;
        }

        if (matching.Any(grant => grant.Condition == PermissionCondition.None))
        {
            return true;
        }

        return matching.Any(grant =>
            grant.Condition == PermissionCondition.Sender
            && context.SenderId is Guid senderId
            && senderId == context.SubjectId);
    }
}
