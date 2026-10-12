namespace Services.Security.Permissions.Models;

public sealed class PermissionContext
{
    public Guid SubjectId { get; init; }

    public Guid? SenderId { get; init; }
}
