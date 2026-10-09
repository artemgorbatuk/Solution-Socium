namespace Datasource.Socium.Ef.Models;

public class Chat
{
    public required Guid Id { get; set; }
    public required Guid RoomId { get; set; }
    public required string Name { get; set; }

    public virtual Room Room { get; set; } = null!;
}
