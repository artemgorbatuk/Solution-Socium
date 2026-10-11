using Microsoft.AspNetCore.SignalR;
using Services.Realtime.Api;
using SignalR.Realtime.Hubs;

namespace SignalR.Realtime.Api;

public class ChatConnection : IChatConnection
{
    private readonly IHubContext<ChatHub> hubContext;

    public ChatConnection(IHubContext<ChatHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    public Task SendToChatAsync(Guid chatId, string method, object payload, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients
            .Group(ChatHubMethods.ChatGroup(chatId))
            .SendAsync(method, payload, cancellationToken);
    }
}
