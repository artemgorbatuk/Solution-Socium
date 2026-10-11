using Services.Messaging.Enums;

namespace Services.Messaging.Options;

public class MessagingOptions
{
    public const string SectionName = "Messaging";

    public ConfirmationOptions Confirmation { get; set; } = new();
    public PasswordResetOptions PasswordReset { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
}

public class ConfirmationOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:4200/confirm-email";
    public int TokenDays { get; set; } = 2;
    public MessageChannel Channel { get; set; } = MessageChannel.Email;
}

public class PasswordResetOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:4200/reset-password";
    public int TokenDays { get; set; } = 1;
    public MessageChannel Channel { get; set; } = MessageChannel.Email;
}

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "noreply@localhost";
    public string FromDisplayName { get; set; } = "Socium";
    /// <summary>Таймаут SMTP в секундах (по умолчанию короткий, чтобы регистрация не зависала без Mailpit).</summary>
    public int TimeoutSeconds { get; set; } = 10;
}
