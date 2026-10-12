using Datasource.Socium.Ef.Models;
using Repositories.Socium.Ef.Options;
using Services.Socium.Events;
using Services.Socium.Models;

namespace Services.Socium.Mapping;

public static class ChatMapper
{
    public static void Apply(Chat model, string name)
    {
        model.Name = name;
    }

    public static ChatCreateStageRequest ToCreateStageRequest(Guid roomId, Guid creatorUserId, string name)
    {
        return new ChatCreateStageRequest
        {
            RoomId = roomId,
            CreatorUserId = creatorUserId,
            Name = name,
        };
    }

    public static ChatCreatedEvent ToCreatedEvent(Guid chatId, Guid creatorUserId)
    {
        return new ChatCreatedEvent
        {
            ChatId = chatId,
            CreatorUserId = creatorUserId,
        };
    }

    public static ChatGetNewOptions ToGetNewOptions(Guid roomId)
    {
        return new ChatGetNewOptions
        {
            RoomId = roomId,
        };
    }

    public static ChatQueryOptions ToQueryOptions(Guid? roomId = null)
    {
        return new ChatQueryOptions
        {
            RoomId = roomId,
        };
    }

    public static ChatCreatePageResponse ToCreatePageResponse(Chat model, bool canCreate)
    {
        return new ChatCreatePageResponse
        {
            RoomId = model.RoomId,
            Name = model.Name,
            CanCreate = canCreate,
        };
    }

    public static ChatUpdatePageResponse ToUpdatePageResponse(Chat model, bool canUpdate)
    {
        return new ChatUpdatePageResponse
        {
            Id = model.Id,
            RoomId = model.RoomId,
            Name = model.Name,
            CanUpdate = canUpdate,
        };
    }

    public static ChatInfoPageResponse ToInfoPageResponse(
        Chat model,
        bool isParticipant,
        bool isAdmin,
        bool canUpdate,
        bool canDelete)
    {
        return new ChatInfoPageResponse
        {
            Id = model.Id,
            RoomId = model.RoomId,
            Name = model.Name,
            IsParticipant = isParticipant,
            IsAdmin = isAdmin,
            CanUpdate = canUpdate,
            CanDelete = canDelete,
        };
    }

    public static ChatListModel ToListModel(
        Chat model,
        bool canUpdate,
        bool canDelete,
        int unreadCount = 0,
        string? lastMessageText = null,
        DateTime? lastMessageAt = null,
        string? lastMessageSenderName = null,
        bool lastMessageHasAttachments = false)
    {
        return new ChatListModel
        {
            Id = model.Id,
            Name = model.Name,
            CanUpdate = canUpdate,
            CanDelete = canDelete,
            UnreadCount = unreadCount,
            LastMessageText = lastMessageText,
            LastMessageAt = lastMessageAt,
            LastMessageSenderName = lastMessageSenderName,
            LastMessageHasAttachments = lastMessageHasAttachments,
        };
    }

    public static string TruncatePreview(string text, int maxLength = 80)
    {
        var normalized = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..maxLength].TrimEnd() + "…";
    }

    public static ChatDeletePageResponse ToDeletePageResponse(Chat model, bool canDelete)
    {
        return new ChatDeletePageResponse
        {
            Id = model.Id,
            RoomId = model.RoomId,
            Name = model.Name,
            CanDelete = canDelete,
        };
    }

    public static ChatDeleteResponse ToDeleteResponse(bool isDeleted = true)
    {
        return new ChatDeleteResponse
        {
            IsDeleted = isDeleted,
        };
    }

    public static ChatListPageResponse ToListPageResponse(ICollection<ChatListModel> rows)
    {
        return new ChatListPageResponse
        {
            Rows = rows,
            RowExists = rows.Count > 0,
            RowCount = rows.Count,
        };
    }
}
