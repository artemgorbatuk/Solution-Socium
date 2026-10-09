using System.Net;
using System.Net.Http.Json;
using Services.Shared.Enums;
using Services.Socium.Models;
using Services.Socium.Texts;
using WebApi.Controllers.Shared;
using static Tests.Infrastructure.ApiAssert;

namespace Tests.Integrations.Socium;

[Collection(SociumApiCollection.Name)]
public sealed class MessageApiTests(SociumApiFixture fixture)
{
    private const string BaseUrl = "/api/message";
    private const string ChatUrl = "/api/chat";
    private const string RoomUrl = "/api/room";

    private HttpClient Client => fixture.Client;

    private static string UniqueText() => $"Message {Guid.NewGuid():N}";

    private async Task<Guid> CreateRoomAsync()
    {
        var name = $"Room {Guid.NewGuid():N}";
        var created = await Client.PostAsJsonAsync(RoomUrl, new RoomCreateRequest { Name = name }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var list = await ReadSuccessAsync<RoomListPageResponse>(await Client.GetAsync(RoomUrl, TestContext.Current.CancellationToken));
        return list.Response!.Rows.Single(row => row.Name == name).Id;
    }

    private async Task<Guid> CreateChatAsync(Guid roomId)
    {
        var name = $"Chat {Guid.NewGuid():N}";
        var created = await Client.PostAsJsonAsync(ChatUrl, new ChatCreateRequest { RoomId = roomId, Name = name }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var list = await ReadSuccessAsync<ChatListPageResponse>(await Client.GetAsync($"{ChatUrl}?roomId={roomId}", TestContext.Current.CancellationToken));
        return list.Response!.Rows.Single(row => row.Name == name).Id;
    }

    private async Task<Guid> CreateChatAsync() => await CreateChatAsync(await CreateRoomAsync());

    private async Task<Guid> CreateMessageAsync(Guid chatId, string text)
    {
        var created = await Client.PostAsJsonAsync(BaseUrl, new MessageCreateRequest { ChatId = chatId, Text = text }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var list = await GetListAsync(chatId);
        return list.Response!.Rows.Single(row => row.Text == text).Id;
    }

    private async Task<ApiSuccessResponse<MessageListPageResponse>> GetListAsync(Guid chatId)
        => await ReadSuccessAsync<MessageListPageResponse>(await Client.GetAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken));

    [Fact]
    public async Task List_GET_WithChatMessages_ShouldReturn200WithMessagesAndUtcTime()
    {
        var chatId = await CreateChatAsync();
        var text = UniqueText();
        var id = await CreateMessageAsync(chatId, text);

        var body = await GetListAsync(chatId);

        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        var row = Assert.Single(body.Response!.Rows);
        Assert.Equal(id, row.Id);
        Assert.Equal(text, row.Text);
        Assert.Equal(DateTimeKind.Utc, row.CreatedAt.Kind);
    }

    [Fact]
    public async Task List_GET_WithEmptyChatId_ShouldReturn400()
    {
        var response = await Client.GetAsync($"{BaseUrl}?chatId={Guid.Empty}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task List_GET_WithUnknownChatId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}?chatId={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Info_GET_WithExistingMessage_ShouldReturn200WithMessage()
    {
        var chatId = await CreateChatAsync();
        var text = UniqueText();
        var id = await CreateMessageAsync(chatId, text);

        var body = await ReadSuccessAsync<MessageInfoPageResponse>(await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(chatId, body.Response.ChatId);
        Assert.Equal(text, body.Response.Text);
    }

    [Fact]
    public async Task Info_GET_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/info?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(MessageCrudTexts.Messages.Validation.MessageNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Create_GET_WithExistingChat_ShouldReturn200WithEmptyForm()
    {
        var chatId = await CreateChatAsync();

        var body = await ReadSuccessAsync<MessageCreatePageResponse>(await Client.GetAsync($"{BaseUrl}/create?chatId={chatId}", TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        Assert.Equal(chatId, body.Response!.ChatId);
        Assert.Equal(string.Empty, body.Response.Text);
    }

    [Fact]
    public async Task Create_GET_WithUnknownChatId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/create?chatId={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithValidText_ShouldReturn200Saved()
    {
        var chatId = await CreateChatAsync();

        var response = await Client.PostAsJsonAsync(BaseUrl, new MessageCreateRequest { ChatId = chatId, Text = UniqueText() }, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<MessageCreatePageResponse>(response);
        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        Assert.Equal(MessageCrudTexts.Messages.Success.CreateCompleted, body.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_POST_WithMillionCharacters_ShouldReturn200AndKeepWholeText()
    {
        var chatId = await CreateChatAsync();
        var text = new string('я', 1_000_000);

        var response = await Client.PostAsJsonAsync(BaseUrl, new MessageCreateRequest { ChatId = chatId, Text = text }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = Assert.Single((await GetListAsync(chatId)).Response!.Rows);
        Assert.Equal(text.Length, row.Text.Length);
    }

    [Fact]
    public async Task Create_POST_WithEmptyChatId_ShouldReturn400()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new MessageCreateRequest { ChatId = Guid.Empty, Text = UniqueText() }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatIdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithUnknownChatId_ShouldReturn404()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new MessageCreateRequest { ChatId = Guid.CreateVersion7(), Text = UniqueText() }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(MessageCrudTexts.Messages.Validation.ChatNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithBlankText_ShouldReturn422()
    {
        var chatId = await CreateChatAsync();

        var response = await Client.PostAsJsonAsync(BaseUrl, new MessageCreateRequest { ChatId = chatId, Text = "  " }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(MessageCrudTexts.Messages.Validation.TextNotEmpty, problem.Detail);
    }

    [Fact]
    public async Task Update_GET_WithExistingMessage_ShouldReturn200WithForm()
    {
        var chatId = await CreateChatAsync();
        var text = UniqueText();
        var id = await CreateMessageAsync(chatId, text);

        var body = await ReadSuccessAsync<MessageUpdatePageResponse>(await Client.GetAsync($"{BaseUrl}/update?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(chatId, body.Response.ChatId);
        Assert.Equal(text, body.Response.Text);
    }

    [Fact]
    public async Task Update_PUT_WithNewText_ShouldReturn200AndChangeText()
    {
        var chatId = await CreateChatAsync();
        var id = await CreateMessageAsync(chatId, UniqueText());
        var newText = UniqueText();

        var response = await Client.PutAsJsonAsync(BaseUrl, new MessageUpdateRequest { Id = id, Text = newText }, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<MessageUpdatePageResponse>(response);
        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        var info = await ReadSuccessAsync<MessageInfoPageResponse>(await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken));
        Assert.Equal(newText, info.Response!.Text);
    }

    [Fact]
    public async Task Update_PUT_WithEmptyId_ShouldReturn400()
    {
        var response = await Client.PutAsJsonAsync(BaseUrl, new MessageUpdateRequest { Id = Guid.Empty, Text = UniqueText() }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(MessageCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Update_PUT_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.PutAsJsonAsync(BaseUrl, new MessageUpdateRequest { Id = Guid.CreateVersion7(), Text = UniqueText() }, TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_PUT_WithBlankText_ShouldReturn422()
    {
        var chatId = await CreateChatAsync();
        var id = await CreateMessageAsync(chatId, UniqueText());

        var response = await Client.PutAsJsonAsync(BaseUrl, new MessageUpdateRequest { Id = id, Text = " " }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(MessageCrudTexts.Messages.Validation.TextNotEmpty, problem.Detail);
    }

    [Fact]
    public async Task Delete_GET_WithExistingMessage_ShouldReturn200WithConfirmation()
    {
        var chatId = await CreateChatAsync();
        var text = UniqueText();
        var id = await CreateMessageAsync(chatId, text);

        var body = await ReadSuccessAsync<MessageDeletePageResponse>(await Client.GetAsync($"{BaseUrl}/delete?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(text, body.Response.Text);
    }

    [Fact]
    public async Task Delete_GET_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/delete?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(MessageCrudTexts.Messages.Validation.MessageNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Delete_DELETE_WithExistingMessage_ShouldReturn200AndRemoveMessage()
    {
        var chatId = await CreateChatAsync();
        var id = await CreateMessageAsync(chatId, UniqueText());

        var body = await ReadSuccessAsync<MessageDeleteResponse>(await Client.DeleteAsync($"{BaseUrl}?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        Assert.True(body.Response!.IsDeleted);
        var info = await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, info.StatusCode);
    }

    [Fact]
    public async Task Delete_DELETE_WithEmptyId_ShouldReturn400()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}?id={Guid.Empty}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(MessageCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Delete_DELETE_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Chat_DELETE_WithChatMessages_ShouldRemoveMessagesCascade()
    {
        var chatId = await CreateChatAsync();
        var id = await CreateMessageAsync(chatId, UniqueText());

        var deleted = await Client.DeleteAsync($"{ChatUrl}?id={chatId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        var info = await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, info.StatusCode);
    }

    [Fact]
    public async Task Room_DELETE_WithChatMessages_ShouldRemoveMessagesCascade()
    {
        var roomId = await CreateRoomAsync();
        var chatId = await CreateChatAsync(roomId);
        var id = await CreateMessageAsync(chatId, UniqueText());

        var deleted = await Client.DeleteAsync($"{RoomUrl}?id={roomId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        var info = await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, info.StatusCode);
    }

    [Fact]
    public async Task List_GET_WithNonParticipantOrWithoutUser_ShouldReturn403Or401()
    {
        var chatId = await CreateChatAsync();
        var userId = await fixture.CreateUserAsync(Client, $"user.{Guid.NewGuid():N}", "Пётр");
        using var userClient = fixture.CreateClientAs(userId);
        using var anonymousClient = fixture.CreateClientAs(null);

        var forbidden = await userClient.GetAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken);
        var unauthorized = await anonymousClient.GetAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(forbidden, HttpStatusCode.Forbidden);
        Assert.Contains(MessageCrudTexts.Messages.Validation.NotParticipant, problem.Detail);
        await ReadProblemAsync(unauthorized, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_PUT_WithOtherParticipant_ShouldReturn403()
    {
        var chatId = await CreateChatAsync();
        var id = await CreateMessageAsync(chatId, UniqueText());
        var userId = await fixture.CreateUserAsync(Client, $"user.{Guid.NewGuid():N}", "Пётр");
        using var userClient = fixture.CreateClientAs(userId);
        await userClient.PostAsJsonAsync("/api/participant", new ParticipantCreateRequest { ChatId = chatId }, TestContext.Current.CancellationToken);

        var response = await userClient.PutAsJsonAsync(BaseUrl, new MessageUpdateRequest { Id = id, Text = UniqueText() }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.Forbidden);
        Assert.Contains(MessageCrudTexts.Messages.Validation.NotSender, problem.Detail);
    }
}
