using Services.Realtime.Api;
using Services.Realtime.Models;
using Services.Socium.Ports;

namespace WebApi.Ports;

/// <summary>
/// Адаптер порта Socium к сервису уведомлений области Realtime.
/// </summary>
public class ChatNotifyPort : IChatNotifyPort
{
    private readonly IServiceChatNotify serviceChatNotify;

    public ChatNotifyPort(IServiceChatNotify serviceChatNotify)
    {
        this.serviceChatNotify = serviceChatNotify;
    }

    public Task NotifyMessageCreatedAsync(ChatNotifyMessageModel message, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyMessageCreatedAsync(ToRealtime(message), cancellationToken);

    public Task NotifyMessageUpdatedAsync(ChatNotifyMessageModel message, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyMessageUpdatedAsync(ToRealtime(message), cancellationToken);

    public Task NotifyMessageDeletedAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyMessageDeletedAsync(
            new ChatMessageDeletedNotifyModel { ChatId = chatId, Id = messageId },
            cancellationToken);

    public Task NotifyParticipantJoinedAsync(ChatNotifyParticipantModel participant, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyParticipantJoinedAsync(ToRealtime(participant), cancellationToken);

    public Task NotifyParticipantLeftAsync(Guid chatId, Guid participantId, Guid userId, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyParticipantLeftAsync(
            new ChatParticipantLeftNotifyModel { ChatId = chatId, Id = participantId, UserId = userId },
            cancellationToken);

    public Task NotifyParticipantRoleChangedAsync(ChatNotifyParticipantModel participant, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyParticipantRoleChangedAsync(ToRealtime(participant), cancellationToken);

    public Task NotifyChatListUpdatedAsync(ChatNotifyListModel update, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyChatListUpdatedAsync(
            new ChatListNotifyModel
            {
                ChatId = update.ChatId,
                RoomId = update.RoomId,
                TargetUserId = update.TargetUserId,
                UnreadCount = update.UnreadCount,
                LastMessageText = update.LastMessageText,
                LastMessageAt = update.LastMessageAt,
                LastMessageSenderName = update.LastMessageSenderName,
                LastMessageHasAttachments = update.LastMessageHasAttachments,
                LastMessageCleared = update.LastMessageCleared,
            },
            cancellationToken);

    public Task NotifyRoomChangedAsync(ChatNotifyRoomChangedModel change, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyRoomChangedAsync(
            new RoomChangedNotifyModel
            {
                Kind = change.Kind,
                RoomId = change.RoomId,
                Name = change.Name,
            },
            cancellationToken);

    public Task NotifyChatChangedAsync(ChatNotifyChatChangedModel change, CancellationToken cancellationToken = default)
        => serviceChatNotify.NotifyChatChangedAsync(
            new ChatChangedNotifyModel
            {
                Kind = change.Kind,
                RoomId = change.RoomId,
                ChatId = change.ChatId,
                Name = change.Name,
            },
            cancellationToken);

    private static ChatMessageNotifyModel ToRealtime(ChatNotifyMessageModel message) => new()
    {
        ChatId = message.ChatId,
        Id = message.Id,
        Text = message.Text,
        CreatedAt = message.CreatedAt,
        SenderUserId = message.SenderUserId,
        SenderName = message.SenderName,
        Attachments =
        [
            .. message.Attachments.Select(attachment => new ChatMessageAttachmentNotifyModel
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                SizeBytes = attachment.SizeBytes,
            }),
        ],
    };

    private static ChatParticipantNotifyModel ToRealtime(ChatNotifyParticipantModel participant) => new()
    {
        ChatId = participant.ChatId,
        Id = participant.Id,
        UserId = participant.UserId,
        Login = participant.Login,
        Name = participant.Name,
        IsAdmin = participant.IsAdmin,
    };
}
