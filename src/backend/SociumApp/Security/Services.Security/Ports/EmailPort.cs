using Services.Shared.Models;

namespace Services.Security.Ports;

/// <summary>
/// Порт Security к области Messaging: письма и одноразовые токены.
/// Адаптер в WebApi вызывает <c>IServiceEmailToken</c>.
/// </summary>
public interface IEmailPort
{
    Task<ResponseInfo<object>> SendConfirmationAsync(Guid userId, string email, string displayName, CancellationToken cancellationToken = default);
    Task<ResponseInfo<EmailPortAcceptResponse>> AcceptConfirmationAsync(string token, CancellationToken cancellationToken = default);
    Task<ResponseInfo<object>> SendPasswordResetAsync(Guid userId, string email, string displayName, CancellationToken cancellationToken = default);
    Task<ResponseInfo<EmailPortAcceptResponse>> AcceptPasswordResetAsync(string token, CancellationToken cancellationToken = default);
}

public class EmailPortAcceptResponse
{
    public required Guid UserId { get; set; }
}
