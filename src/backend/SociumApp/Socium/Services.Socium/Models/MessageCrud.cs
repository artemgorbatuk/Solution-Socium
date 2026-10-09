namespace Services.Socium.Models;

public class MessageCreatePageRequest
{
    public required Guid ChatId { get; set; }
}

public class MessageCreateRequest
{
    public required Guid ChatId { get; set; }
    public required string Text { get; set; }
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
}

public class MessageListPageResponse
{
    public required bool RowExists { get; set; }
    public required int RowCount { get; set; }
    public required ICollection<MessageListModel> Rows { get; set; } = [];
}

public class MessageListModel
{
    public required Guid Id { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required Guid SenderUserId { get; set; }
    public required string SenderName { get; set; }
}
