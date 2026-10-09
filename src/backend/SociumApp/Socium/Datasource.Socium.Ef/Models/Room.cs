namespace Datasource.Socium.Ef.Models;

public class Room
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }

    public virtual ICollection<Chat> Chats { get; set; } = [];
}
