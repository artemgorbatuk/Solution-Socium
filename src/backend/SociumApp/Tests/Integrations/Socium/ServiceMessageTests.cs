using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Api;
using Services.Socium.Models;
using Services.Socium.Texts;

namespace Tests.Integrations.Socium;

[Collection(SociumServiceCollection.Name)]
public sealed class ServiceMessageTests(SociumServiceFixture fixture)
{
    private static string UniqueText() => $"Message {Guid.NewGuid():N}";

    private Task<ResponseInfo<T>> RunAsync<T>(Func<IServiceMessage, Task<ResponseInfo<T>>> action) where T : class
        => fixture.RunAsync(action);

    private async Task<Guid> CreateChatAsync()
    {
        var roomId = await fixture.CreateRoomAsync($"Room {Guid.NewGuid():N}");
        return await fixture.CreateChatAsync(roomId, $"Chat {Guid.NewGuid():N}");
    }

    private Task<ResponseInfo<MessageListPageResponse>> GetListAsync(Guid chatId)
        => RunAsync(service => service.DisplayListPageAsync(new MessageListPageRequest { ChatId = chatId }));

    [Fact]
    public async Task Create_Load_WithExistingChat_ShouldReturnLoadedWithChatAndEmptyText()
    {
        var chatId = await CreateChatAsync();

        var result = await RunAsync(service => service.DisplayCreatePageAsync(new MessageCreatePageRequest { ChatId = chatId }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(chatId, result.Response!.ChatId);
        Assert.Equal(string.Empty, result.Response.Text);
    }

    [Fact]
    public async Task Create_Load_WithEmptyChatId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayCreatePageAsync(new MessageCreatePageRequest { ChatId = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Load_WithUnknownChatId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayCreatePageAsync(new MessageCreatePageRequest { ChatId = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithPaddedMultilineText_ShouldSaveTrimmedTextKeepLinesAndSetUtcTime()
    {
        var chatId = await CreateChatAsync();
        var text = $"{UniqueText()}\n\nВторая строка";
        var before = DateTime.UtcNow;

        var result = await RunAsync(service => service.CreateAsync(new MessageCreateRequest { ChatId = chatId, Text = $" \n {text} \n" }));

        var after = DateTime.UtcNow;
        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.Equal(MessageCrudTexts.Messages.Success.CreateCompleted, result.MessageInfo.MessageText);
        var row = Assert.Single((await GetListAsync(chatId)).Response!.Rows);
        Assert.Equal(text, row.Text);
        Assert.Equal(7, row.Id.Version);
        Assert.Equal(DateTimeKind.Utc, row.CreatedAt.Kind);
        Assert.InRange(row.CreatedAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public async Task Create_Submit_WithMillionCharacters_ShouldSaveWholeText()
    {
        var chatId = await CreateChatAsync();
        var text = new string('я', 1_000_000);

        var result = await RunAsync(service => service.CreateAsync(new MessageCreateRequest { ChatId = chatId, Text = text }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var row = Assert.Single((await GetListAsync(chatId)).Response!.Rows);
        Assert.Equal(text.Length, row.Text.Length);
    }

    [Fact]
    public async Task Create_Submit_WithNullRequest_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.CreateAsync(null!));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.RequestCannotBeNull, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithEmptyChatId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.CreateAsync(new MessageCreateRequest { ChatId = Guid.Empty, Text = UniqueText() }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithUnknownChatId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.CreateAsync(new MessageCreateRequest { ChatId = Guid.CreateVersion7(), Text = UniqueText() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithUnknownChatIdAndEmptyText_ShouldReturnNotFoundBeforeInvalid()
    {
        var result = await RunAsync(service => service.CreateAsync(new MessageCreateRequest { ChatId = Guid.CreateVersion7(), Text = "" }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Create_Submit_WithBlankText_ShouldReturnInvalid()
    {
        var chatId = await CreateChatAsync();

        var result = await RunAsync(service => service.CreateAsync(new MessageCreateRequest { ChatId = chatId, Text = " \n\t " }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.TextNotEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task List_Load_WithMessagesInSeveralChats_ShouldReturnOnlyChatMessagesInSendOrder()
    {
        var chatId = await CreateChatAsync();
        var otherChatId = await CreateChatAsync();
        await fixture.CreateMessageAsync(chatId, "Первое");
        await fixture.CreateMessageAsync(otherChatId, "Чужое");
        await fixture.CreateMessageAsync(chatId, "Второе");
        await fixture.CreateMessageAsync(chatId, "Третье");

        var result = await GetListAsync(chatId);

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.RowExists);
        Assert.Equal(3, result.Response.RowCount);
        Assert.Equal(["Первое", "Второе", "Третье"], result.Response.Rows.Select(row => row.Text));
    }

    [Fact]
    public async Task List_Load_WithChatWithoutMessages_ShouldReturnEmptyList()
    {
        var chatId = await CreateChatAsync();

        var result = await GetListAsync(chatId);

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.False(result.Response!.RowExists);
        Assert.Equal(0, result.Response.RowCount);
        Assert.Empty(result.Response.Rows);
    }

    [Fact]
    public async Task List_Load_WithEmptyChatId_ShouldReturnBadRequest()
    {
        var result = await GetListAsync(Guid.Empty);

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task List_Load_WithUnknownChatId_ShouldReturnNotFound()
    {
        var result = await GetListAsync(Guid.CreateVersion7());

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Info_Load_WithExistingMessage_ShouldReturnMessage()
    {
        var chatId = await CreateChatAsync();
        var text = UniqueText();
        var id = await fixture.CreateMessageAsync(chatId, text);

        var result = await RunAsync(service => service.DisplayInfoPageAsync(new MessageInfoPageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(chatId, result.Response.ChatId);
        Assert.Equal(text, result.Response.Text);
        Assert.NotEqual(default, result.Response.CreatedAt);
    }

    [Fact]
    public async Task Info_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayInfoPageAsync(new MessageInfoPageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.MessageNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Info_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayInfoPageAsync(new MessageInfoPageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Load_WithExistingMessage_ShouldReturnMessage()
    {
        var chatId = await CreateChatAsync();
        var text = UniqueText();
        var id = await fixture.CreateMessageAsync(chatId, text);

        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new MessageUpdatePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(chatId, result.Response.ChatId);
        Assert.Equal(text, result.Response.Text);
    }

    [Fact]
    public async Task Update_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new MessageUpdatePageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.MessageNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new MessageUpdatePageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithNewText_ShouldSaveTrimmedTextAndKeepChatAndCreatedAt()
    {
        var chatId = await CreateChatAsync();
        var id = await fixture.CreateMessageAsync(chatId, UniqueText());
        var before = await RunAsync(service => service.DisplayInfoPageAsync(new MessageInfoPageRequest { Id = id }));
        var newText = $"{UniqueText()}\nисправлено";

        var result = await RunAsync(service => service.UpdateAsync(new MessageUpdateRequest { Id = id, Text = $"  {newText} " }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new MessageInfoPageRequest { Id = id }));
        Assert.Equal(newText, info.Response!.Text);
        Assert.Equal(chatId, info.Response.ChatId);
        Assert.Equal(before.Response!.CreatedAt, info.Response.CreatedAt);
    }

    [Fact]
    public async Task Update_Submit_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.UpdateAsync(new MessageUpdateRequest { Id = Guid.Empty, Text = UniqueText() }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.UpdateAsync(new MessageUpdateRequest { Id = Guid.CreateVersion7(), Text = UniqueText() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.MessageNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithBlankText_ShouldReturnInvalidAndKeepText()
    {
        var chatId = await CreateChatAsync();
        var text = UniqueText();
        var id = await fixture.CreateMessageAsync(chatId, text);

        var result = await RunAsync(service => service.UpdateAsync(new MessageUpdateRequest { Id = id, Text = " " }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.TextNotEmpty, result.MessageInfo.MessageText);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new MessageInfoPageRequest { Id = id }));
        Assert.Equal(text, info.Response!.Text);
    }

    [Fact]
    public async Task Delete_Load_WithExistingMessage_ShouldReturnMessage()
    {
        var chatId = await CreateChatAsync();
        var text = UniqueText();
        var id = await fixture.CreateMessageAsync(chatId, text);

        var result = await RunAsync(service => service.DisplayDeletePageAsync(new MessageDeletePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(chatId, result.Response.ChatId);
        Assert.Equal(text, result.Response.Text);
    }

    [Fact]
    public async Task Delete_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayDeletePageAsync(new MessageDeletePageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.MessageNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayDeletePageAsync(new MessageDeletePageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Submit_WithExistingMessage_ShouldDeleteOnlyThisMessage()
    {
        var chatId = await CreateChatAsync();
        var id = await fixture.CreateMessageAsync(chatId, "A");
        var otherId = await fixture.CreateMessageAsync(chatId, "B");

        var result = await RunAsync(service => service.DeleteAsync(new MessageDeleteRequest { Id = id }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.IsDeleted);
        var row = Assert.Single((await GetListAsync(chatId)).Response!.Rows);
        Assert.Equal(otherId, row.Id);
    }

    [Fact]
    public async Task Delete_Submit_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DeleteAsync(new MessageDeleteRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(MessageCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Submit_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DeleteAsync(new MessageDeleteRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }
}
