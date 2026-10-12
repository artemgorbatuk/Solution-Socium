namespace Services.Socium.Models;

public class MessageCreatePageRequest
{
    public required Guid ChatId { get; set; }
}

public class MessageCreateRequest
{
    public required Guid ChatId { get; set; }
    public string Text { get; set; } = string.Empty;
    public IReadOnlyList<Guid>? FileIds { get; set; }
}

public class MessageCreatePageResponse
{
    public required Guid ChatId { get; set; }
    public required string Text { get; set; }
}

public class MessageUpdatePageRequest
{
    public required Guid Id { get; set; }
}

public class MessageUpdatePageResponse
{
    public required Guid Id { get; set; }
    public required Guid ChatId { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class MessageUpdateRequest
{
    public required Guid Id { get; set; }
    public required string Text { get; set; }
}

public class MessageDeletePageRequest
{
    public required Guid Id { get; set; }
}

public class MessageDeletePageResponse
{
    public required Guid Id { get; set; }
    public required Guid ChatId { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class MessageDeleteRequest
{
    public required Guid Id { get; set; }
}

public class MessageDeleteResponse
{
    public required bool IsDeleted { get; set; }
}

public class MessageInfoPageRequest
{
    public required Guid Id { get; set; }
}

public class MessageInfoPageResponse
{
    public required Guid Id { get; set; }
    public required Guid ChatId { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class MessageListPageRequest
{
    public required Guid ChatId { get; set; }

    /// <summary>
    /// Подгрузка истории: сообщения старше этого id. Без значения — последние сообщения чата.
    /// </summary>
    public Guid? BeforeId { get; set; }
}

public class MessageListPageResponse
{
    public required bool RowExists { get; set; }
    public required int RowCount { get; set; }
    public required bool HasOlder { get; set; }
    public required ICollection<MessageListModel> Rows { get; set; } = [];
}

public static class MessageListPaging
{
    public const int PageSize = 50;
}

public class MessageListModel
{
    public required Guid Id { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required Guid SenderUserId { get; set; }
    public required string SenderName { get; set; }
    public required bool CanUpdate { get; set; }
    public required bool CanDelete { get; set; }
    public IReadOnlyList<MessageAttachmentModel> Attachments { get; set; } = [];
}

public class MessageAttachmentModel
{
    public required Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
}

public class MessageDeliveredRequest
{
    public required Guid ChatId { get; set; }
    public required IReadOnlyList<Guid> MessageIds { get; set; }
}

public class MessageDeliveredResponse
{
    public required int UpdatedCount { get; set; }
}

public class MessageReadRequest
{
    public required Guid ChatId { get; set; }
}

public class MessageReadResponse
{
    public required int UpdatedCount { get; set; }
    public required int UnreadCount { get; set; }
}
