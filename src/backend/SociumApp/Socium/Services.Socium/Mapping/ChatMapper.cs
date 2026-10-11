using Datasource.Socium.Ef.Models;
using Services.Socium.Models;

namespace Services.Socium.Mapping;

public static class ChatMapper
{
    public static void Apply(Chat model, string name)
    {
        model.Name = name;
    }

    public static ChatCreatePageResponse ToCreatePageResponse(Chat model)
    {
        return new ChatCreatePageResponse
        {
            RoomId = model.RoomId,
            Name = model.Name,
        };
    }

    public static ChatUpdatePageResponse ToUpdatePageResponse(Chat model)
    {
        return new ChatUpdatePageResponse
        {
            Id = model.Id,
            RoomId = model.RoomId,
            Name = model.Name,
        };
    }

    public static ChatInfoPageResponse ToInfoPageResponse(Chat model, Participant? currentParticipant)
    {
        return new ChatInfoPageResponse
        {
            Id = model.Id,
            RoomId = model.RoomId,
            Name = model.Name,
            IsParticipant = currentParticipant != null,
            IsAdmin = currentParticipant?.IsAdmin ?? false,
        };
    }

    public static ChatListModel ToListModel(
        Chat model,
        bool isAdmin,
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
            IsAdmin = isAdmin,
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

    public static ChatDeletePageResponse ToDeletePageResponse(Chat model)
    {
        return new ChatDeletePageResponse
        {
            Id = model.Id,
            RoomId = model.RoomId,
            Name = model.Name,
        };
    }
}
