using Datasource.Socium.Ef.Models;
using Services.Socium.Mapping;

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

    [Fact]
    public void ListModel_Map_WithExistingMessage_ShouldCopyIdTextAndCreatedAt()
    {
        var message = CreateMessage();

        var model = MessageMapper.ToListModel(message);

        Assert.Equal(message.Id, model.Id);
        Assert.Equal(message.Text, model.Text);
        Assert.Equal(message.CreatedAt, model.CreatedAt);
    }
}
