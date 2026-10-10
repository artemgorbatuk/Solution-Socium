namespace Repositories.Security.Ef.Options;

public class RefreshTokenGetNewOptions
{
    public required Guid UserId { get; init; }
    public required string TokenHash { get; init; }
    public required bool IsPersistent { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
