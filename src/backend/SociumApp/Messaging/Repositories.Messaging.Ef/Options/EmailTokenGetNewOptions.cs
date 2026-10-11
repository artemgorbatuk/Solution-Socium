using Datasource.Messaging.Ef.Models;

namespace Repositories.Messaging.Ef.Options;

public class EmailTokenGetNewOptions
{
    public required Guid UserId { get; set; }
    public required EmailTokenPurpose Purpose { get; set; }
    public required string TokenHash { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime ExpiresAt { get; set; }
}
