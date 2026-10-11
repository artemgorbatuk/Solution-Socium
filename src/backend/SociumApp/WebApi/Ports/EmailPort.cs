using Services.Messaging.Tokens.Api;
using Services.Security.Ports;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace WebApi.Ports;

/// <summary>
/// Адаптер порта Security к сервису токенов области Messaging.
/// </summary>
public class EmailPort : IEmailPort
{
    private readonly IServiceEmailToken serviceEmailToken;

    public EmailPort(IServiceEmailToken serviceEmailToken)
    {
        this.serviceEmailToken = serviceEmailToken;
    }

    public Task<ResponseInfo<object>> SendConfirmationAsync(Guid userId, string email, string displayName, CancellationToken cancellationToken = default)
    {
        return serviceEmailToken.SendConfirmationAsync(
            new EmailDispatchRequest { UserId = userId, Email = email, DisplayName = displayName },
            cancellationToken);
    }

    public async Task<ResponseInfo<EmailPortAcceptResponse>> AcceptConfirmationAsync(string token, CancellationToken cancellationToken = default)
    {
        var result = await serviceEmailToken.AcceptConfirmationAsync(new EmailTokenAcceptRequest { Token = token }, cancellationToken);
        return MapAccept(result);
    }

    public Task<ResponseInfo<object>> SendPasswordResetAsync(Guid userId, string email, string displayName, CancellationToken cancellationToken = default)
    {
        return serviceEmailToken.SendPasswordResetAsync(
            new EmailDispatchRequest { UserId = userId, Email = email, DisplayName = displayName },
            cancellationToken);
    }

    public async Task<ResponseInfo<EmailPortAcceptResponse>> AcceptPasswordResetAsync(string token, CancellationToken cancellationToken = default)
    {
        var result = await serviceEmailToken.AcceptPasswordResetAsync(new EmailTokenAcceptRequest { Token = token }, cancellationToken);
        return MapAccept(result);
    }

    private static ResponseInfo<EmailPortAcceptResponse> MapAccept(ResponseInfo<EmailTokenAcceptResponse> result)
    {
        var mapped = result.Response is { } accepted
            ? new EmailPortAcceptResponse { UserId = accepted.UserId }
            : null;

        return result.MessageInfo.MessageType switch
        {
            MessageType.SAVED => ResponseInfo.Success(mapped, MessageType.SAVED, result.MessageInfo.MessageText),
            MessageType.UNAUTHORIZED => ResponseInfo.Unauthorized<EmailPortAcceptResponse>(MessageType.UNAUTHORIZED, result.MessageInfo.MessageText),
            MessageType.BAD_REQUEST => ResponseInfo.BadRequest<EmailPortAcceptResponse>(MessageType.BAD_REQUEST, result.MessageInfo.MessageText),
            _ => ResponseInfo.Error<EmailPortAcceptResponse>(MessageType.ERROR, result.MessageInfo.MessageText),
        };
    }
}
