namespace Repositories.Socium.Ef.Options;

public class ParticipantQueryOptions
{
    public Guid? ChatId { get; set; }
    public Guid? UserId { get; set; }
    public bool? IsAdmin { get; set; }
}
