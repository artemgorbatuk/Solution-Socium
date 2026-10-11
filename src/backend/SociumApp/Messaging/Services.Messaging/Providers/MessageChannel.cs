using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Messaging.Enums;
using Services.Messaging.Models;
using Services.Messaging.Options;
using Services.Messaging.Texts;

namespace Services.Messaging.Providers;

public interface IMessageChannel
{
    MessageChannel Channel { get; }
    Task<MessageSendResult> SendAsync(Message message, CancellationToken cancellationToken = default);
}

public class SmtpEmailChannel : IMessageChannel
{
    private readonly SmtpOptions smtpOptions;
    private readonly ILogger<SmtpEmailChannel> logger;

    public SmtpEmailChannel(IOptions<MessagingOptions> messagingOptions, ILogger<SmtpEmailChannel> logger)
    {
        smtpOptions = messagingOptions.Value.Smtp;
        this.logger = logger;
    }

    public MessageChannel Channel => MessageChannel.Email;

    public async Task<MessageSendResult> SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message is not EmailMessage email)
        {
            return MessageSendResult.Failure(MessagingTexts.Messages.Error.InvalidMessageType);
        }

        if (string.IsNullOrWhiteSpace(smtpOptions.Host))
        {
            return MessageSendResult.Failure(MessagingTexts.Messages.Error.SmtpHostNotConfigured);
        }

        if (string.IsNullOrWhiteSpace(smtpOptions.From))
        {
            return MessageSendResult.Failure(MessagingTexts.Messages.Error.SmtpFromNotConfigured);
        }

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpOptions.From, smtpOptions.FromDisplayName),
                Subject = email.Subject ?? string.Empty,
                Body = email.Body,
                IsBodyHtml = email.IsHtml,
            };
            mailMessage.To.Add(email.To);

            using var client = new SmtpClient(smtpOptions.Host, smtpOptions.Port)
            {
                EnableSsl = smtpOptions.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = Math.Max(1, smtpOptions.TimeoutSeconds) * 1000,
            };

            if (!string.IsNullOrWhiteSpace(smtpOptions.UserName))
            {
                client.Credentials = new NetworkCredential(smtpOptions.UserName, smtpOptions.Password);
            }

            await client.SendMailAsync(mailMessage, cancellationToken);
            logger.LogInformation("{Message} {To}", MessagingTexts.Messages.Success.EmailSent, email.To);
            return MessageSendResult.Success();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", MessagingTexts.Messages.Error.SendFailed);
            return MessageSendResult.Failure(MessagingTexts.Messages.Error.SendFailed);
        }
    }
}
