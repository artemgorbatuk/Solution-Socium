using System.Net;
using System.Net.Http.Json;
using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.Extensions.DependencyInjection;
using Services.Security.Permissions.Catalogs;
using Services.Shared.Enums;
using Services.Socium.Models;
using Services.Socium.Texts;
using static Tests.Infrastructure.ApiAssert;

namespace Tests.Integrations.Socium;

[Collection(SociumApiCollection.Name)]
public sealed class JoinRequestApiTests(SociumApiFixture fixture)
{
    private const string BaseUrl = "/api/joinRequest";
    private const string ParticipantUrl = "/api/participant";

    private HttpClient Client => fixture.Client;

    private async Task<(Guid RoomId, Guid ChatId)> CreateChatAsync()
    {
        var roomName = $"Room {Guid.NewGuid():N}";
        Assert.Equal(
            HttpStatusCode.OK,
            (await Client.PostAsJsonAsync("/api/room", new RoomCreateRequest { Name = roomName }, TestContext.Current.CancellationToken)).StatusCode);
        var rooms = await ReadSuccessAsync<RoomListPageResponse>(await Client.GetAsync("/api/room", TestContext.Current.CancellationToken));
        var roomId = rooms.Response!.Rows.Single(row => row.Name == roomName).Id;

        var chatName = $"Chat {Guid.NewGuid():N}";
        Assert.Equal(
            HttpStatusCode.OK,
            (await Client.PostAsJsonAsync("/api/chat", new ChatCreateRequest { RoomId = roomId, Name = chatName }, TestContext.Current.CancellationToken)).StatusCode);
        var chats = await ReadSuccessAsync<ChatListPageResponse>(await Client.GetAsync($"/api/chat?roomId={roomId}", TestContext.Current.CancellationToken));
        return (roomId, chats.Response!.Rows.Single(row => row.Name == chatName).Id);
    }

    private async Task GrantRoomAdminAsync(Guid roomId, Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContextSocium>();
        db.RoomRoleAssignments.Add(new RoomRoleAssignment
        {
            Id = Guid.CreateVersion7(),
            RoomId = roomId,
            UserId = userId,
            RoleName = RoleNames.RoomAdmin,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAccept_POST_WithRoomAdmin_ShouldAddParticipant()
    {
        var (roomId, chatId) = await CreateChatAsync();
        var userId = await fixture.CreateUserAsync($"jr.{Guid.NewGuid():N}", "Заявитель");
        await GrantRoomAdminAsync(roomId, userId);
        using var requester = fixture.CreateClientAs(userId);

        var created = await ReadSuccessAsync<JoinRequestCreateResponse>(
            await requester.PostAsJsonAsync(BaseUrl, new JoinRequestCreateRequest { ChatId = chatId }, TestContext.Current.CancellationToken));
        Assert.Equal(MessageType.SAVED, created.MessageInfo.MessageType);

        var pending = await ReadSuccessAsync<JoinRequestPendingListPageResponse>(
            await Client.GetAsync($"{BaseUrl}/pending?chatId={chatId}", TestContext.Current.CancellationToken));
        Assert.Equal(1, pending.Response!.RowCount);
        Assert.Equal(userId, Assert.Single(pending.Response.Rows).UserId);

        var accepted = await ReadSuccessAsync<JoinRequestAcceptResponse>(
            await Client.PostAsJsonAsync(
                $"{BaseUrl}/accept",
                new JoinRequestAcceptRequest { Id = created.Response!.Id },
                TestContext.Current.CancellationToken));
        Assert.Equal(MessageType.SAVED, accepted.MessageInfo.MessageType);

        var list = await ReadSuccessAsync<ParticipantListPageResponse>(
            await requester.GetAsync($"{ParticipantUrl}?chatId={chatId}", TestContext.Current.CancellationToken));
        Assert.Contains(list.Response!.Rows, row => row.UserId == userId);
    }

    [Fact]
    public async Task Reject_POST_WithChatAdmin_ShouldKeepUserOut()
    {
        var (roomId, chatId) = await CreateChatAsync();
        var userId = await fixture.CreateUserAsync($"jr.{Guid.NewGuid():N}", "Заявитель");
        await GrantRoomAdminAsync(roomId, userId);
        using var requester = fixture.CreateClientAs(userId);

        var created = await ReadSuccessAsync<JoinRequestCreateResponse>(
            await requester.PostAsJsonAsync(BaseUrl, new JoinRequestCreateRequest { ChatId = chatId }, TestContext.Current.CancellationToken));

        var rejected = await ReadSuccessAsync<JoinRequestRejectResponse>(
            await Client.PostAsJsonAsync(
                $"{BaseUrl}/reject",
                new JoinRequestRejectRequest { Id = created.Response!.Id },
                TestContext.Current.CancellationToken));
        Assert.Equal(MessageType.SAVED, rejected.MessageInfo.MessageType);

        var pending = await ReadSuccessAsync<JoinRequestPendingListPageResponse>(
            await Client.GetAsync($"{BaseUrl}/pending?chatId={chatId}", TestContext.Current.CancellationToken));
        Assert.Equal(0, pending.Response!.RowCount);

        var list = await ReadSuccessAsync<ParticipantListPageResponse>(
            await Client.GetAsync($"{ParticipantUrl}?chatId={chatId}", TestContext.Current.CancellationToken));
        Assert.DoesNotContain(list.Response!.Rows, row => row.UserId == userId);
    }

    [Fact]
    public async Task Create_POST_WithDuplicatePending_ShouldReturn422()
    {
        var (roomId, chatId) = await CreateChatAsync();
        var userId = await fixture.CreateUserAsync($"jr.{Guid.NewGuid():N}", "Заявитель");
        await GrantRoomAdminAsync(roomId, userId);
        using var requester = fixture.CreateClientAs(userId);

        Assert.Equal(
            HttpStatusCode.OK,
            (await requester.PostAsJsonAsync(BaseUrl, new JoinRequestCreateRequest { ChatId = chatId }, TestContext.Current.CancellationToken)).StatusCode);

        var repeated = await requester.PostAsJsonAsync(
            BaseUrl,
            new JoinRequestCreateRequest { ChatId = chatId },
            TestContext.Current.CancellationToken);
        var problem = await ReadProblemAsync(repeated, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(JoinRequestCrudTexts.Messages.Validation.PendingAlreadyExists, problem.Detail);
    }
}
