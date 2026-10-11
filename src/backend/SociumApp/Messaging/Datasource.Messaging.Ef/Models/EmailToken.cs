namespace Datasource.Messaging.Ef.Models;

public class EmailToken
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required EmailTokenPurpose Purpose { get; set; }
    public required string TokenHash { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
