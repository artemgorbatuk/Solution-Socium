namespace Services.Socium.Ports;

/// <summary>
/// Порт Socium к области Realtime: уведомления открытого чата.
/// Адаптер в WebApi вызывает <c>IServiceChatNotify</c>.
/// </summary>
public interface IChatNotifyPort
{
    Task NotifyMessageCreatedAsync(ChatNotifyMessageModel message, CancellationToken cancellationToken = default);
    Task NotifyMessageUpdatedAsync(ChatNotifyMessageModel message, CancellationToken cancellationToken = default);
    Task NotifyMessageDeletedAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken = default);
    Task NotifyParticipantJoinedAsync(ChatNotifyParticipantModel participant, CancellationToken cancellationToken = default);
    Task NotifyParticipantLeftAsync(Guid chatId, Guid participantId, Guid userId, CancellationToken cancellationToken = default);
    Task NotifyParticipantRoleChangedAsync(ChatNotifyParticipantModel participant, CancellationToken cancellationToken = default);
    Task NotifyChatListUpdatedAsync(ChatNotifyListModel update, CancellationToken cancellationToken = default);
    Task NotifyRoomChangedAsync(ChatNotifyRoomChangedModel change, CancellationToken cancellationToken = default);
    Task NotifyChatChangedAsync(ChatNotifyChatChangedModel change, CancellationToken cancellationToken = default);
    Task NotifyJoinRequestChangedAsync(ChatNotifyJoinRequestChangedModel change, CancellationToken cancellationToken = default);
}

public class ChatNotifyMessageModel
{
    public required Guid ChatId { get; set; }
    public required Guid Id { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required Guid SenderUserId { get; set; }
    public required string SenderName { get; set; }
    public IReadOnlyList<ChatNotifyAttachmentModel> Attachments { get; set; } = [];
}

public class ChatNotifyAttachmentModel
{
    public required Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
}

public class ChatNotifyParticipantModel
{
    public required Guid ChatId { get; set; }
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
    public required bool IsAdmin { get; set; }
}

public class ChatNotifyListModel
{
    public required Guid ChatId { get; set; }
    public required Guid RoomId { get; set; }
    public required Guid TargetUserId { get; set; }
    public required int UnreadCount { get; set; }
    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessageSenderName { get; set; }
    public Guid? LastMessageSenderUserId { get; set; }
    public bool LastMessageHasAttachments { get; set; }
    public bool LastMessageCleared { get; set; }
}

public class ChatNotifyRoomChangedModel
{
    /// <summary>created | updated | deleted</summary>
    public required string Kind { get; set; }
    public required Guid RoomId { get; set; }
    public string? Name { get; set; }
}

public class ChatNotifyChatChangedModel
{
    /// <summary>created | updated | deleted</summary>
    public required string Kind { get; set; }
    public required Guid RoomId { get; set; }
    public required Guid ChatId { get; set; }
    public string? Name { get; set; }
}

public class ChatNotifyJoinRequestChangedModel
{
    /// <summary>created | accepted | rejected</summary>
    public required string Kind { get; set; }
    public required Guid ChatId { get; set; }
    public required Guid JoinRequestId { get; set; }
    public required Guid UserId { get; set; }
    public required int PendingCount { get; set; }
}
