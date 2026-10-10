namespace Datasource.Security.Ef.Models;

public class RefreshToken
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }

    public required string TokenHash { get; set; }
    public required bool IsPersistent { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public virtual User User { get; set; } = null!;
}
