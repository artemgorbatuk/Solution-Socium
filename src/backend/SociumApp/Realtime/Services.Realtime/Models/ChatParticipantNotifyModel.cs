namespace Services.Realtime.Models;

public class ChatParticipantNotifyModel
{
    public required Guid ChatId { get; set; }
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
    public required bool IsAdmin { get; set; }
}

public class ChatParticipantLeftNotifyModel
{
    public required Guid ChatId { get; set; }
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
}
