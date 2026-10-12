using Datasource.Socium.Ef.Models;
using Services.Socium.Enums;
using Services.Socium.Events;
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
    public void DeletePageResponse_Map_WithExistingRoom_ShouldCopyIdNameAndChatCount()
    {
        var room = CreateRoom();

        var response = RoomMapper.ToDeletePageResponse(room, 3);

        Assert.Equal(room.Id, response.Id);
        Assert.Equal(room.Name, response.Name);
        Assert.Equal(3, response.ChatCount);
    }

    [Fact]
    public void ListModel_Map_WithExistingRoom_ShouldCopyIdAndName()
    {
        var room = CreateRoom();

        var model = RoomMapper.ToListModel(room);

        Assert.Equal(room.Id, model.Id);
        Assert.Equal(room.Name, model.Name);
    }

    [Fact]
    public void CreatedEvent_Map_WithIds_ShouldCopyRoomIdAndCreatorUserId()
    {
        var roomId = Guid.CreateVersion7();
        var creatorUserId = Guid.CreateVersion7();

        var domainEvent = RoomMapper.ToCreatedEvent(roomId, creatorUserId);

        Assert.Equal(roomId, domainEvent.RoomId);
        Assert.Equal(creatorUserId, domainEvent.CreatorUserId);
    }

    [Fact]
    public void ChangedEvent_Map_WithKindAndName_ShouldCopyFields()
    {
        var roomId = Guid.CreateVersion7();

        var domainEvent = RoomMapper.ToChangedEvent(RoomChangedKind.Updated, roomId, "Столовая");

        Assert.Equal(RoomChangedKind.Updated, domainEvent.Kind);
        Assert.Equal(roomId, domainEvent.RoomId);
        Assert.Equal("Столовая", domainEvent.Name);
    }

    [Fact]
    public void NotifyRoomChanged_Map_WithChangedEvent_ShouldUseKindString()
    {
        var roomId = Guid.CreateVersion7();
        var domainEvent = RoomMapper.ToChangedEvent(RoomChangedKind.Deleted, roomId, "Кухня");

        var notify = RoomMapper.ToNotifyRoomChanged(domainEvent);

        Assert.Equal("deleted", notify.Kind);
        Assert.Equal(roomId, notify.RoomId);
        Assert.Equal("Кухня", notify.Name);
    }
}
