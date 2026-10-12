using Microsoft.Extensions.Logging;
using Services.Realtime.Models;

namespace Services.Realtime.Api;

public interface IServiceChatNotify
{
    Task NotifyMessageCreatedAsync(ChatMessageNotifyModel message, CancellationToken cancellationToken = default);
    Task NotifyMessageUpdatedAsync(ChatMessageNotifyModel message, CancellationToken cancellationToken = default);
    Task NotifyMessageDeletedAsync(ChatMessageDeletedNotifyModel deleted, CancellationToken cancellationToken = default);
    Task NotifyParticipantJoinedAsync(ChatParticipantNotifyModel participant, CancellationToken cancellationToken = default);
    Task NotifyParticipantLeftAsync(ChatParticipantLeftNotifyModel left, CancellationToken cancellationToken = default);
    Task NotifyParticipantRoleChangedAsync(ChatParticipantNotifyModel participant, CancellationToken cancellationToken = default);
    Task NotifyChatListUpdatedAsync(ChatListNotifyModel update, CancellationToken cancellationToken = default);
    Task NotifyRoomChangedAsync(RoomChangedNotifyModel change, CancellationToken cancellationToken = default);
    Task NotifyChatChangedAsync(ChatChangedNotifyModel change, CancellationToken cancellationToken = default);
    Task NotifyUserChangedAsync(UserChangedNotifyModel change, CancellationToken cancellationToken = default);
    Task NotifyJoinRequestChangedAsync(JoinRequestChangedNotifyModel change, CancellationToken cancellationToken = default);
}

public class ServiceChatNotify : IServiceChatNotify
{
    private readonly IChatConnection connection;
    private readonly ILogger<ServiceChatNotify> logger;

    public ServiceChatNotify(IChatConnection connection, ILogger<ServiceChatNotify> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }

    public Task NotifyMessageCreatedAsync(ChatMessageNotifyModel message, CancellationToken cancellationToken = default)
        => SendSafeAsync(message.ChatId, ChatHubMethods.MessageCreated, message, cancellationToken);

    public Task NotifyMessageUpdatedAsync(ChatMessageNotifyModel message, CancellationToken cancellationToken = default)
        => SendSafeAsync(message.ChatId, ChatHubMethods.MessageUpdated, message, cancellationToken);

    public Task NotifyMessageDeletedAsync(ChatMessageDeletedNotifyModel deleted, CancellationToken cancellationToken = default)
        => SendSafeAsync(deleted.ChatId, ChatHubMethods.MessageDeleted, deleted, cancellationToken);

    public async Task NotifyParticipantJoinedAsync(ChatParticipantNotifyModel participant, CancellationToken cancellationToken = default)
    {
        await SendSafeAsync(participant.ChatId, ChatHubMethods.ParticipantJoined, participant, cancellationToken);
        await SendToUserSafeAsync(participant.UserId, ChatHubMethods.ParticipantJoined, participant, cancellationToken);
    }

    public async Task NotifyParticipantLeftAsync(ChatParticipantLeftNotifyModel left, CancellationToken cancellationToken = default)
    {
        await SendSafeAsync(left.ChatId, ChatHubMethods.ParticipantLeft, left, cancellationToken);
        await SendToUserSafeAsync(left.UserId, ChatHubMethods.ParticipantLeft, left, cancellationToken);
    }

    public Task NotifyParticipantRoleChangedAsync(ChatParticipantNotifyModel participant, CancellationToken cancellationToken = default)
        => SendSafeAsync(participant.ChatId, ChatHubMethods.ParticipantRoleChanged, participant, cancellationToken);

    public Task NotifyChatListUpdatedAsync(ChatListNotifyModel update, CancellationToken cancellationToken = default)
        => SendToUserSafeAsync(update.TargetUserId, ChatHubMethods.ChatListUpdated, update, cancellationToken);

    public Task NotifyRoomChangedAsync(RoomChangedNotifyModel change, CancellationToken cancellationToken = default)
        => SendToAllSafeAsync(ChatHubMethods.RoomChanged, change, cancellationToken);

    public Task NotifyChatChangedAsync(ChatChangedNotifyModel change, CancellationToken cancellationToken = default)
        => SendToAllSafeAsync(ChatHubMethods.ChatChanged, change, cancellationToken);

    public Task NotifyUserChangedAsync(UserChangedNotifyModel change, CancellationToken cancellationToken = default)
        => SendToAllSafeAsync(ChatHubMethods.UserChanged, change, cancellationToken);

    public async Task NotifyJoinRequestChangedAsync(
        JoinRequestChangedNotifyModel change,
        CancellationToken cancellationToken = default)
    {
        await SendSafeAsync(change.ChatId, ChatHubMethods.JoinRequestChanged, change, cancellationToken);
        await SendToUserSafeAsync(change.UserId, ChatHubMethods.JoinRequestChanged, change, cancellationToken);
    }

    private async Task SendSafeAsync(Guid chatId, string method, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await connection.SendToChatAsync(chatId, method, payload, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Realtime notify failed: {Method} chat {ChatId}", method, chatId);
        }
    }

    private async Task SendToUserSafeAsync(Guid userId, string method, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await connection.SendToUserAsync(userId, method, payload, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Realtime notify failed: {Method} user {UserId}", method, userId);
        }
    }

    private async Task SendToAllSafeAsync(string method, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await connection.SendToAllAsync(method, payload, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Realtime notify failed: {Method} (all)", method);
        }
    }
}
