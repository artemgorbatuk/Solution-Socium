namespace Services.Realtime.Models;

public class UserChangedNotifyModel
{
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
}
