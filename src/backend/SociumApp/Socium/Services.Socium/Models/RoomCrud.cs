namespace Services.Socium.Models;

public class RoomCreatePageRequest
{
}

public class RoomCreateRequest
{
    public required string Name { get; set; }
}

public class RoomCreatePageResponse
{
    public required string Name { get; set; }
    public required bool CanCreate { get; set; }
}

public class RoomAssignRoomAdminStageRequest
{
    public required Guid RoomId { get; set; }
    public required Guid UserId { get; set; }
}

public class RoomUpdatePageRequest
{
    public required Guid Id { get; set; }
}

public class RoomUpdatePageResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required bool CanUpdate { get; set; }
}

public class RoomUpdateRequest
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class RoomDeletePageRequest
{
    public required Guid Id { get; set; }
}

public class RoomDeletePageResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required int ChatCount { get; set; }
    public required bool CanDelete { get; set; }
}

public class RoomDeleteRequest
{
    public required Guid Id { get; set; }
}

public class RoomDeleteResponse
{
    public required bool IsDeleted { get; set; }
}

public class RoomInfoPageRequest
{
    public required Guid Id { get; set; }
}

public class RoomInfoPageResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required bool CanUpdate { get; set; }
    public required bool CanDelete { get; set; }
}

public class RoomListPageRequest
{
}

public class RoomListPageResponse
{
    public required bool RowExists { get; set; }
    public required int RowCount { get; set; }
    public required ICollection<RoomListModel> Rows { get; set; } = [];
}

public class RoomListModel
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required bool CanUpdate { get; set; }
    public required bool CanDelete { get; set; }
}
