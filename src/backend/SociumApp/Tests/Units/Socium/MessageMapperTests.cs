using Datasource.Socium.Ef.Models;
using Services.Socium.Mapping;
using Services.Socium.Ports;
using Services.Socium.Texts;

namespace Tests.Units.Socium;

public sealed class MessageMapperTests
{
    private static Message CreateMessage() => new()
    {
        Id = Guid.CreateVersion7(),
        ChatId = Guid.CreateVersion7(),
        Text = "Привет\nмир",
        CreatedAt = new DateTime(2026, 10, 9, 12, 30, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void Message_Apply_WithNewText_ShouldSetTextAndKeepIdChatAndCreatedAt()
    {
        var message = CreateMessage();
        var id = message.Id;
        var chatId = message.ChatId;
        var createdAt = message.CreatedAt;

        MessageMapper.Apply(message, "Изменено");

        Assert.Equal("Изменено", message.Text);
        Assert.Equal(id, message.Id);
        Assert.Equal(chatId, message.ChatId);
        Assert.Equal(createdAt, message.CreatedAt);
    }

    [Fact]
    public void CreatePageResponse_Map_WithNewMessage_ShouldCopyChatIdAndText()
    {
        var message = CreateMessage();

        var response = MessageMapper.ToCreatePageResponse(message);

        Assert.Equal(message.ChatId, response.ChatId);
        Assert.Equal(message.Text, response.Text);
    }

    [Fact]
    public void UpdatePageResponse_Map_WithExistingMessage_ShouldCopyAllFields()
    {
        var message = CreateMessage();

        var response = MessageMapper.ToUpdatePageResponse(message);

        Assert.Equal(message.Id, response.Id);
        Assert.Equal(message.ChatId, response.ChatId);
        Assert.Equal(message.Text, response.Text);
        Assert.Equal(message.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public void InfoPageResponse_Map_WithExistingMessage_ShouldCopyAllFields()
    {
        var message = CreateMessage();

        var response = MessageMapper.ToInfoPageResponse(message);

        Assert.Equal(message.Id, response.Id);
        Assert.Equal(message.ChatId, response.ChatId);
        Assert.Equal(message.Text, response.Text);
        Assert.Equal(message.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public void DeletePageResponse_Map_WithExistingMessage_ShouldCopyAllFields()
    {
        var message = CreateMessage();

        var response = MessageMapper.ToDeletePageResponse(message);

        Assert.Equal(message.Id, response.Id);
        Assert.Equal(message.ChatId, response.ChatId);
        Assert.Equal(message.Text, response.Text);
        Assert.Equal(message.CreatedAt, response.CreatedAt);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ListModel_Map_WithExistingMessage_ShouldCopyIdTextCreatedAtSenderAndCanFlags(bool canUpdate, bool canDelete)
    {
        var message = CreateMessage();
        var user = new UserPortModel { Id = Guid.CreateVersion7(), Login = "ivan", Name = "Иван" };
        message.Sender = new Sender { Id = Guid.CreateVersion7(), MessageId = message.Id, UserId = user.Id };

        var model = MessageMapper.ToListModel(message, user, canUpdate, canDelete);

        Assert.Equal(message.Id, model.Id);
        Assert.Equal(message.Text, model.Text);
        Assert.Equal(message.CreatedAt, model.CreatedAt);
        Assert.Equal(user.Id, model.SenderUserId);
        Assert.Equal(user.Name, model.SenderName);
        Assert.Equal(canUpdate, model.CanUpdate);
        Assert.Equal(canDelete, model.CanDelete);
    }

    [Fact]
    public void ListModel_Map_WithMissingSenderUser_ShouldShowDeletedUser()
    {
        var message = CreateMessage();
        var userId = Guid.CreateVersion7();
        message.Sender = new Sender { Id = Guid.CreateVersion7(), MessageId = message.Id, UserId = userId };

        var model = MessageMapper.ToListModel(message, null, canUpdate: false, canDelete: false);

        Assert.Equal(userId, model.SenderUserId);
        Assert.Equal(UserPortTexts.DeletedUserName, model.SenderName);
        Assert.False(model.CanUpdate);
        Assert.False(model.CanDelete);
    }
}
