namespace Datasource.Socium.Ef.Models;

public class Participant
{
    public required Guid Id { get; set; }

    public required Guid ChatId { get; set; }
    public required Guid UserId { get; set; }

    public virtual Chat Chat { get; set; } = null!;
}
