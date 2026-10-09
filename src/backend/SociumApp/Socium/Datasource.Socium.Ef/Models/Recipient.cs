namespace Datasource.Socium.Ef.Models;

public class Recipient
{
    public required Guid Id { get; set; }

    public required Guid MessageId { get; set; }
    public Guid? UserId { get; set; }

    public virtual Message Message { get; set; } = null!;
    public virtual User? User { get; set; }
}
