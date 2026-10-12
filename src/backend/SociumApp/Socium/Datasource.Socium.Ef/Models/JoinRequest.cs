using Datasource.Socium.Ef.Enums;

namespace Datasource.Socium.Ef.Models;

public class JoinRequest
{
    public required Guid Id { get; set; }
    public required Guid ChatId { get; set; }
    public required Guid UserId { get; set; }
    public required JoinRequestStatus Status { get; set; }
    public required Guid CreatedByUserId { get; set; }
    public Guid? RespondedByUserId { get; set; }
    public DateTime? RespondedAt { get; set; }

    public virtual Chat Chat { get; set; } = null!;
}
