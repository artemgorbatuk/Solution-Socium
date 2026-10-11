using Datasource.Messaging.Ef.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Messaging.Ef.Api;
using Repositories.Messaging.Ef.Options;
using Services.Messaging.Enums;
using Services.Messaging.Models;
using Services.Messaging.Options;
using Services.Messaging.Providers;
using Services.Messaging.Texts;
using Services.Messaging.Tokens.Providers;
using Services.Messaging.Tokens.Texts;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace Services.Messaging.Tokens.Api;

public class EmailDispatchRequest
{
    public required Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
}

public class EmailTokenAcceptRequest
{
    public required string Token { get; set; }
}

public class EmailTokenAcceptResponse
{
    public required Guid UserId { get; set; }
}

public interface IServiceEmailToken
{
    Task<ResponseInfo<object>> SendConfirmationAsync(EmailDispatchRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<EmailTokenAcceptResponse>> AcceptConfirmationAsync(EmailTokenAcceptRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<object>> SendPasswordResetAsync(EmailDispatchRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<EmailTokenAcceptResponse>> AcceptPasswordResetAsync(EmailTokenAcceptRequest request, CancellationToken cancellationToken = default);
}

public class ServiceEmailToken : IServiceEmailToken
{
    private readonly IUnitOfWorkMessaging unitOfWork;
    private readonly IMessageContext messageContext;
    private readonly IEmailTokenHasher tokenHasher;
    private readonly MessagingOptions messagingOptions;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ServiceEmailToken> logger;

    public ServiceEmailToken(
        IUnitOfWorkMessaging unitOfWork,
        IMessageContext messageContext,
        IEmailTokenHasher tokenHasher,
        IOptions<MessagingOptions> messagingOptions,
        TimeProvider timeProvider,
        ILogger<ServiceEmailToken> logger)
    {
        this.unitOfWork = unitOfWork;
        this.messageContext = messageContext;
        this.tokenHasher = tokenHasher;
        this.messagingOptions = messagingOptions.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public Task<ResponseInfo<object>> SendConfirmationAsync(EmailDispatchRequest request, CancellationToken cancellationToken = default)
        => SendAsync(
            request,
            EmailTokenPurpose.ConfirmEmail,
            messagingOptions.Confirmation.TokenDays,
            messagingOptions.Confirmation.BaseUrl,
            messagingOptions.Confirmation.Channel,
            MessagingTexts.Messages.Confirmation.EmailSubject,
            MessagingTexts.Messages.Confirmation.EmailBody,
            EmailTokenTexts.Messages.Success.ConfirmationSent,
            EmailTokenTexts.Messages.Error.SendConfirmationFailed,
            cancellationToken);

    public Task<ResponseInfo<object>> SendPasswordResetAsync(EmailDispatchRequest request, CancellationToken cancellationToken = default)
        => SendAsync(
            request,
            EmailTokenPurpose.ResetPassword,
            messagingOptions.PasswordReset.TokenDays,
            messagingOptions.PasswordReset.BaseUrl,
            messagingOptions.PasswordReset.Channel,
            MessagingTexts.Messages.PasswordReset.EmailSubject,
            MessagingTexts.Messages.PasswordReset.EmailBody,
            EmailTokenTexts.Messages.Success.PasswordResetSent,
            EmailTokenTexts.Messages.Error.SendPasswordResetFailed,
            cancellationToken);

    public Task<ResponseInfo<EmailTokenAcceptResponse>> AcceptConfirmationAsync(EmailTokenAcceptRequest request, CancellationToken cancellationToken = default)
        => AcceptAsync(request, EmailTokenPurpose.ConfirmEmail, cancellationToken);

    public Task<ResponseInfo<EmailTokenAcceptResponse>> AcceptPasswordResetAsync(EmailTokenAcceptRequest request, CancellationToken cancellationToken = default)
        => AcceptAsync(request, EmailTokenPurpose.ResetPassword, cancellationToken);

    private async Task<ResponseInfo<object>> SendAsync(
        EmailDispatchRequest request,
        EmailTokenPurpose purpose,
        int tokenDays,
        string baseUrl,
        MessageChannel channel,
        string subject,
        string bodyTemplate,
        string successText,
        string errorText,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("{Message}", successText);

        try
        {
            if (channel != MessageChannel.Email)
            {
                logger.LogError("{Message}", EmailTokenTexts.Messages.Error.UnsupportedChannel);
                return ResponseInfo.Error<object>(MessageType.ERROR, EmailTokenTexts.Messages.Error.UnsupportedChannel);
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            await unitOfWork.EmailTokens.InvalidateUnusedAsync(request.UserId, purpose, now, cancellationToken);

            var plainToken = tokenHasher.CreatePlainToken();
            var token = unitOfWork.EmailTokens.GetNew(new EmailTokenGetNewOptions
            {
                UserId = request.UserId,
                Purpose = purpose,
                TokenHash = tokenHasher.Hash(plainToken),
                CreatedAt = now,
                ExpiresAt = now.AddDays(tokenDays),
            });
            await unitOfWork.EmailTokens.CreateAsync(token, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var link = $"{baseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(plainToken)}";
            var sendResult = await messageContext.SendAsync(
                new EmailMessage
                {
                    To = request.Email,
                    Subject = subject,
                    Body = string.Format(bodyTemplate, request.DisplayName, link),
                },
                cancellationToken);

            if (!sendResult.Succeeded)
            {
                logger.LogError("{Message}: {Detail}", errorText, sendResult.Error);
                return ResponseInfo.Error<object>(MessageType.ERROR, errorText);
            }

            logger.LogInformation("{Message}", successText);
            return ResponseInfo.Success(new object(), MessageType.SAVED, successText);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", errorText);
            return ResponseInfo.Error<object>(MessageType.ERROR, errorText);
        }
    }

    private async Task<ResponseInfo<EmailTokenAcceptResponse>> AcceptAsync(
        EmailTokenAcceptRequest request,
        EmailTokenPurpose purpose,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.Token))
            {
                return ResponseInfo.Unauthorized<EmailTokenAcceptResponse>(
                    MessageType.UNAUTHORIZED,
                    EmailTokenTexts.Messages.Error.AcceptFailed);
            }

            var token = await unitOfWork.EmailTokens.GetSingleByHashOrDefaultAsync(tokenHasher.Hash(request.Token), cancellationToken);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (token is null
                || token.Purpose != purpose
                || token.UsedAt is not null
                || token.ExpiresAt <= now)
            {
                return ResponseInfo.Unauthorized<EmailTokenAcceptResponse>(
                    MessageType.UNAUTHORIZED,
                    EmailTokenTexts.Messages.Error.AcceptFailed);
            }

            token.UsedAt = now;
            await unitOfWork.EmailTokens.UpdateAsync(token, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ResponseInfo.Success(
                new EmailTokenAcceptResponse { UserId = token.UserId },
                MessageType.SAVED,
                purpose == EmailTokenPurpose.ConfirmEmail
                    ? EmailTokenTexts.Messages.Success.ConfirmationAccepted
                    : EmailTokenTexts.Messages.Success.PasswordResetAccepted);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", EmailTokenTexts.Messages.Error.AcceptFailed);
            return ResponseInfo.Error<EmailTokenAcceptResponse>(MessageType.ERROR, EmailTokenTexts.Messages.Error.AcceptFailed);
        }
    }
}
