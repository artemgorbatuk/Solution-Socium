namespace Services.Security.Tokens.Options;

/// <summary>
/// Секция <c>Security:Jwt</c>. Ключ подписи в репозиторий не попадает: локально — user-secrets,
/// в тестах — случайный на прогон, на стенде — переменная окружения <c>Security__Jwt__SigningKey</c>.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Security:Jwt";
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; set; } = "Socium";
    public string Audience { get; set; } = "Socium";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
