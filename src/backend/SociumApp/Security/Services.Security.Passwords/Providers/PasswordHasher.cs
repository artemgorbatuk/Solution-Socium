using Microsoft.AspNetCore.Identity;

namespace Services.Security.Passwords.Providers;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

/// <summary>
/// PBKDF2 из ASP.NET Core Identity; хеш содержит соль и параметры, поэтому старые хеши проверяются и после смены параметров.
/// </summary>
public class AspNetIdentityPasswordHasher : IPasswordHasher
{
    private static readonly object HashUser = new();

    private readonly PasswordHasher<object> hasher = new();

    public string Hash(string password)
    {
        return hasher.HashPassword(HashUser, password);
    }

    public bool Verify(string passwordHash, string password)
    {
        var result = hasher.VerifyHashedPassword(HashUser, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
