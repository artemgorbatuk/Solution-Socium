namespace Services.Realtime.Models;

public class RoomChangedNotifyModel
{
    /// <summary>created | updated | deleted</summary>
    public required string Kind { get; set; }
    public required Guid RoomId { get; set; }
    public string? Name { get; set; }
}
