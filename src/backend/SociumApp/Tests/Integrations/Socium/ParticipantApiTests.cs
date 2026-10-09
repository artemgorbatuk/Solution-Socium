using System.Net;
using System.Net.Http.Json;
using Services.Shared.Enums;
using Services.Socium.Models;
using Services.Socium.Texts;
using static Tests.Infrastructure.ApiAssert;

namespace Tests.Integrations.Socium;

[Collection(SociumApiCollection.Name)]
public sealed class ParticipantApiTests(SociumApiFixture fixture)
{
    private const string BaseUrl = "/api/participant";

    private HttpClient Client => fixture.Client;

    /// <summary>Чат, созданный пользователем по умолчанию (он участник и админ).</summary>
    private async Task<Guid> CreateChatAsync()
    {
        var roomName = $"Room {Guid.NewGuid():N}";
        var roomCreated = await Client.PostAsJsonAsync("/api/room", new RoomCreateRequest { Name = roomName }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, roomCreated.StatusCode);
        var rooms = await ReadSuccessAsync<RoomListPageResponse>(await Client.GetAsync("/api/room", TestContext.Current.CancellationToken));
        var roomId = rooms.Response!.Rows.Single(row => row.Name == roomName).Id;

        var chatName = $"Chat {Guid.NewGuid():N}";
        var chatCreated = await Client.PostAsJsonAsync("/api/chat", new ChatCreateRequest { RoomId = roomId, Name = chatName }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, chatCreated.StatusCode);
        var chats = await ReadSuccessAsync<ChatListPageResponse>(await Client.GetAsync($"/api/chat?roomId={roomId}", TestContext.Current.CancellationToken));
        return chats.Response!.Rows.Single(row => row.Name == chatName).Id;
    }

    private async Task<HttpClient> CreateUserClientAsync()
    {
        var userId = await fixture.CreateUserAsync(Client, $"user.{Guid.NewGuid():N}", "Пётр");
        return fixture.CreateClientAs(userId);
    }

    private static Task<HttpResponseMessage> JoinAsync(HttpClient client, Guid chatId)
        => client.PostAsJsonAsync(BaseUrl, new ParticipantCreateRequest { ChatId = chatId }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task List_GET_WithParticipant_ShouldReturn200WithParticipants()
    {
        var chatId = await CreateChatAsync();

        var body = await ReadSuccessAsync<ParticipantListPageResponse>(await Client.GetAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        Assert.True(body.Response!.IsAdmin);
        var row = Assert.Single(body.Response.Rows);
        Assert.Equal(fixture.DefaultUserId, row.UserId);
        Assert.True(row.IsAdmin);
    }

    [Fact]
    public async Task List_GET_WithNonParticipant_ShouldReturn403()
    {
        var chatId = await CreateChatAsync();
        using var client = await CreateUserClientAsync();

        var response = await client.GetAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.Forbidden);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.NotParticipant, problem.Detail);
    }

    [Fact]
    public async Task List_GET_WithoutUserHeader_ShouldReturn401()
    {
        var chatId = await CreateChatAsync();
        using var client = fixture.CreateClientAs(null);

        var response = await client.GetAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.Unauthorized);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.CurrentUserNotFound, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithNewUser_ShouldReturn200AndJoin()
    {
        var chatId = await CreateChatAsync();
        using var client = await CreateUserClientAsync();

        var body = await ReadSuccessAsync<ParticipantCreateResponse>(await JoinAsync(client, chatId));

        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        var list = await ReadSuccessAsync<ParticipantListPageResponse>(await client.GetAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken));
        Assert.Equal(2, list.Response!.RowCount);
        Assert.False(list.Response.IsAdmin);
    }

    [Fact]
    public async Task Create_POST_WithExistingParticipantOrUnknownChat_ShouldReturn422Or404()
    {
        var chatId = await CreateChatAsync();

        var repeated = await JoinAsync(Client, chatId);
        var unknown = await JoinAsync(Client, Guid.CreateVersion7());

        var problem = await ReadProblemAsync(repeated, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.AlreadyParticipant, problem.Detail);
        await ReadProblemAsync(unknown, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_PUT_WithAdminAndOrdinaryParticipant_ShouldReturn200And403()
    {
        var chatId = await CreateChatAsync();
        using var client = await CreateUserClientAsync();
        await JoinAsync(client, chatId);
        var list = await ReadSuccessAsync<ParticipantListPageResponse>(await Client.GetAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken));
        var memberId = list.Response!.Rows.Single(row => row.UserId != fixture.DefaultUserId).Id;
        var adminId = list.Response.Rows.Single(row => row.UserId == fixture.DefaultUserId).Id;

        var forbidden = await client.PutAsJsonAsync(BaseUrl, new ParticipantUpdateRequest { Id = adminId, IsAdmin = false }, TestContext.Current.CancellationToken);
        var granted = await Client.PutAsJsonAsync(BaseUrl, new ParticipantUpdateRequest { Id = memberId, IsAdmin = true }, TestContext.Current.CancellationToken);

        await ReadProblemAsync(forbidden, HttpStatusCode.Forbidden);
        var body = await ReadSuccessAsync<ParticipantUpdateResponse>(granted);
        Assert.True(body.Response!.IsAdmin);
    }

    [Fact]
    public async Task Update_PUT_WithRevokeFromLastAdmin_ShouldReturn422()
    {
        var chatId = await CreateChatAsync();
        var list = await ReadSuccessAsync<ParticipantListPageResponse>(await Client.GetAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken));

        var response = await Client.PutAsJsonAsync(BaseUrl, new ParticipantUpdateRequest { Id = Assert.Single(list.Response!.Rows).Id, IsAdmin = false }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.LastAdminCannotRevoke, problem.Detail);
    }

    [Fact]
    public async Task DeleteConfirm_GET_WithSoleParticipant_ShouldReturn200WithReason()
    {
        var chatId = await CreateChatAsync();

        var body = await ReadSuccessAsync<ParticipantDeletePageResponse>(await Client.GetAsync($"{BaseUrl}/delete?chatId={chatId}", TestContext.Current.CancellationToken));

        Assert.Equal(chatId, body.Response!.ChatId);
        Assert.False(body.Response.CanLeave);
        Assert.Equal(ParticipantCrudTexts.Messages.Validation.SoleParticipantCannotLeave, body.Response.Reason);
    }

    [Fact]
    public async Task Delete_DELETE_WithOrdinaryParticipantAndLastAdmin_ShouldReturn200And422()
    {
        var chatId = await CreateChatAsync();
        using var client = await CreateUserClientAsync();
        await JoinAsync(client, chatId);

        var adminLeave = await Client.DeleteAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken);
        var memberLeave = await client.DeleteAsync($"{BaseUrl}?chatId={chatId}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(adminLeave, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.LastAdminCannotLeave, problem.Detail);
        var body = await ReadSuccessAsync<ParticipantDeleteResponse>(memberLeave);
        Assert.True(body.Response!.IsDeleted);
    }
}
