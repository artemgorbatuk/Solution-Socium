namespace Repositories.Socium.Ef.Options;

public class ParticipantGetNewOptions
{
    public required Guid ChatId { get; set; }
    public required Guid UserId { get; set; }
    public bool IsAdmin { get; set; }
}
