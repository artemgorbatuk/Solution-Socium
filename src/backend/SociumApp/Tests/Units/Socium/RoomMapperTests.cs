using Datasource.Socium.Ef.Models;
using Services.Socium.Mapping;

namespace Tests.Units.Socium;

public sealed class RoomMapperTests
{
    private static Room CreateRoom() => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "Room",
    };

    [Fact]
    public void Room_Apply_WithNewName_ShouldSetNameAndKeepId()
    {
        var room = CreateRoom();
        var id = room.Id;

        RoomMapper.Apply(room, "Renamed");

        Assert.Equal("Renamed", room.Name);
        Assert.Equal(id, room.Id);
    }

    [Fact]
    public void CreatePageResponse_Map_WithExistingRoom_ShouldCopyName()
    {
        var room = CreateRoom();

        var response = RoomMapper.ToCreatePageResponse(room);

        Assert.Equal(room.Name, response.Name);
    }

    [Fact]
    public void UpdatePageResponse_Map_WithExistingRoom_ShouldCopyIdAndName()
    {
        var room = CreateRoom();

        var response = RoomMapper.ToUpdatePageResponse(room);

        Assert.Equal(room.Id, response.Id);
        Assert.Equal(room.Name, response.Name);
    }

    [Fact]
    public void InfoPageResponse_Map_WithExistingRoom_ShouldCopyIdAndName()
    {
        var room = CreateRoom();

        var response = RoomMapper.ToInfoPageResponse(room);

        Assert.Equal(room.Id, response.Id);
        Assert.Equal(room.Name, response.Name);
    }

    [Fact]
    public void DeletePageResponse_Map_WithExistingRoom_ShouldCopyIdAndName()
    {
        var room = CreateRoom();

        var response = RoomMapper.ToDeletePageResponse(room);

        Assert.Equal(room.Id, response.Id);
        Assert.Equal(room.Name, response.Name);
    }

    [Fact]
    public void ListModel_Map_WithExistingRoom_ShouldCopyIdAndName()
    {
        var room = CreateRoom();

        var model = RoomMapper.ToListModel(room);

        Assert.Equal(room.Id, model.Id);
        Assert.Equal(room.Name, model.Name);
    }
}
