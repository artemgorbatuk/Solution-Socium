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
    public void CreatePageResponse_Map_WithExistingRoom_ShouldCopyNameAndCanCreate()
    {
        var room = CreateRoom();

        var response = RoomMapper.ToCreatePageResponse(room, canCreate: true);

        Assert.Equal(room.Name, response.Name);
        Assert.True(response.CanCreate);
    }

    [Fact]
    public void UpdatePageResponse_Map_WithExistingRoom_ShouldCopyIdNameAndCanUpdate()
    {
        var room = CreateRoom();

        var response = RoomMapper.ToUpdatePageResponse(room, canUpdate: true);

        Assert.Equal(room.Id, response.Id);
        Assert.Equal(room.Name, response.Name);
        Assert.True(response.CanUpdate);
    }

    [Fact]
    public void InfoPageResponse_Map_WithExistingRoom_ShouldCopyIdNameAndFlags()
    {
        var room = CreateRoom();

        var response = RoomMapper.ToInfoPageResponse(room, canUpdate: true, canDelete: false);

        Assert.Equal(room.Id, response.Id);
        Assert.Equal(room.Name, response.Name);
        Assert.True(response.CanUpdate);
        Assert.False(response.CanDelete);
    }

    [Fact]
    public void DeletePageResponse_Map_WithExistingRoom_ShouldCopyIdNameChatCountAndCanDelete()
    {
        var room = CreateRoom();

        var response = RoomMapper.ToDeletePageResponse(room, 3, canDelete: true);

        Assert.Equal(room.Id, response.Id);
        Assert.Equal(room.Name, response.Name);
        Assert.Equal(3, response.ChatCount);
        Assert.True(response.CanDelete);
    }

    [Fact]
    public void ListModel_Map_WithExistingRoom_ShouldCopyIdNameAndFlags()
    {
        var room = CreateRoom();

        var model = RoomMapper.ToListModel(room, canUpdate: false, canDelete: true);

        Assert.Equal(room.Id, model.Id);
        Assert.Equal(room.Name, model.Name);
        Assert.False(model.CanUpdate);
        Assert.True(model.CanDelete);
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
