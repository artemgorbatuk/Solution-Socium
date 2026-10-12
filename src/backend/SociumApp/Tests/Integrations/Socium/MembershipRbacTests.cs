using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Services.Security.Permissions.Catalogs;
using Services.Shared.Enums;
using Services.Socium.Api;
using Services.Socium.Models;
using Services.Socium.Texts;

namespace Tests.Integrations.Socium;

/// <summary>Персоны членства по RBAC (task-0028): RoomAdmin / ChatAdmin / Participant.</summary>
[Collection(SociumServiceCollection.Name)]
public sealed class MembershipRbacTests(SociumServiceFixture fixture)
{
    private async Task<(Guid RoomId, Guid ChatId)> CreateRoomAndChatAsync()
    {
        var roomId = await fixture.CreateRoomAsync($"Room {Guid.NewGuid():N}");
        var chatId = await fixture.CreateChatAsync(roomId, $"Chat {Guid.NewGuid():N}");
        return (roomId, chatId);
    }

    private async Task GrantRoomAdminAsync(Guid roomId, Guid userId)
    {
        await fixture.RunAsync(async (DbContextSocium db) =>
        {
            db.RoomRoleAssignments.Add(new RoomRoleAssignment
            {
                Id = Guid.CreateVersion7(),
                RoomId = roomId,
                UserId = userId,
                RoleName = RoleNames.RoomAdmin,
            });
            return await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Info_Load_WithRoomAdminNotParticipant_ShouldAllowRequestNotManage()
    {
        var (roomId, chatId) = await CreateRoomAndChatAsync();
        var userId = await fixture.CreateUserAsync($"ra.{Guid.NewGuid():N}", "Room Admin");
        await GrantRoomAdminAsync(roomId, userId);

        var info = await fixture.RunAsAsync(
            userId,
            (IServiceChat service) => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = chatId }));

        Assert.Equal(MessageType.LOADED, info.MessageInfo.MessageType);
        Assert.False(info.Response!.IsParticipant);
        Assert.False(info.Response.IsAdmin);
        Assert.False(info.Response.CanManageParticipants);
        Assert.Equal(0, info.Response.PendingJoinRequestCount);
    }

    [Fact]
    public async Task List_Load_WithChatAdmin_ShouldExposeGrantKickAndChangeAdmin()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();

        var list = await fixture.RunAsAsync(
            fixture.DefaultUserId,
            (IServiceParticipant service) => service.DisplayListPageAsync(new ParticipantListPageRequest { ChatId = chatId }));

        Assert.Equal(MessageType.LOADED, list.MessageInfo.MessageType);
        Assert.True(list.Response!.CanChangeAdmin);
        Assert.True(list.Response.CanGrantParticipant);
        Assert.True(list.Response.CanKick);
    }

    [Fact]
    public async Task List_Load_WithOrdinaryParticipant_ShouldDenyManageFlags()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();
        var userId = await fixture.CreateUserAsync($"mem.{Guid.NewGuid():N}", "Member");
        await fixture.JoinChatAsync(chatId, userId);

        var list = await fixture.RunAsAsync(
            userId,
            (IServiceParticipant service) => service.DisplayListPageAsync(new ParticipantListPageRequest { ChatId = chatId }));

        Assert.Equal(MessageType.LOADED, list.MessageInfo.MessageType);
        Assert.False(list.Response!.CanChangeAdmin);
        Assert.False(list.Response.CanGrantParticipant);
        Assert.False(list.Response.CanKick);
    }

    [Fact]
    public async Task Info_Load_WithChatAdmin_ShouldManageParticipantsAndPendingCount()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();

        var info = await fixture.RunAsAsync(
            fixture.DefaultUserId,
            (IServiceChat service) => service.DisplayInfoPageAsync(new ChatInfoPageRequest { Id = chatId }));

        Assert.Equal(MessageType.LOADED, info.MessageInfo.MessageType);
        Assert.True(info.Response!.IsParticipant);
        Assert.True(info.Response.IsAdmin);
        Assert.True(info.Response.CanManageParticipants);
    }

    [Fact]
    public async Task Kick_Submit_WithSelfId_ShouldReturnCannotKickSelf()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();
        var list = await fixture.RunAsAsync(
            fixture.DefaultUserId,
            (IServiceParticipant service) => service.DisplayListPageAsync(new ParticipantListPageRequest { ChatId = chatId }));
        var selfId = Assert.Single(list.Response!.Rows).Id;

        var result = await fixture.RunAsAsync(
            fixture.DefaultUserId,
            (IServiceParticipant service) => service.DeleteAsync(new ParticipantDeleteRequest { Id = selfId }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.CannotKickSelf, result.MessageInfo.MessageText);
    }
}
