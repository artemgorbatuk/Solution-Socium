namespace Repositories.Socium.Ef.Options;

public class MessageGetNewOptions
{
    public required Guid ChatId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required Guid SenderUserId { get; set; }
    public ICollection<Guid> RecipientUserIds { get; set; } = [];
}
