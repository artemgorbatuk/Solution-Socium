namespace Datasource.Security.Ef.Models;

public class User
{
    public required Guid Id { get; set; }

    public required string Login { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsDeleted { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
