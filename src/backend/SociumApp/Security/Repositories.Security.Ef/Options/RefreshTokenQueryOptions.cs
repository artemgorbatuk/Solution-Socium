namespace Repositories.Security.Ef.Options;

public class RefreshTokenQueryOptions
{
    public Guid? UserId { get; set; }
    public bool? IsRevoked { get; set; }
}
