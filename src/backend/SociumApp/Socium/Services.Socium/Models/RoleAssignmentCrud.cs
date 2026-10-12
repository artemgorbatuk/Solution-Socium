namespace Services.Socium.Models;

/// <summary>Дерево «все комнаты → чаты» с ролями пользователя; для админа приложения (<c>UserRead</c>).</summary>
public class RoleAssignmentListPageRequest
{
    public required Guid UserId { get; set; }
}

public class RoleAssignmentListPageResponse
{
    public required Guid UserId { get; set; }
    /// <summary><c>UserAccessManage</c>, цель — не текущий пользователь и не удалена.</summary>
    public required bool CanManage { get; set; }
    public required ICollection<RoleAssignmentRoomModel> Rooms { get; set; } = [];
}

public class RoleAssignmentRoomModel
{
    public required Guid RoomId { get; set; }
    public required string RoomName { get; set; }
    public required bool IsRoomAdmin { get; set; }
    public required bool IsLastRoomAdmin { get; set; }
    public required ICollection<RoleAssignmentChatModel> Chats { get; set; } = [];
}

public class RoleAssignmentChatModel
{
    public required Guid ChatId { get; set; }
    public required string ChatName { get; set; }
    public required bool IsParticipant { get; set; }
    public required bool IsChatAdmin { get; set; }
    public required bool IsLastChatAdmin { get; set; }
}

/// <summary>Роль комнаты — с <see cref="RoomId"/>, роль чата — с <see cref="ChatId"/>.</summary>
public class RoleAssignmentCreateRequest
{
    public required Guid UserId { get; set; }
    public required string RoleName { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? ChatId { get; set; }
}

public class RoleAssignmentCreateResponse
{
    public required Guid UserId { get; set; }
    public required string RoleName { get; set; }
}

/// <summary>Снятие «Участник» исключает из чата вместе с ролью админа.</summary>
public class RoleAssignmentDeleteRequest
{
    public required Guid UserId { get; set; }
    public required string RoleName { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? ChatId { get; set; }
}

public class RoleAssignmentDeleteResponse
{
    public required bool IsDeleted { get; set; }
}

/// <summary>Где пользователь — единственный админ комнаты или чата; проверка перед удалением пользователя.</summary>
public class RoleAssignmentLastAdminListRequest
{
    public required Guid UserId { get; set; }
}

public class RoleAssignmentLastAdminListResponse
{
    public required ICollection<string> Places { get; set; } = [];
}

/// <summary>Снять все роли, членство и заявки удалённого пользователя (после удаления в Security).</summary>
public class RoleAssignmentDeleteByUserRequest
{
    public required Guid UserId { get; set; }
}

public class RoleAssignmentDeleteByUserResponse
{
    public required bool IsDeleted { get; set; }
}
