using Datasource.Socium.Ef.Models;
using Repositories.Socium.Ef.Options;
using Services.Socium.Models;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Services.Socium.Mapping;

public static class MessageMapper
{
    public static void Apply(Message model, string text)
    {
        model.Text = text;
    }

    public static MessageGetNewOptions ToGetNewOptions(
        Guid chatId,
        DateTime createdAt,
        Guid senderUserId,
        ICollection<Guid>? recipientUserIds = null)
    {
        return new MessageGetNewOptions
        {
            ChatId = chatId,
            CreatedAt = createdAt,
            SenderUserId = senderUserId,
            RecipientUserIds = recipientUserIds ?? [],
        };
    }

    public static MessageQueryOptions ToQueryOptions(Guid? chatId = null, Guid? beforeId = null, int take = 50)
    {
        return new MessageQueryOptions
        {
            ChatId = chatId,
            BeforeId = beforeId,
            Take = take,
        };
    }

    public static MessageCreatePageResponse ToCreatePageResponse(Message model)
    {
        return new MessageCreatePageResponse
        {
            ChatId = model.ChatId,
            Text = model.Text,
        };
    }

    public static MessageUpdatePageResponse ToUpdatePageResponse(Message model)
    {
        return new MessageUpdatePageResponse
        {
            Id = model.Id,
            ChatId = model.ChatId,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
        };
    }

    public static MessageInfoPageResponse ToInfoPageResponse(Message model)
    {
        return new MessageInfoPageResponse
        {
            Id = model.Id,
            ChatId = model.ChatId,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
        };
    }

    public static MessageListModel ToListModel(
        Message model,
        UserPortModel? sender,
        bool canUpdate,
        bool canDelete,
        IReadOnlyList<MessageAttachmentModel>? attachments = null)
    {
        return new MessageListModel
        {
            Id = model.Id,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
            SenderUserId = model.Sender.UserId,
            SenderName = UserPortMapper.ToDisplayName(sender),
            CanUpdate = canUpdate,
            CanDelete = canDelete,
            Attachments = attachments ?? [],
        };
    }

    public static MessageAttachmentModel ToAttachmentModel(FilePortAttachment attachment)
    {
        return new MessageAttachmentModel
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            SizeBytes = attachment.SizeBytes,
        };
    }

    public static MessageDeletePageResponse ToDeletePageResponse(Message model)
    {
        return new MessageDeletePageResponse
        {
            Id = model.Id,
            ChatId = model.ChatId,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
        };
    }

    public static MessageDeleteResponse ToDeleteResponse(bool isDeleted = true)
    {
        return new MessageDeleteResponse
        {
            IsDeleted = isDeleted,
        };
    }

    public static MessageListPageResponse ToListPageResponse(ICollection<MessageListModel> rows, bool hasOlder)
    {
        return new MessageListPageResponse
        {
            Rows = rows,
            RowExists = rows.Count > 0,
            RowCount = rows.Count,
            HasOlder = hasOlder,
        };
    }

    public static MessageDeliveredResponse ToDeliveredResponse(int updatedCount)
    {
        return new MessageDeliveredResponse
        {
            UpdatedCount = updatedCount,
        };
    }

    public static MessageReadResponse ToReadResponse(int updatedCount, int unreadCount)
    {
        return new MessageReadResponse
        {
            UpdatedCount = updatedCount,
            UnreadCount = unreadCount,
        };
    }

    public static ChatNotifyAttachmentModel ToNotifyAttachment(FilePortAttachment attachment)
    {
        return new ChatNotifyAttachmentModel
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            SizeBytes = attachment.SizeBytes,
        };
    }

    public static ChatNotifyMessageModel ToNotifyMessage(
        Message model,
        string senderName,
        IReadOnlyList<FilePortAttachment> attachments)
    {
        return new ChatNotifyMessageModel
        {
            ChatId = model.ChatId,
            Id = model.Id,
            Text = model.Text,
            CreatedAt = model.CreatedAt,
            SenderUserId = model.Sender.UserId,
            SenderName = senderName,
            Attachments =
            [
                .. attachments.Select(ToNotifyAttachment),
            ],
        };
    }

    public static ChatNotifyListModel ToNotifyList(
        Guid chatId,
        Guid roomId,
        Guid targetUserId,
        int unreadCount,
        string? lastMessageText = null,
        DateTime? lastMessageAt = null,
        string? lastMessageSenderName = null,
        Guid? lastMessageSenderUserId = null,
        bool lastMessageHasAttachments = false,
        bool lastMessageCleared = false)
    {
        return new ChatNotifyListModel
        {
            ChatId = chatId,
            RoomId = roomId,
            TargetUserId = targetUserId,
            UnreadCount = unreadCount,
            LastMessageText = lastMessageCleared ? null : lastMessageText,
            LastMessageAt = lastMessageCleared ? null : lastMessageAt,
            LastMessageSenderName = lastMessageCleared ? null : lastMessageSenderName,
            LastMessageSenderUserId = lastMessageCleared ? null : lastMessageSenderUserId,
            LastMessageHasAttachments = !lastMessageCleared && lastMessageHasAttachments,
            LastMessageCleared = lastMessageCleared,
        };
    }
}
