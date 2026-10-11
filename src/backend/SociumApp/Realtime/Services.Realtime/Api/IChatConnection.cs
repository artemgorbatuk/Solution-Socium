namespace Services.Realtime.Api;

/// <summary>
/// Абстракция отправки в группу чата без зависимости от ASP.NET SignalR.
/// </summary>
public interface IChatConnection
{
    Task SendToChatAsync(Guid chatId, string method, object payload, CancellationToken cancellationToken = default);
}
