namespace Repositories.Socium.Ef.Models;

public class ChatLastMessageRow
{
    public required Guid ChatId { get; set; }
    public required Guid MessageId { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required Guid SenderUserId { get; set; }
}
