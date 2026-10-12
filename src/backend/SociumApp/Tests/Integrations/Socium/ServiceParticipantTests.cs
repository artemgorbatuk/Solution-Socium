using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Api;
using Services.Socium.Models;
using Services.Socium.Texts;

namespace Tests.Integrations.Socium;

[Collection(SociumServiceCollection.Name)]
public sealed class ServiceParticipantTests(SociumServiceFixture fixture)
{
    private Task<ResponseInfo<T>> RunAsAsync<T>(Guid? userId, Func<IServiceParticipant, Task<ResponseInfo<T>>> action) where T : class
        => fixture.RunAsAsync(userId, action);

    private Task<Guid> CreateUserAsync() => fixture.CreateUserAsync($"user.{Guid.NewGuid():N}", $"User {Guid.NewGuid():N}");

    /// <summary>Чат, созданный пользователем по умолчанию (он участник и админ).</summary>
    private async Task<Guid> CreateChatAsync()
    {
        var roomId = await fixture.CreateRoomAsync($"Room {Guid.NewGuid():N}");
        return await fixture.CreateChatAsync(roomId, $"Chat {Guid.NewGuid():N}");
    }

    private Task<ResponseInfo<ParticipantListPageResponse>> GetListAsync(Guid chatId, Guid? userId)
        => RunAsAsync(userId, service => service.DisplayListPageAsync(new ParticipantListPageRequest { ChatId = chatId }));

    private async Task<Guid> GetParticipantIdAsync(Guid chatId, Guid userId)
    {
        var list = await GetListAsync(chatId, fixture.DefaultUserId);
        return list.Response!.Rows.Single(row => row.UserId == userId).Id;
    }

