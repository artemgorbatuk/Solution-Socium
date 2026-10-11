namespace Services.Realtime.Models;

public class ChatMessageNotifyModel
{
    public required Guid ChatId { get; set; }
    public required Guid Id { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required Guid SenderUserId { get; set; }
    public required string SenderName { get; set; }
    public IReadOnlyList<ChatMessageAttachmentNotifyModel> Attachments { get; set; } = [];
}

public class ChatMessageAttachmentNotifyModel
{
    public required Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
}

public class ChatMessageDeletedNotifyModel
{
    public required Guid ChatId { get; set; }
    public required Guid Id { get; set; }
}
