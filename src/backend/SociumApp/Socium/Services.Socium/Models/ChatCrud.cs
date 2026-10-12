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

/// <summary>Создание чата в текущем UoW без SaveChanges (внешняя транзакция / handler).</summary>
public class ChatCreateStageRequest
{
    public required Guid RoomId { get; set; }
    public required Guid CreatorUserId { get; set; }
    public required string Name { get; set; }
}

public class ChatCreatePageResponse
{
    public required Guid Id { get; set; }
    public required Guid RoomId { get; set; }
    public required string Name { get; set; }
    public required bool CanCreate { get; set; }
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
    public required bool CanUpdate { get; set; }
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
    public required bool CanDelete { get; set; }
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
    public required bool IsParticipant { get; set; }
    public required bool IsAdmin { get; set; }
    public required bool CanUpdate { get; set; }
    public required bool CanDelete { get; set; }
    /// <summary><c>ChatParticipantGrant</c> — заявки / добавление участников.</summary>
    public required bool CanManageParticipants { get; set; }
    /// <summary>У текущего пользователя есть pending-заявка в этот чат.</summary>
    public required bool HasPendingJoinRequest { get; set; }
    /// <summary>Число pending-заявок чата; без <c>CanManageParticipants</c> — 0.</summary>
    public required int PendingJoinRequestCount { get; set; }
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
    public required bool CanUpdate { get; set; }
    public required bool CanDelete { get; set; }
    public required int UnreadCount { get; set; }
    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessageSenderName { get; set; }
    public Guid? LastMessageSenderUserId { get; set; }
    public bool LastMessageHasAttachments { get; set; }
}
