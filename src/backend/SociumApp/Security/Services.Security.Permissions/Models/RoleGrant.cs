using Services.Security.Permissions.Enums;

namespace Services.Security.Permissions.Models;

public sealed class RoleGrant
{
    public required string Permission { get; init; }

    public PermissionCondition Condition { get; init; } = PermissionCondition.None;
}
