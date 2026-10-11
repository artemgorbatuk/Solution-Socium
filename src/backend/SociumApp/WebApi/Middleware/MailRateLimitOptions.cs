namespace WebApi.Middleware;

/// <summary>
/// Секция <c>Messaging:RateLimit</c>: ограничение повторной отправки писем и сброса пароля с одного IP.
/// </summary>
public class MailRateLimitOptions
{
    public const string SectionName = "Messaging:RateLimit";

    public int PermitLimit { get; set; } = 5;
    public int WindowSeconds { get; set; } = 60;
}
