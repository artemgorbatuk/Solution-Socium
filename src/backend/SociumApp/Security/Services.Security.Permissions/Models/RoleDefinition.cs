using Services.Security.Permissions.Enums;

namespace Services.Security.Permissions.Models;

public sealed class RoleDefinition
{
    public required string Name { get; init; }

    public required RoleScope Scope { get; init; }

    public required IReadOnlyList<RoleGrant> Grants { get; init; }
}
