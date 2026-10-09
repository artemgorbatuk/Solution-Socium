using Datasource.Socium.Ef.Models;
using Services.Socium.Mapping;

namespace Tests.Units.Socium;

public sealed class ChatMapperTests
{
    private static Chat CreateChat() => new()
    {
        Id = Guid.CreateVersion7(),
        RoomId = Guid.CreateVersion7(),
        Name = "Chat",
    };

    [Fact]
    public void Chat_Apply_WithNewName_ShouldSetNameAndKeepIdAndRoom()
    {
        var chat = CreateChat();
        var id = chat.Id;
        var roomId = chat.RoomId;

        ChatMapper.Apply(chat, "Renamed");

        Assert.Equal("Renamed", chat.Name);
        Assert.Equal(id, chat.Id);
        Assert.Equal(roomId, chat.RoomId);
    }

    [Fact]
    public void CreatePageResponse_Map_WithNewChat_ShouldCopyRoomIdAndName()
    {
        var chat = CreateChat();

        var response = ChatMapper.ToCreatePageResponse(chat);

        Assert.Equal(chat.RoomId, response.RoomId);
        Assert.Equal(chat.Name, response.Name);
    }

    [Fact]
    public void UpdatePageResponse_Map_WithExistingChat_ShouldCopyIdRoomIdAndName()
    {
        var chat = CreateChat();

        var response = ChatMapper.ToUpdatePageResponse(chat);

        Assert.Equal(chat.Id, response.Id);
        Assert.Equal(chat.RoomId, response.RoomId);
        Assert.Equal(chat.Name, response.Name);
    }

    [Fact]
    public void InfoPageResponse_Map_WithExistingChat_ShouldCopyIdRoomIdAndName()
    {
        var chat = CreateChat();

        var response = ChatMapper.ToInfoPageResponse(chat);

        Assert.Equal(chat.Id, response.Id);
        Assert.Equal(chat.RoomId, response.RoomId);
        Assert.Equal(chat.Name, response.Name);
    }

    [Fact]
    public void DeletePageResponse_Map_WithExistingChat_ShouldCopyIdRoomIdAndName()
    {
        var chat = CreateChat();

        var response = ChatMapper.ToDeletePageResponse(chat);

        Assert.Equal(chat.Id, response.Id);
        Assert.Equal(chat.RoomId, response.RoomId);
        Assert.Equal(chat.Name, response.Name);
    }

    [Fact]
    public void ListModel_Map_WithExistingChat_ShouldCopyIdAndName()
    {
        var chat = CreateChat();

        var model = ChatMapper.ToListModel(chat);

        Assert.Equal(chat.Id, model.Id);
        Assert.Equal(chat.Name, model.Name);
    }
}
