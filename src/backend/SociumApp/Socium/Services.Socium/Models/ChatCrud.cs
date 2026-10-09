namespace Services.Socium.Models;

public class ChatCreatePageRequest
{
    public required Guid RoomId { get; set; }
}

public class ChatCreateRequest
{
    public required Guid RoomId { get; set; }
    public required string Name { get; set; }
}

public class ChatCreatePageResponse
{
    public required Guid RoomId { get; set; }
    public required string Name { get; set; }
}

public class ChatUpdatePageRequest
{
    public required Guid Id { get; set; }
}

public class ChatUpdatePageResponse
{
    public required Guid Id { get; set; }
    public required Guid RoomId { get; set; }
    public required string Name { get; set; }
}

public class ChatUpdateRequest
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class ChatDeletePageRequest
{
    public required Guid Id { get; set; }
}

public class ChatDeletePageResponse
{
    public required Guid Id { get; set; }
    public required Guid RoomId { get; set; }
    public required string Name { get; set; }
}

public class ChatDeleteRequest
{
    public required Guid Id { get; set; }
}

public class ChatDeleteResponse
{
    public required bool IsDeleted { get; set; }
}

public class ChatInfoPageRequest
{
    public required Guid Id { get; set; }
}

public class ChatInfoPageResponse
{
    public required Guid Id { get; set; }
    public required Guid RoomId { get; set; }
    public required string Name { get; set; }
}

public class ChatListPageRequest
{
    public required Guid RoomId { get; set; }
}

public class ChatListPageResponse
{
    public required bool RowExists { get; set; }
    public required int RowCount { get; set; }
    public required ICollection<ChatListModel> Rows { get; set; } = [];
}

public class ChatListModel
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}
