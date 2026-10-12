namespace Services.Realtime.Models;

public class JoinRequestChangedNotifyModel
{
    /// <summary>created | accepted | rejected</summary>
    public required string Kind { get; set; }
    public required Guid ChatId { get; set; }
    public required Guid JoinRequestId { get; set; }
    public required Guid UserId { get; set; }
    public required int PendingCount { get; set; }
}
