namespace Repositories.Security.Ef.Options;

public class AppRoleAssignmentQueryOptions
{
    public Guid? UserId { get; set; }
    public string? RoleName { get; set; }
}
