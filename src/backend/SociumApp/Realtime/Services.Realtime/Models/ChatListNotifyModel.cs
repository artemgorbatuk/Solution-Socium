namespace Services.Realtime.Models;

public class ChatListNotifyModel
{
    public required Guid ChatId { get; set; }
    public required Guid RoomId { get; set; }
    public required Guid TargetUserId { get; set; }
    public required int UnreadCount { get; set; }
    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessageSenderName { get; set; }
    public bool LastMessageHasAttachments { get; set; }
    public bool LastMessageCleared { get; set; }
}
