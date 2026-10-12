namespace Services.Socium.Models;

public class JoinRequestCreateRequest
{
    public required Guid ChatId { get; set; }
}

public class JoinRequestCreateResponse
{
    public required Guid Id { get; set; }
}

public class JoinRequestAcceptRequest
{
    public required Guid Id { get; set; }
}

public class JoinRequestAcceptResponse
{
    public required Guid Id { get; set; }
    public required Guid ParticipantId { get; set; }
}

public class JoinRequestRejectRequest
{
    public required Guid Id { get; set; }
}

public class JoinRequestRejectResponse
{
    public required Guid Id { get; set; }
}

public class JoinRequestPendingListPageRequest
{
    public required Guid ChatId { get; set; }
}

public class JoinRequestPendingListPageResponse
{
    public required bool RowExists { get; set; }
    public required int RowCount { get; set; }
    public required ICollection<JoinRequestPendingListModel> Rows { get; set; } = [];
}

public class JoinRequestPendingListModel
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
}