    [Fact]
    public async Task List_Load_WithCreator_ShouldReturnCreatorAsAdmin()
    {
        var chatId = await CreateChatAsync();

        var result = await GetListAsync(chatId, fixture.DefaultUserId);

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.CanChangeAdmin);
        Assert.True(result.Response.CanGrantParticipant);
        Assert.True(result.Response.CanKick);
        var row = Assert.Single(result.Response.Rows);
        Assert.Equal(fixture.DefaultUserId, row.UserId);
        Assert.Equal("default-user", row.Login);
        Assert.True(row.IsAdmin);
    }

    [Fact]
    public async Task List_Load_WithNonParticipant_ShouldReturnForbidden()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();

        var result = await GetListAsync(chatId, userId);

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.AccessDenied, result.MessageInfo.MessageText);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task List_Load_WithoutCurrentUser_ShouldReturnUnauthorized()
    {
        var chatId = await CreateChatAsync();

        var result = await GetListAsync(chatId, null);

        Assert.Equal(MessageType.UNAUTHORIZED, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.CurrentUserNotFound, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task List_Load_WithUnknownChat_ShouldReturnNotFound()
    {
        var result = await GetListAsync(Guid.CreateVersion7(), fixture.DefaultUserId);

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.ChatNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithAdminAddingUser_ShouldJoinAsOrdinaryParticipant()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();

        var result = await RunAsAsync(
            fixture.DefaultUserId,
            service => service.CreateAsync(new ParticipantCreateRequest { ChatId = chatId, UserId = userId }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.Equal(ParticipantCrudTexts.Messages.Success.CreateCompleted, result.MessageInfo.MessageText);
        var list = await GetListAsync(chatId, userId);
        Assert.False(list.Response!.CanChangeAdmin);
        Assert.Equal(2, list.Response.RowCount);
        var row = list.Response.Rows.Single(row => row.UserId == userId);
        Assert.Equal(result.Response!.Id, row.Id);
        Assert.False(row.IsAdmin);
    }

    [Fact]
    public async Task Create_Submit_WithExistingParticipant_ShouldReturnInvalid()
    {
        var chatId = await CreateChatAsync();

        var result = await RunAsAsync(
            fixture.DefaultUserId,
            service => service.CreateAsync(new ParticipantCreateRequest { ChatId = chatId, UserId = fixture.DefaultUserId }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.AlreadyParticipant, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithChatWithoutAdmins_ShouldJoinAsAdmin()
    {
        var chatId = await CreateChatAsync();
        await fixture.RunAsync((DbContextSocium db) => db.ChatRoleAssignments.Where(assignment => assignment.ChatId == chatId).ExecuteDeleteAsync());
        await fixture.RunAsync((DbContextSocium db) => db.Participants.Where(participant => participant.ChatId == chatId).ExecuteDeleteAsync());
        var userId = await CreateUserAsync();

        // Пустой чат: CreateAsync требует ChatParticipantGrant; первый участник через Stage+Save в одном UoW.
        await fixture.RunAsAsync(fixture.DefaultUserId, async (Repositories.Socium.Ef.Api.IUnitOfWorkSocium unitOfWork) =>
        {
            var participant = unitOfWork.Participants.GetNew(new Repositories.Socium.Ef.Options.ParticipantGetNewOptions
            {
                ChatId = chatId,
                UserId = userId,
            });
            await unitOfWork.Participants.CreateAsync(participant);

            var participantRole = unitOfWork.ChatRoleAssignments.GetNew();
            participantRole.UserId = userId;
            participantRole.ChatId = chatId;
            participantRole.RoleName = Services.Security.Permissions.Catalogs.RoleNames.ChatParticipant;
            await unitOfWork.ChatRoleAssignments.CreateAsync(participantRole);

            var adminRole = unitOfWork.ChatRoleAssignments.GetNew();
            adminRole.UserId = userId;
            adminRole.ChatId = chatId;
            adminRole.RoleName = Services.Security.Permissions.Catalogs.RoleNames.ChatAdmin;
            await unitOfWork.ChatRoleAssignments.CreateAsync(adminRole);

            await unitOfWork.SaveChangesAsync();
            return 0;
        });

        var list = await GetListAsync(chatId, userId);
        Assert.True(list.Response!.CanChangeAdmin);
        Assert.True(list.Response.Rows.Single(row => row.UserId == userId).IsAdmin);
    }

    [Fact]
    public async Task Create_Submit_WithoutCurrentUserOrUnknownChat_ShouldReturnUnauthorizedOrNotFound()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();

        var unauthorized = await RunAsAsync(
            null,
            service => service.CreateAsync(new ParticipantCreateRequest { ChatId = chatId, UserId = userId }));
        var notFound = await RunAsAsync(
            fixture.DefaultUserId,
            service => service.CreateAsync(new ParticipantCreateRequest { ChatId = Guid.CreateVersion7(), UserId = userId }));
        var badRequest = await RunAsAsync(
            fixture.DefaultUserId,
            service => service.CreateAsync(new ParticipantCreateRequest { ChatId = Guid.Empty, UserId = userId }));

        Assert.Equal(MessageType.UNAUTHORIZED, unauthorized.MessageInfo.MessageType);
        Assert.Equal(MessageType.NOT_FOUND, notFound.MessageInfo.MessageType);
        Assert.Equal(MessageType.BAD_REQUEST, badRequest.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Create_Submit_WithDeletedUser_ShouldReturnNotFound()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.RunAsAsync(userId, (Services.Security.Api.IServiceUser service) => service.DeleteAsync(new Services.Security.Models.UserDeleteRequest { Id = userId }));

        var result = await RunAsAsync(
            fixture.DefaultUserId,
            service => service.CreateAsync(new ParticipantCreateRequest { ChatId = chatId, UserId = userId }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.UserNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithAdminGrantingRole_ShouldMakeParticipantAdmin()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);
        var participantId = await GetParticipantIdAsync(chatId, userId);

        var result = await RunAsAsync(fixture.DefaultUserId, service => service.UpdateAsync(new ParticipantUpdateRequest { Id = participantId, IsAdmin = true }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.IsAdmin);
        Assert.True((await GetListAsync(chatId, userId)).Response!.CanChangeAdmin);
    }

    [Fact]
    public async Task Update_Submit_WithOrdinaryParticipant_ShouldReturnForbiddenAndKeepRole()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);
        var participantId = await GetParticipantIdAsync(chatId, userId);

        var result = await RunAsAsync(userId, service => service.UpdateAsync(new ParticipantUpdateRequest { Id = participantId, IsAdmin = true }));

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.AccessDenied, result.MessageInfo.MessageText);
        Assert.False((await GetListAsync(chatId, userId)).Response!.CanChangeAdmin);
    }

    [Fact]
    public async Task Update_Submit_WithRevokeFromLastAdmin_ShouldReturnInvalidAndKeepRole()
    {
        var chatId = await CreateChatAsync();
        var adminParticipantId = await GetParticipantIdAsync(chatId, fixture.DefaultUserId);

        var result = await RunAsAsync(fixture.DefaultUserId, service => service.UpdateAsync(new ParticipantUpdateRequest { Id = adminParticipantId, IsAdmin = false }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.LastAdminCannotRevoke, result.MessageInfo.MessageText);
        Assert.True((await GetListAsync(chatId, fixture.DefaultUserId)).Response!.CanChangeAdmin);
    }

    [Fact]
    public async Task Update_Submit_WithRevokeFromSelfWhenTwoAdmins_ShouldRevokeRole()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);
        var userParticipantId = await GetParticipantIdAsync(chatId, userId);
        var adminParticipantId = await GetParticipantIdAsync(chatId, fixture.DefaultUserId);
        await RunAsAsync(fixture.DefaultUserId, service => service.UpdateAsync(new ParticipantUpdateRequest { Id = userParticipantId, IsAdmin = true }));

        var result = await RunAsAsync(fixture.DefaultUserId, service => service.UpdateAsync(new ParticipantUpdateRequest { Id = adminParticipantId, IsAdmin = false }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var creatorList = await GetListAsync(chatId, fixture.DefaultUserId);
        var otherList = await GetListAsync(chatId, userId);
        // Создатель комнаты остаётся RoomAdmin > ChatAdminGrant/Revoke; бейдж ChatAdmin снят.
        Assert.True(creatorList.Response!.CanChangeAdmin);
        Assert.False(creatorList.Response.Rows.Single(row => row.UserId == fixture.DefaultUserId).IsAdmin);
        Assert.True(otherList.Response!.CanChangeAdmin);
        Assert.True(otherList.Response.Rows.Single(row => row.UserId == userId).IsAdmin);
    }

    [Fact]
    public async Task Update_Submit_WithUnknownOrEmptyId_ShouldReturnNotFoundOrBadRequest()
    {
        var notFound = await RunAsAsync(fixture.DefaultUserId, service => service.UpdateAsync(new ParticipantUpdateRequest { Id = Guid.CreateVersion7(), IsAdmin = true }));
        var badRequest = await RunAsAsync(fixture.DefaultUserId, service => service.UpdateAsync(new ParticipantUpdateRequest { Id = Guid.Empty, IsAdmin = true }));

        Assert.Equal(MessageType.NOT_FOUND, notFound.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.ParticipantNotFoundById, notFound.MessageInfo.MessageText);
        Assert.Equal(MessageType.BAD_REQUEST, badRequest.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Load_WithSoleParticipant_ShouldForbidLeaveWithReason()
    {
        var chatId = await CreateChatAsync();

        var result = await RunAsAsync(fixture.DefaultUserId, service => service.DisplayDeletePageAsync(new ParticipantDeletePageRequest { ChatId = chatId }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.False(result.Response!.CanLeave);
        Assert.Equal(ParticipantCrudTexts.Messages.Validation.SoleParticipantCannotLeave, result.Response.Reason);
    }

    [Fact]
    public async Task Delete_Load_WithLastAdminAndOrdinaryParticipant_ShouldForbidAdminAndAllowParticipant()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);

        var admin = await RunAsAsync(fixture.DefaultUserId, service => service.DisplayDeletePageAsync(new ParticipantDeletePageRequest { ChatId = chatId }));
        var member = await RunAsAsync(userId, service => service.DisplayDeletePageAsync(new ParticipantDeletePageRequest { ChatId = chatId }));

        Assert.False(admin.Response!.CanLeave);
        Assert.Equal(ParticipantCrudTexts.Messages.Validation.LastAdminCannotLeave, admin.Response.Reason);
        Assert.True(member.Response!.CanLeave);
        Assert.Null(member.Response.Reason);
    }

    [Fact]
    public async Task Delete_Load_WithNonParticipant_ShouldReturnForbidden()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();

        var result = await RunAsAsync(userId, service => service.DisplayDeletePageAsync(new ParticipantDeletePageRequest { ChatId = chatId }));

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Submit_WithOrdinaryParticipant_ShouldLeaveAndKeepMessages()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);
        var text = $"Message {Guid.NewGuid():N}";
        await fixture.RunAsAsync(userId, (IServiceMessage service) => service.CreateAsync(new MessageCreateRequest { ChatId = chatId, Text = text }));

        var result = await RunAsAsync(userId, service => service.DeleteAsync(new ParticipantDeleteRequest { ChatId = chatId }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.IsDeleted);
        Assert.Equal(MessageType.FORBIDDEN, (await GetListAsync(chatId, userId)).MessageInfo.MessageType);
        var messages = await fixture.RunAsync((IServiceMessage service) => service.DisplayListPageAsync(new MessageListPageRequest { ChatId = chatId }));
        var row = Assert.Single(messages.Response!.Rows);
        Assert.Equal(text, row.Text);
        Assert.Equal(userId, row.SenderUserId);
    }

    [Fact]
    public async Task Delete_Submit_WithLastAdmin_ShouldReturnInvalidAndStayInChat()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);

        var result = await RunAsAsync(fixture.DefaultUserId, service => service.DeleteAsync(new ParticipantDeleteRequest { ChatId = chatId }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.LastAdminCannotLeave, result.MessageInfo.MessageText);
        Assert.Equal(2, (await GetListAsync(chatId, fixture.DefaultUserId)).Response!.RowCount);
    }

    [Fact]
    public async Task Delete_Submit_WithSoleParticipant_ShouldReturnInvalid()
    {
        var chatId = await CreateChatAsync();

        var result = await RunAsAsync(fixture.DefaultUserId, service => service.DeleteAsync(new ParticipantDeleteRequest { ChatId = chatId }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.SoleParticipantCannotLeave, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Submit_WithAdminAfterGrantingRole_ShouldLeave()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);
        var userParticipantId = await GetParticipantIdAsync(chatId, userId);
        await RunAsAsync(fixture.DefaultUserId, service => service.UpdateAsync(new ParticipantUpdateRequest { Id = userParticipantId, IsAdmin = true }));

        var result = await RunAsAsync(fixture.DefaultUserId, service => service.DeleteAsync(new ParticipantDeleteRequest { ChatId = chatId }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var row = Assert.Single((await GetListAsync(chatId, userId)).Response!.Rows);
        Assert.Equal(userId, row.UserId);
    }

    [Fact]
    public async Task Delete_Submit_WithOtherParticipantId_ShouldKick()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);
        var memberId = await GetParticipantIdAsync(chatId, userId);

        var result = await RunAsAsync(
            fixture.DefaultUserId,
            service => service.DeleteAsync(new ParticipantDeleteRequest { Id = memberId }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.IsDeleted);
        Assert.Equal(MessageType.FORBIDDEN, (await GetListAsync(chatId, userId)).MessageInfo.MessageType);
        var row = Assert.Single((await GetListAsync(chatId, fixture.DefaultUserId)).Response!.Rows);
        Assert.Equal(fixture.DefaultUserId, row.UserId);
    }

    [Fact]
    public async Task Delete_Submit_WithOwnParticipantId_ShouldReturnInvalid()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);
        var adminId = await GetParticipantIdAsync(chatId, fixture.DefaultUserId);

        var result = await RunAsAsync(
            fixture.DefaultUserId,
            service => service.DeleteAsync(new ParticipantDeleteRequest { Id = adminId }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(ParticipantCrudTexts.Messages.Validation.CannotKickSelf, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Submit_WithMemberKickingAdmin_ShouldReturnForbidden()
    {
        var chatId = await CreateChatAsync();
        var userId = await CreateUserAsync();
        await fixture.JoinChatAsync(chatId, userId);
        var adminId = await GetParticipantIdAsync(chatId, fixture.DefaultUserId);

        var result = await RunAsAsync(
            userId,
            service => service.DeleteAsync(new ParticipantDeleteRequest { Id = adminId }));

        Assert.Equal(MessageType.FORBIDDEN, result.MessageInfo.MessageType);
    }
}
