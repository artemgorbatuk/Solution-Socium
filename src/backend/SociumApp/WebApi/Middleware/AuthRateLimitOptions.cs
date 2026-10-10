namespace WebApi.Middleware;

/// <summary>
/// Секция <c>Security:RateLimit</c>: ограничение попыток входа и регистрации с одного IP.
/// </summary>
public class AuthRateLimitOptions
{
    public const string SectionName = "Security:RateLimit";

    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}
