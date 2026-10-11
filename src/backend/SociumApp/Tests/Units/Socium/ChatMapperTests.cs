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
    public void InfoPageResponse_Map_WithoutParticipant_ShouldCopyIdRoomIdAndNameAndClearFlags()
    {
        var chat = CreateChat();

        var response = ChatMapper.ToInfoPageResponse(chat, null);

        Assert.Equal(chat.Id, response.Id);
        Assert.Equal(chat.RoomId, response.RoomId);
        Assert.Equal(chat.Name, response.Name);
        Assert.False(response.IsParticipant);
        Assert.False(response.IsAdmin);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InfoPageResponse_Map_WithParticipant_ShouldSetParticipantAndCopyAdmin(bool isAdmin)
    {
        var chat = CreateChat();
        var participant = new Participant { Id = Guid.CreateVersion7(), ChatId = chat.Id, UserId = Guid.CreateVersion7(), IsAdmin = isAdmin };

        var response = ChatMapper.ToInfoPageResponse(chat, participant);

        Assert.True(response.IsParticipant);
        Assert.Equal(isAdmin, response.IsAdmin);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ListModel_Map_WithExistingChat_ShouldCopyIdAndNameAndAdmin(bool isAdmin)
    {
        var chat = CreateChat();

        var model = ChatMapper.ToListModel(chat, isAdmin);

        Assert.Equal(chat.Id, model.Id);
        Assert.Equal(chat.Name, model.Name);
        Assert.Equal(isAdmin, model.IsAdmin);
        Assert.Equal(0, model.UnreadCount);
    }

    [Fact]
    public void ListModel_Map_WithPreviewAndUnread_ShouldCopyFields()
    {
        var chat = CreateChat();
        var at = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var model = ChatMapper.ToListModel(chat, isAdmin: false, unreadCount: 4, "привет", at, "Анна", lastMessageHasAttachments: true);

        Assert.Equal(4, model.UnreadCount);
        Assert.Equal("привет", model.LastMessageText);
        Assert.Equal(at, model.LastMessageAt);
        Assert.Equal("Анна", model.LastMessageSenderName);
        Assert.True(model.LastMessageHasAttachments);
    }

    [Theory]
    [InlineData("короткий", "короткий")]
    [InlineData("  a\nb  ", "a b")]
    public void TruncatePreview_WithShortText_ShouldNormalizeWhitespace(string input, string expected)
    {
        Assert.Equal(expected, ChatMapper.TruncatePreview(input));
    }

    [Fact]
    public void TruncatePreview_WithLongText_ShouldEllipsize()
    {
        var text = new string('я', 100);

        var preview = ChatMapper.TruncatePreview(text, maxLength: 10);

        Assert.Equal(new string('я', 10) + "…", preview);
    }
}
