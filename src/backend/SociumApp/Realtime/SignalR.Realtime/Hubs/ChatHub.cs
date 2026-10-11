using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Services.Realtime.Api;
using Services.Realtime.Texts;

namespace SignalR.Realtime.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private const string SubClaimType = "sub";

    private readonly IChatMembership membership;

    public ChatHub(IChatMembership membership)
    {
        this.membership = membership;
    }

    public async Task JoinChat(Guid chatId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            throw new HubException(ChatNotifyTexts.Hub.Unauthorized);
        }

        if (!await membership.IsParticipantAsync(chatId, userId.Value, Context.ConnectionAborted))
        {
            throw new HubException(ChatNotifyTexts.Hub.Forbidden);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ChatHubMethods.ChatGroup(chatId), Context.ConnectionAborted);
    }

    public Task LeaveChat(Guid chatId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatHubMethods.ChatGroup(chatId), Context.ConnectionAborted);
    }

    private Guid? GetUserId()
    {
        var value = Context.User?.FindFirstValue(SubClaimType)
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
    }
}
