namespace Datasource.Security.Ef.Models;

public class AppRoleAssignment
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required string RoleName { get; set; }

    public virtual User User { get; set; } = null!;
}
