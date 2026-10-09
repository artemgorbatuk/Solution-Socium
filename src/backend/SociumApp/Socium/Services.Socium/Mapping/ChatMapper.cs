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

    public static ChatListModel ToListModel(Chat model, bool isAdmin)
    {
        return new ChatListModel
        {
            Id = model.Id,
            Name = model.Name,
            IsAdmin = isAdmin,
        };
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
