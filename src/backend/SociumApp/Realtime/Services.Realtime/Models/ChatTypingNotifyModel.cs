namespace Services.Realtime.Models;

public class ChatTypingStartedNotifyModel
{
    public required Guid ChatId { get; set; }
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
}

public class ChatTypingStoppedNotifyModel
{
    public required Guid ChatId { get; set; }
    public required Guid UserId { get; set; }
}
