using System.Security.Cryptography;

namespace Tests.Infrastructure;

/// <summary>
/// Ключ подписи JWT генерируется на каждый прогон — в репозитории ключа нет.
/// </summary>
public static class TestJwt
{
    public const string SigningKeySetting = "Security:Jwt:SigningKey";

    public static string CreateSigningKey()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    }
}
