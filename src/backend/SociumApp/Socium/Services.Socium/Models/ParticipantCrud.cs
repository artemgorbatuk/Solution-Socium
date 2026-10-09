namespace Services.Socium.Models;

public class ParticipantCreateRequest
{
    public required Guid ChatId { get; set; }
}

public class ParticipantCreateResponse
{
    public required Guid Id { get; set; }
}

public class ParticipantUpdateRequest
{
    public required Guid Id { get; set; }
    public required bool IsAdmin { get; set; }
}

public class ParticipantUpdateResponse
{
    public required Guid Id { get; set; }
    public required bool IsAdmin { get; set; }
}

public class ParticipantDeletePageRequest
{
    public required Guid ChatId { get; set; }
}

public class ParticipantDeletePageResponse
{
    public required Guid ChatId { get; set; }
    public required bool CanLeave { get; set; }
    public string? Reason { get; set; }
}

public class ParticipantDeleteRequest
{
    public required Guid ChatId { get; set; }
}

public class ParticipantDeleteResponse
{
    public required bool IsDeleted { get; set; }
}

public class ParticipantListPageRequest
{
    public required Guid ChatId { get; set; }
}

public class ParticipantListPageResponse
{
    public required bool RowExists { get; set; }
    public required int RowCount { get; set; }
    public required bool IsAdmin { get; set; }
    public required ICollection<ParticipantListModel> Rows { get; set; } = [];
}

public class ParticipantListModel
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
    public required bool IsAdmin { get; set; }
}
