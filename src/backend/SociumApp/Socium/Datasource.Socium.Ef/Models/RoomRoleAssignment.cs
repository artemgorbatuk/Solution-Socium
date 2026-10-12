namespace Datasource.Socium.Ef.Models;

public class RoomRoleAssignment
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required Guid RoomId { get; set; }

    public required string RoleName { get; set; }

    public virtual Room Room { get; set; } = null!;
}
