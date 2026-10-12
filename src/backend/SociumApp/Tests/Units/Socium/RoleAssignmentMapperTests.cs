using Datasource.Socium.Ef.Models;
using Services.Socium.Mapping;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Tests.Units.Socium;

public sealed class RoleAssignmentMapperTests
{
    private static readonly Room Room = new() { Id = Guid.CreateVersion7(), Name = "Тест 1" };
    private static readonly Chat Chat = new() { Id = Guid.CreateVersion7(), RoomId = Room.Id, Name = "Чат 1" };

    [Fact]
    public void ChatModel_Map_WithSingleAdmin_ShouldMarkLastChatAdmin()
    {
        var model = RoleAssignmentMapper.ToChatModel(Chat, isParticipant: true, isChatAdmin: true, chatAdminCount: 1);

        Assert.Equal(Chat.Id, model.ChatId);
        Assert.Equal(Chat.Name, model.ChatName);
        Assert.True(model.IsParticipant);
        Assert.True(model.IsChatAdmin);
        Assert.True(model.IsLastChatAdmin);
    }

    [Fact]
    public void ChatModel_Map_WithNotAdmin_ShouldNotMarkLastChatAdmin()
    {
        var model = RoleAssignmentMapper.ToChatModel(Chat, isParticipant: false, isChatAdmin: false, chatAdminCount: 1);

        Assert.False(model.IsLastChatAdmin);
    }

    [Fact]
    public void RoomModel_Map_WithTwoAdmins_ShouldNotMarkLastRoomAdmin()
    {
        var model = RoleAssignmentMapper.ToRoomModel(Room, isRoomAdmin: true, roomAdminCount: 2, chats: []);

        Assert.Equal(Room.Id, model.RoomId);
        Assert.Equal(Room.Name, model.RoomName);
        Assert.True(model.IsRoomAdmin);
        Assert.False(model.IsLastRoomAdmin);
    }

    [Fact]
    public void DisplayName_Map_WithDeletedOrMissingUser_ShouldMarkDeleted()
    {
        var deleted = new UserPortModel { Id = Guid.CreateVersion7(), Login = "ivan", Name = "Иван", IsDeleted = true };
        var active = new UserPortModel { Id = Guid.CreateVersion7(), Login = "petr", Name = "Пётр" };

        Assert.Equal($"Иван {UserPortTexts.DeletedMark}", UserPortMapper.ToDisplayName(deleted));
        Assert.Equal("Пётр", UserPortMapper.ToDisplayName(active));
        Assert.Equal(UserPortTexts.DeletedUserName, UserPortMapper.ToDisplayName(null));
    }
}
