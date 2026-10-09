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
}

public class RoomUpdatePageRequest
{
    public required Guid Id { get; set; }
}

public class RoomUpdatePageResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
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
}
