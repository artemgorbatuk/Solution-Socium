using System.Net;
using System.Net.Http.Json;
using Services.Shared.Enums;
using Services.Socium.Models;
using Services.Socium.Texts;
using static Tests.Infrastructure.ApiAssert;

namespace Tests.Integrations.Socium;

[Collection(SociumApiCollection.Name)]
public sealed class ChatApiTests(SociumApiFixture fixture)
{
    private const string BaseUrl = "/api/chat";
    private const string RoomUrl = "/api/room";

    private HttpClient Client => fixture.Client;

    private static string UniqueName() => $"Chat {Guid.NewGuid():N}";

    private async Task<Guid> CreateRoomAsync()
    {
        var name = $"Room {Guid.NewGuid():N}";
        var created = await Client.PostAsJsonAsync(RoomUrl, new RoomCreateRequest { Name = name }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var list = await ReadSuccessAsync<RoomListPageResponse>(await Client.GetAsync(RoomUrl, TestContext.Current.CancellationToken));
        return list.Response!.Rows.Single(row => row.Name == name).Id;
    }

    private async Task<Guid> CreateChatAsync(Guid roomId, string name)
    {
        var created = await Client.PostAsJsonAsync(BaseUrl, new ChatCreateRequest { RoomId = roomId, Name = name }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var list = await ReadSuccessAsync<ChatListPageResponse>(await Client.GetAsync($"{BaseUrl}?roomId={roomId}", TestContext.Current.CancellationToken));
        return list.Response!.Rows.Single(row => row.Name == name).Id;
    }

    [Fact]
    public async Task List_GET_WithRoomChats_ShouldReturn200WithChats()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        var id = await CreateChatAsync(roomId, name);

        var body = await ReadSuccessAsync<ChatListPageResponse>(await Client.GetAsync($"{BaseUrl}?roomId={roomId}", TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        var row = Assert.Single(body.Response!.Rows);
        Assert.Equal(id, row.Id);
        Assert.Equal(name, row.Name);
    }

    [Fact]
    public async Task List_GET_WithEmptyRoomId_ShouldReturn400()
    {
        var response = await Client.GetAsync($"{BaseUrl}?roomId={Guid.Empty}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomIdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task List_GET_WithUnknownRoomId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}?roomId={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Info_GET_WithExistingChat_ShouldReturn200WithChat()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        var id = await CreateChatAsync(roomId, name);

        var body = await ReadSuccessAsync<ChatInfoPageResponse>(await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(roomId, body.Response.RoomId);
        Assert.Equal(name, body.Response.Name);
    }

    [Fact]
    public async Task Info_GET_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/info?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(ChatCrudTexts.Messages.Validation.ChatNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Create_GET_WithExistingRoom_ShouldReturn200WithEmptyForm()
    {
        var roomId = await CreateRoomAsync();

        var body = await ReadSuccessAsync<ChatCreatePageResponse>(await Client.GetAsync($"{BaseUrl}/create?roomId={roomId}", TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        Assert.Equal(roomId, body.Response!.RoomId);
        Assert.Equal(string.Empty, body.Response.Name);
    }

    [Fact]
    public async Task Create_GET_WithUnknownRoomId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/create?roomId={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithValidName_ShouldReturn200Saved()
    {
        var roomId = await CreateRoomAsync();

        var response = await Client.PostAsJsonAsync(BaseUrl, new ChatCreateRequest { RoomId = roomId, Name = UniqueName() }, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<ChatCreatePageResponse>(response);
        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        Assert.Equal(ChatCrudTexts.Messages.Success.CreateCompleted, body.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_POST_WithEmptyRoomId_ShouldReturn400()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new ChatCreateRequest { RoomId = Guid.Empty, Name = UniqueName() }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomIdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithUnknownRoomId_ShouldReturn404()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new ChatCreateRequest { RoomId = Guid.CreateVersion7(), Name = UniqueName() }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(ChatCrudTexts.Messages.Validation.RoomNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithTooLongName_ShouldReturn422()
    {
        var roomId = await CreateRoomAsync();

        var response = await Client.PostAsJsonAsync(BaseUrl, new ChatCreateRequest { RoomId = roomId, Name = new string('a', 129) }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(ChatCrudTexts.Messages.Validation.NameMaximumLength(128), problem.Detail);
    }

    [Fact]
    public async Task Update_GET_WithExistingChat_ShouldReturn200WithForm()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        var id = await CreateChatAsync(roomId, name);

        var body = await ReadSuccessAsync<ChatUpdatePageResponse>(await Client.GetAsync($"{BaseUrl}/update?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(roomId, body.Response.RoomId);
        Assert.Equal(name, body.Response.Name);
    }

    [Fact]
    public async Task Update_PUT_WithNewName_ShouldReturn200AndRenameChat()
    {
        var roomId = await CreateRoomAsync();
        var id = await CreateChatAsync(roomId, UniqueName());
        var newName = UniqueName();

        var response = await Client.PutAsJsonAsync(BaseUrl, new ChatUpdateRequest { Id = id, Name = newName }, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<ChatUpdatePageResponse>(response);
        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        var info = await ReadSuccessAsync<ChatInfoPageResponse>(await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken));
        Assert.Equal(newName, info.Response!.Name);
    }

    [Fact]
    public async Task Update_PUT_WithEmptyId_ShouldReturn400()
    {
        var response = await Client.PutAsJsonAsync(BaseUrl, new ChatUpdateRequest { Id = Guid.Empty, Name = UniqueName() }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(ChatCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Update_PUT_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.PutAsJsonAsync(BaseUrl, new ChatUpdateRequest { Id = Guid.CreateVersion7(), Name = UniqueName() }, TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_PUT_WithEmptyName_ShouldReturn422()
    {
        var roomId = await CreateRoomAsync();
        var id = await CreateChatAsync(roomId, UniqueName());

        var response = await Client.PutAsJsonAsync(BaseUrl, new ChatUpdateRequest { Id = id, Name = " " }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(ChatCrudTexts.Messages.Validation.NameNotEmpty, problem.Detail);
    }

    [Fact]
    public async Task Delete_GET_WithExistingChat_ShouldReturn200WithConfirmation()
    {
        var roomId = await CreateRoomAsync();
        var name = UniqueName();
        var id = await CreateChatAsync(roomId, name);

        var body = await ReadSuccessAsync<ChatDeletePageResponse>(await Client.GetAsync($"{BaseUrl}/delete?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(name, body.Response.Name);
    }

    [Fact]
    public async Task Delete_GET_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/delete?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(ChatCrudTexts.Messages.Validation.ChatNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Delete_DELETE_WithExistingChat_ShouldReturn200AndRemoveChat()
    {
        var roomId = await CreateRoomAsync();
        var id = await CreateChatAsync(roomId, UniqueName());

        var body = await ReadSuccessAsync<ChatDeleteResponse>(await Client.DeleteAsync($"{BaseUrl}?id={id}", TestContext.Current.CancellationToken));

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
        Assert.Contains(ChatCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Delete_DELETE_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Room_DELETE_WithRoomChats_ShouldRemoveChatsCascade()
    {
        var roomId = await CreateRoomAsync();
        var id = await CreateChatAsync(roomId, UniqueName());

        var deleted = await Client.DeleteAsync($"{RoomUrl}?id={roomId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        var info = await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, info.StatusCode);
    }
}
