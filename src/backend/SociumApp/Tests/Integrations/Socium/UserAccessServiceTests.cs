using Repositories.Security.Ef.Api;
using Repositories.Security.Ef.Options;
using Services.Security.Api;
using Services.Security.Models;
using Services.Security.Permissions.Catalogs;
using Services.Security.Texts;
using Services.Shared.Enums;
using Services.Socium.Api;
using Services.Socium.Models;
using Services.Socium.Texts;

namespace Tests.Integrations.Socium;

/// <summary>Доступы пользователей для админа приложения (task-0029): персоны, «последний админ», удаление пользователя.</summary>
[Collection(SociumServiceCollection.Name)]
public sealed class UserAccessServiceTests(SociumServiceFixture fixture)
{
    private Task<Guid> CreateUserAsync(string prefix) => fixture.CreateUserAsync($"{prefix}.{Guid.NewGuid():N}", $"User {prefix}");

    private async Task<Guid> CreateAppAdminAsync()
    {
        var userId = await CreateUserAsync("admin");
        await fixture.GrantAppAdminAsync(userId);
        return userId;
    }

    private async Task<(Guid RoomId, Guid ChatId)> CreateRoomAndChatAsync()
    {
        var roomId = await fixture.CreateRoomAsync($"Room {Guid.NewGuid():N}");
        var chatId = await fixture.CreateChatAsync(roomId, $"Chat {Guid.NewGuid():N}");
        return (roomId, chatId);
    }

    private Task<Services.Shared.Models.ResponseInfo<RoleAssignmentListPageResponse>> LoadTreeAsync(Guid actorId, Guid userId)
        => fixture.RunAsAsync(actorId, (IServiceRoleAssignment service) =>
            service.DisplayListPageAsync(new RoleAssignmentListPageRequest { UserId = userId }));

    private Task<Services.Shared.Models.ResponseInfo<RoleAssignmentCreateResponse>> GrantAsync(
        Guid actorId,
        Guid userId,
        string roleName,
        Guid? roomId = null,
        Guid? chatId = null)
        => fixture.RunAsAsync(actorId, (IServiceRoleAssignment service) => service.CreateAsync(new RoleAssignmentCreateRequest
        {
            UserId = userId,
            RoleName = roleName,
            RoomId = roomId,
            ChatId = chatId,
        }));

    private Task<Services.Shared.Models.ResponseInfo<RoleAssignmentDeleteResponse>> RevokeAsync(
        Guid actorId,
        Guid userId,
        string roleName,
        Guid? roomId = null,
        Guid? chatId = null)
        => fixture.RunAsAsync(actorId, (IServiceRoleAssignment service) => service.DeleteAsync(new RoleAssignmentDeleteRequest
        {
            UserId = userId,
            RoleName = roleName,
            RoomId = roomId,
            ChatId = chatId,
        }));

    private static RoleAssignmentChatModel FindChat(RoleAssignmentListPageResponse tree, Guid chatId)
        => tree.Rooms.SelectMany(room => room.Chats).Single(chat => chat.ChatId == chatId);

    [Fact]
    public async Task AdminList_Load_WithAppAdmin_ShouldListUsersWithAppAdminFlags()
    {
        var userId = await CreateUserAsync("list");

        var result = await fixture.RunAsync((IServiceUser service) => service.DisplayAdminListPageAsync(new UserAdminListPageRequest()));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        var self = result.Response!.Rows.Single(row => row.Id == fixture.DefaultUserId);
        var other = result.Response.Rows.Single(row => row.Id == userId);
        Assert.True(self.IsAppAdmin);
        Assert.False(self.CanGrantAppAdmin);
        Assert.False(other.IsAppAdmin);
        Assert.True(other.CanGrantAppAdmin);
        Assert.True(other.IsEmailConfirmed);
    }

