namespace Services.Realtime.Models;

public class ChatChangedNotifyModel
{
    /// <summary>created | updated | deleted</summary>
    public required string Kind { get; set; }
    public required Guid RoomId { get; set; }
    public required Guid ChatId { get; set; }
    public string? Name { get; set; }
}
