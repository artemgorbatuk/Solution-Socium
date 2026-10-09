namespace Repositories.Socium.Ef.Options;

public class MessageGetNewOptions
{
    public required Guid ChatId { get; set; }
    public required DateTime CreatedAt { get; set; }
}
