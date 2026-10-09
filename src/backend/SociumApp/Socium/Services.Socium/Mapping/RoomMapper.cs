using Datasource.Socium.Ef.Models;
using Services.Socium.Models;

namespace Services.Socium.Mapping;

public static class RoomMapper
{
    public static void Apply(Room model, string name)
    {
        model.Name = name;
    }

    public static RoomCreatePageResponse ToCreatePageResponse(Room model)
    {
        return new RoomCreatePageResponse
        {
            Name = model.Name,
        };
    }

    public static RoomUpdatePageResponse ToUpdatePageResponse(Room model)
    {
        return new RoomUpdatePageResponse
        {
            Id = model.Id,
            Name = model.Name,
        };
    }

    public static RoomInfoPageResponse ToInfoPageResponse(Room model)
    {
        return new RoomInfoPageResponse
        {
            Id = model.Id,
            Name = model.Name,
        };
    }

    public static RoomListModel ToListModel(Room model)
    {
        return new RoomListModel
        {
            Id = model.Id,
            Name = model.Name,
        };
    }

    public static RoomDeletePageResponse ToDeletePageResponse(Room model, int chatCount)
    {
        return new RoomDeletePageResponse
        {
            Id = model.Id,
            Name = model.Name,
            ChatCount = chatCount,
        };
    }
}
