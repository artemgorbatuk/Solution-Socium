namespace Datasource.Socium.Ef.Models;

public class Message
{
    public required Guid Id { get; set; }
    public required Guid ChatId { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }

    public virtual Chat Chat { get; set; } = null!;
}