    [Fact]
    public async Task AdminList_Load_WithOrdinaryUser_ShouldReturnForbidden()
    {
        var userId = await CreateUserAsync("plain");

        var result = await fixture.RunAsAsync(userId, (IServiceUser service) => service.DisplayAdminListPageAsync(new UserAdminListPageRequest()));

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.AccessDenied, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task AppRole_CreateDelete_WithOtherUser_ShouldGrantThenRevokeUserRead()
    {
        var userId = await CreateUserAsync("grant");

        var created = await fixture.RunAsync((IServiceAppRole service) =>
            service.CreateAsync(new AppRoleCreateRequest { UserId = userId, RoleName = RoleNames.AppAdmin }));
        var allowed = await fixture.RunAsAsync(userId, (IServiceUser service) => service.DisplayAdminListPageAsync(new UserAdminListPageRequest()));
        var deleted = await fixture.RunAsync((IServiceAppRole service) =>
            service.DeleteAsync(new AppRoleDeleteRequest { UserId = userId, RoleName = RoleNames.AppAdmin }));
        var denied = await fixture.RunAsAsync(userId, (IServiceUser service) => service.DisplayAdminListPageAsync(new UserAdminListPageRequest()));

        Assert.Equal(MessageType.SAVED, created.MessageInfo.MessageType);
        Assert.Equal(MessageType.LOADED, allowed.MessageInfo.MessageType);
        Assert.Equal(MessageType.SAVED, deleted.MessageInfo.MessageType);
        Assert.Equal(MessageType.FORBIDDEN, denied.MessageInfo.MessageType);
    }

    [Fact]
    public async Task AppRole_Create_WithSelf_ShouldReturnForbidden()
    {
        var adminId = await CreateAppAdminAsync();

        var result = await fixture.RunAsAsync(adminId, (IServiceAppRole service) =>
            service.CreateAsync(new AppRoleCreateRequest { UserId = adminId, RoleName = RoleNames.AppAdmin }));

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
        Assert.Contains(AppRoleCrudTexts.Messages.Validation.CannotGrantSelf, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task AppRole_Create_WithOrdinaryUser_ShouldReturnForbidden()
    {
        var userId = await CreateUserAsync("plain");
        var targetId = await CreateUserAsync("target");

        var result = await fixture.RunAsAsync(userId, (IServiceAppRole service) =>
            service.CreateAsync(new AppRoleCreateRequest { UserId = targetId, RoleName = RoleNames.AppAdmin }));

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task AppRole_Delete_WithSelfAndAnotherAdmin_ShouldRevokeOwnRole()
    {
        var adminId = await CreateAppAdminAsync();

        var result = await fixture.RunAsAsync(adminId, (IServiceAppRole service) =>
            service.DeleteAsync(new AppRoleDeleteRequest { UserId = adminId, RoleName = RoleNames.AppAdmin }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task AppRole_DeleteAndUserDelete_WithLastAppAdmin_ShouldReturnInvalid()
    {
        var adminId = await CreateAppAdminAsync();
        var otherAdminIds = await fixture.RunAsync(async (IUnitOfWorkSecurity unitOfWork) =>
        {
            var admins = await unitOfWork.AppRoleAssignments.GetListAsync(new AppRoleAssignmentQueryOptions { RoleName = RoleNames.AppAdmin });
            var otherIds = admins.Select(assignment => assignment.UserId).Where(userId => userId != adminId).ToList();
            foreach (var userId in otherIds)
            {
                var assignment = await unitOfWork.AppRoleAssignments.GetSingleOrDefaultAsync(userId, RoleNames.AppAdmin);
                await unitOfWork.AppRoleAssignments.DeleteAsync(assignment!);
            }

            await unitOfWork.SaveChangesAsync();
            return otherIds;
        });

        try
        {
            var revoke = await fixture.RunAsAsync(adminId, (IServiceAppRole service) =>
                service.DeleteAsync(new AppRoleDeleteRequest { UserId = adminId, RoleName = RoleNames.AppAdmin }));
            var delete = await fixture.RunAsAsync(adminId, (IServiceUser service) =>
                service.DeleteAsync(new UserDeleteRequest { Id = adminId }));

            Assert.Equal(MessageType.INVALID, revoke.MessageInfo.MessageType);
            Assert.Contains(AppRoleCrudTexts.Messages.Validation.LastAppAdminCannotRevoke, revoke.MessageInfo.MessageText);
            Assert.Equal(MessageType.INVALID, delete.MessageInfo.MessageType);
            Assert.Contains(UserCrudTexts.Messages.Validation.LastAppAdminPlace, delete.MessageInfo.MessageText);
        }
        finally
        {
            foreach (var userId in otherAdminIds)
            {
                await fixture.GrantAppAdminAsync(userId);
            }
        }
    }

    [Fact]
    public async Task Tree_Load_WithParticipantAndSelf_ShouldShowRolesAndManageFlags()
    {
        var (roomId, chatId) = await CreateRoomAndChatAsync();
        var userId = await CreateUserAsync("tree");
        await fixture.JoinChatAsync(chatId, userId);

        var other = await LoadTreeAsync(fixture.DefaultUserId, userId);
        var self = await LoadTreeAsync(fixture.DefaultUserId, fixture.DefaultUserId);

        Assert.Equal(MessageType.LOADED, other.MessageInfo.MessageType);
        Assert.True(other.Response!.CanManage);
        Assert.False(other.Response.Rooms.Single(room => room.RoomId == roomId).IsRoomAdmin);
        var otherChat = FindChat(other.Response, chatId);
        Assert.True(otherChat.IsParticipant);
        Assert.False(otherChat.IsChatAdmin);

        Assert.False(self.Response!.CanManage);
        var selfRoom = self.Response.Rooms.Single(room => room.RoomId == roomId);
        Assert.True(selfRoom.IsRoomAdmin);
        Assert.True(selfRoom.IsLastRoomAdmin);
        Assert.True(FindChat(self.Response, chatId).IsLastChatAdmin);
    }

    [Fact]
    public async Task Tree_Load_WithOrdinaryUser_ShouldReturnForbidden()
    {
        var userId = await CreateUserAsync("plain");

        var result = await LoadTreeAsync(userId, fixture.DefaultUserId);

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task ChatRoles_CreateDelete_WithOtherUser_ShouldAddAdminThenKick()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();
        var userId = await CreateUserAsync("chat");

        var participant = await GrantAsync(fixture.DefaultUserId, userId, RoleNames.ChatParticipant, chatId: chatId);
        var admin = await GrantAsync(fixture.DefaultUserId, userId, RoleNames.ChatAdmin, chatId: chatId);
        var granted = await LoadTreeAsync(fixture.DefaultUserId, userId);
        var kicked = await RevokeAsync(fixture.DefaultUserId, userId, RoleNames.ChatParticipant, chatId: chatId);
        var revoked = await LoadTreeAsync(fixture.DefaultUserId, userId);

        Assert.Equal(MessageType.SAVED, participant.MessageInfo.MessageType);
        Assert.Equal(MessageType.SAVED, admin.MessageInfo.MessageType);
        Assert.True(FindChat(granted.Response!, chatId).IsChatAdmin);
        Assert.Equal(MessageType.SAVED, kicked.MessageInfo.MessageType);
        var chat = FindChat(revoked.Response!, chatId);
        Assert.False(chat.IsParticipant);
        Assert.False(chat.IsChatAdmin);
    }

    [Fact]
    public async Task ChatAdmin_Create_WithoutParticipant_ShouldReturnInvalid()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();
        var userId = await CreateUserAsync("chat");

        var result = await GrantAsync(fixture.DefaultUserId, userId, RoleNames.ChatAdmin, chatId: chatId);

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(RoleAssignmentCrudTexts.Messages.Validation.NotParticipant, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task ChatRoles_Delete_WithLastChatAdmin_ShouldReturnInvalid()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();
        var adminId = await CreateAppAdminAsync();
        var userId = await CreateUserAsync("chat");
        await GrantAsync(adminId, userId, RoleNames.ChatParticipant, chatId: chatId);
        await GrantAsync(adminId, userId, RoleNames.ChatAdmin, chatId: chatId);

        var revokeCreator = await RevokeAsync(adminId, fixture.DefaultUserId, RoleNames.ChatAdmin, chatId: chatId);
        var revokeLast = await RevokeAsync(adminId, userId, RoleNames.ChatAdmin, chatId: chatId);
        var kickLast = await RevokeAsync(adminId, userId, RoleNames.ChatParticipant, chatId: chatId);

        Assert.Equal(MessageType.SAVED, revokeCreator.MessageInfo.MessageType);
        Assert.Equal(MessageType.INVALID, revokeLast.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.LastAdminCannotRevoke, revokeLast.MessageInfo.MessageText);
        Assert.Equal(MessageType.INVALID, kickLast.MessageInfo.MessageType);
    }

    [Fact]
    public async Task RoomAdmin_CreateDelete_WithLastRoomAdmin_ShouldReturnInvalid()
    {
        var (roomId, _) = await CreateRoomAndChatAsync();
        var adminId = await CreateAppAdminAsync();
        var userId = await CreateUserAsync("room");

        var granted = await GrantAsync(adminId, userId, RoleNames.RoomAdmin, roomId: roomId);
        var duplicate = await GrantAsync(adminId, userId, RoleNames.RoomAdmin, roomId: roomId);
        var revokeCreator = await RevokeAsync(adminId, fixture.DefaultUserId, RoleNames.RoomAdmin, roomId: roomId);
        var revokeLast = await RevokeAsync(adminId, userId, RoleNames.RoomAdmin, roomId: roomId);

        Assert.Equal(MessageType.SAVED, granted.MessageInfo.MessageType);
        Assert.Equal(MessageType.INVALID, duplicate.MessageInfo.MessageType);
        Assert.Equal(MessageType.SAVED, revokeCreator.MessageInfo.MessageType);
        Assert.Equal(MessageType.INVALID, revokeLast.MessageInfo.MessageType);
        Assert.Contains(RoleAssignmentCrudTexts.Messages.Validation.LastRoomAdminCannotRevoke, revokeLast.MessageInfo.MessageText);
    }

    [Fact]
    public async Task RoleAssignment_Create_WithSelf_ShouldReturnForbidden()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();
        var adminId = await CreateAppAdminAsync();

        var result = await GrantAsync(adminId, adminId, RoleNames.ChatParticipant, chatId: chatId);

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
        Assert.Contains(RoleAssignmentCrudTexts.Messages.Validation.CannotManageSelf, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task RoleAssignment_Create_WithOrdinaryUser_ShouldReturnForbidden()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();
        var userId = await CreateUserAsync("plain");
        var targetId = await CreateUserAsync("target");

        var result = await GrantAsync(userId, targetId, RoleNames.ChatParticipant, chatId: chatId);

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
        Assert.Contains(RoleAssignmentCrudTexts.Messages.Validation.AccessDenied, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task UserDelete_Submit_WithLastChatAdmin_ShouldReturnInvalidWithPlace()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();
        var adminId = await CreateAppAdminAsync();
        var userId = await CreateUserAsync("last");
        await GrantAsync(adminId, userId, RoleNames.ChatParticipant, chatId: chatId);
        await GrantAsync(adminId, userId, RoleNames.ChatAdmin, chatId: chatId);
        await RevokeAsync(adminId, fixture.DefaultUserId, RoleNames.ChatAdmin, chatId: chatId);

        var result = await fixture.RunAsAsync(userId, (IServiceUser service) => service.DeleteAsync(new UserDeleteRequest { Id = userId }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LastAdminCannotDelete, result.MessageInfo.MessageText);
        Assert.Contains("чат «", result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task UserDelete_Submit_WithParticipant_ShouldRemoveRolesAndMarkSender()
    {
        var (_, chatId) = await CreateRoomAndChatAsync();
        var userId = await CreateUserAsync("gone");
        await fixture.JoinChatAsync(chatId, userId);
        var text = $"Сообщение {Guid.NewGuid():N}";
        await fixture.RunAsAsync(userId, (IServiceMessage service) =>
            service.CreateAsync(new MessageCreateRequest { ChatId = chatId, Text = text }));

        var deleted = await fixture.RunAsAsync(userId, (IServiceUser service) => service.DeleteAsync(new UserDeleteRequest { Id = userId }));
        var tree = await LoadTreeAsync(fixture.DefaultUserId, userId);
        var participants = await fixture.RunAsync((IServiceParticipant service) =>
            service.DisplayListPageAsync(new ParticipantListPageRequest { ChatId = chatId }));
        var messages = await fixture.RunAsync((IServiceMessage service) =>
            service.DisplayListPageAsync(new MessageListPageRequest { ChatId = chatId }));

        Assert.Equal(MessageType.SAVED, deleted.MessageInfo.MessageType);
        Assert.Equal(MessageType.LOADED, tree.MessageInfo.MessageType);
        Assert.False(tree.Response!.CanManage);
        Assert.False(FindChat(tree.Response, chatId).IsParticipant);
        Assert.DoesNotContain(participants.Response!.Rows, row => row.UserId == userId);
        var message = messages.Response!.Rows.Single(row => row.Text == text);
        Assert.EndsWith(UserPortTexts.DeletedMark, message.SenderName);
    }
}
