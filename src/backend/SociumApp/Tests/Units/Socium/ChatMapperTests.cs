using Datasource.Socium.Ef.Models;
using Services.Socium.Enums;
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
    public void CreatePageResponse_Map_WithNewChat_ShouldCopyIdRoomIdNameAndCanCreate()
    {
        var chat = CreateChat();

        var response = ChatMapper.ToCreatePageResponse(chat, canCreate: true);

        Assert.Equal(chat.Id, response.Id);
        Assert.Equal(chat.RoomId, response.RoomId);
        Assert.Equal(chat.Name, response.Name);
        Assert.True(response.CanCreate);
    }

    [Fact]
    public void ChangedEvent_Map_WithKindAndName_ShouldCopyFields()
    {
        var chatId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();

        var domainEvent = ChatMapper.ToChangedEvent(ChatChangedKind.Updated, chatId, roomId, "Объявления");

        Assert.Equal(ChatChangedKind.Updated, domainEvent.Kind);
        Assert.Equal(chatId, domainEvent.ChatId);
        Assert.Equal(roomId, domainEvent.RoomId);
        Assert.Equal("Объявления", domainEvent.Name);
    }

    [Fact]
    public void NotifyChatChanged_Map_WithChangedEvent_ShouldUseKindString()
    {
        var chatId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var domainEvent = ChatMapper.ToChangedEvent(ChatChangedKind.Deleted, chatId, roomId, "Общий");

        var notify = ChatMapper.ToNotifyChatChanged(domainEvent);

        Assert.Equal("deleted", notify.Kind);
        Assert.Equal(chatId, notify.ChatId);
        Assert.Equal(roomId, notify.RoomId);
        Assert.Equal("Общий", notify.Name);
    }

    [Fact]
    public void CreateStageRequest_Map_WithValues_ShouldCopyRoomIdCreatorAndName()
    {
        var roomId = Guid.CreateVersion7();
        var creatorUserId = Guid.CreateVersion7();

        var request = ChatMapper.ToCreateStageRequest(roomId, creatorUserId, "Новый чат");

        Assert.Equal(roomId, request.RoomId);
        Assert.Equal(creatorUserId, request.CreatorUserId);
        Assert.Equal("Новый чат", request.Name);
    }

    [Fact]
    public void GetNewOptions_Map_WithRoomId_ShouldCopyRoomId()
    {
        var roomId = Guid.CreateVersion7();

        var options = ChatMapper.ToGetNewOptions(roomId);

        Assert.Equal(roomId, options.RoomId);
    }

    [Fact]
    public void UpdatePageResponse_Map_WithExistingChat_ShouldCopyIdRoomIdNameAndCanUpdate()
    {
        var chat = CreateChat();

        var response = ChatMapper.ToUpdatePageResponse(chat, canUpdate: true);

        Assert.Equal(chat.Id, response.Id);
        Assert.Equal(chat.RoomId, response.RoomId);
        Assert.Equal(chat.Name, response.Name);
        Assert.True(response.CanUpdate);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, true, true)]
    [InlineData(true, false, true, false)]
    public void InfoPageResponse_Map_WithFlags_ShouldCopyIdRoomIdNameAndFlags(
        bool isParticipant,
        bool isAdmin,
        bool canUpdate,
        bool canDelete)
    {
        var chat = CreateChat();

        var response = ChatMapper.ToInfoPageResponse(chat, isParticipant, isAdmin, canUpdate, canDelete);

        Assert.Equal(chat.Id, response.Id);
        Assert.Equal(chat.RoomId, response.RoomId);
        Assert.Equal(chat.Name, response.Name);
        Assert.Equal(isParticipant, response.IsParticipant);
        Assert.Equal(isAdmin, response.IsAdmin);
        Assert.Equal(canUpdate, response.CanUpdate);
        Assert.Equal(canDelete, response.CanDelete);
    }

    [Fact]
    public void DeletePageResponse_Map_WithExistingChat_ShouldCopyIdRoomIdNameAndCanDelete()
    {
        var chat = CreateChat();

        var response = ChatMapper.ToDeletePageResponse(chat, canDelete: true);

        Assert.Equal(chat.Id, response.Id);
        Assert.Equal(chat.RoomId, response.RoomId);
        Assert.Equal(chat.Name, response.Name);
        Assert.True(response.CanDelete);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ListModel_Map_WithExistingChat_ShouldCopyIdNameAndCanFlags(bool canUpdate, bool canDelete)
    {
        var chat = CreateChat();

        var model = ChatMapper.ToListModel(chat, canUpdate, canDelete);

        Assert.Equal(chat.Id, model.Id);
        Assert.Equal(chat.Name, model.Name);
        Assert.Equal(canUpdate, model.CanUpdate);
        Assert.Equal(canDelete, model.CanDelete);
        Assert.Equal(0, model.UnreadCount);
    }

    [Fact]
    public void ListModel_Map_WithPreviewAndUnread_ShouldCopyFields()
    {
        var chat = CreateChat();
        var at = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var senderUserId = Guid.CreateVersion7();
        var model = ChatMapper.ToListModel(
            chat,
            canUpdate: false,
            canDelete: false,
            unreadCount: 4,
            "привет",
            at,
            "Анна",
            senderUserId,
            lastMessageHasAttachments: true);

        Assert.Equal(4, model.UnreadCount);
        Assert.Equal("привет", model.LastMessageText);
        Assert.Equal(at, model.LastMessageAt);
        Assert.Equal("Анна", model.LastMessageSenderName);
        Assert.Equal(senderUserId, model.LastMessageSenderUserId);
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
