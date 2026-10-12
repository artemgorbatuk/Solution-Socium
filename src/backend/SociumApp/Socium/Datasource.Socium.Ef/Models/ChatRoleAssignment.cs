namespace Datasource.Socium.Ef.Models;

public class ChatRoleAssignment
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required Guid ChatId { get; set; }

    public required string RoleName { get; set; }

    public virtual Chat Chat { get; set; } = null!;
}
