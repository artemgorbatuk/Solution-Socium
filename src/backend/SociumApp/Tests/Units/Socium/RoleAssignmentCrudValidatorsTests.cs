using Datasource.Socium.Ef.Models;
using Services.Security.Permissions.Catalogs;
using Services.Socium.Models;
using Services.Socium.Ports;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Tests.Units.Socium;

public sealed class RoleAssignmentCrudValidatorsTests
{
    private static UserPortModel CreateUser(bool isDeleted = false) => new()
    {
        Id = Guid.CreateVersion7(),
        Login = "ivan",
        Name = "Иван",
        IsDeleted = isDeleted,
    };

    [Fact]
    public void Requests_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        string[] expected = [RoleAssignmentCrudTexts.Messages.Validation.RequestCannotBeNull];

        Assert.Equal(expected, RoleAssignmentCrudValidators.ValidatePrimaryListPageRequest(null));
        Assert.Equal(expected, RoleAssignmentCrudValidators.ValidatePrimaryCreateRequest(null));
        Assert.Equal(expected, RoleAssignmentCrudValidators.ValidatePrimaryDeleteRequest(null));
        Assert.Equal(expected, RoleAssignmentCrudValidators.ValidatePrimaryLastAdminListRequest(null));
        Assert.Equal(expected, RoleAssignmentCrudValidators.ValidatePrimaryDeleteByUserRequest(null));
    }

    [Fact]
    public void CreateRequest_ValidatePrimary_WithAppRole_ShouldReturnUnknownRole()
    {
        var errors = RoleAssignmentCrudValidators.ValidatePrimaryCreateRequest(new RoleAssignmentCreateRequest
        {
            UserId = Guid.CreateVersion7(),
            RoleName = RoleNames.AppAdmin,
        });

        Assert.Equal([RoleAssignmentCrudTexts.Messages.Validation.UnknownRole], errors);
    }

    [Fact]
    public void CreateRequest_ValidatePrimary_WithRoomRoleAndChatId_ShouldReturnRoomIdRequired()
    {
        var errors = RoleAssignmentCrudValidators.ValidatePrimaryCreateRequest(new RoleAssignmentCreateRequest
        {
            UserId = Guid.CreateVersion7(),
            RoleName = RoleNames.RoomAdmin,
            RoomId = Guid.CreateVersion7(),
            ChatId = Guid.CreateVersion7(),
        });

        Assert.Equal([RoleAssignmentCrudTexts.Messages.Validation.RoomIdRequired], errors);
    }

    [Theory]
    [InlineData(RoleNames.ChatParticipant)]
    [InlineData(RoleNames.ChatAdmin)]
    public void DeleteRequest_ValidatePrimary_WithChatRoleWithoutChatId_ShouldReturnChatIdRequired(string roleName)
    {
        var errors = RoleAssignmentCrudValidators.ValidatePrimaryDeleteRequest(new RoleAssignmentDeleteRequest
        {
            UserId = Guid.CreateVersion7(),
            RoleName = roleName,
            RoomId = Guid.CreateVersion7(),
        });

        Assert.Equal([RoleAssignmentCrudTexts.Messages.Validation.ChatIdRequired], errors);
    }

    [Fact]
    public void CreateRequest_ValidatePrimary_WithValidChatRole_ShouldPass()
    {
        var errors = RoleAssignmentCrudValidators.ValidatePrimaryCreateRequest(new RoleAssignmentCreateRequest
        {
            UserId = Guid.CreateVersion7(),
            RoleName = RoleNames.ChatParticipant,
            ChatId = Guid.CreateVersion7(),
        });

        Assert.Empty(errors);
    }

    [Fact]
    public void Manage_ValidateAccess_WithSelfOrDeletedOrNoPermission_ShouldDeny()
    {
        var target = CreateUser();
        var deleted = CreateUser(isDeleted: true);

        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.AccessDenied],
            RoleAssignmentCrudValidators.ValidateAccessManage(allowed: false, target, Guid.CreateVersion7()));
        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.CannotManageSelf],
            RoleAssignmentCrudValidators.ValidateAccessManage(allowed: true, target, target.Id));
        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.UserDeleted],
            RoleAssignmentCrudValidators.ValidateAccessManage(allowed: true, deleted, Guid.CreateVersion7()));
        Assert.Empty(RoleAssignmentCrudValidators.ValidateAccessManage(allowed: true, target, Guid.CreateVersion7()));
    }

    [Fact]
    public void DeleteRoomAdmin_ValidateDomain_WithLastRoomAdmin_ShouldReturnLastRoomAdminCannotRevoke()
    {
        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.LastRoomAdminCannotRevoke],
            RoleAssignmentCrudValidators.ValidateDomainDeleteRoomAdmin(isRoomAdmin: true, roomAdminCount: 1));
        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.NotRoomAdmin],
            RoleAssignmentCrudValidators.ValidateDomainDeleteRoomAdmin(isRoomAdmin: false, roomAdminCount: 1));
        Assert.Empty(RoleAssignmentCrudValidators.ValidateDomainDeleteRoomAdmin(isRoomAdmin: true, roomAdminCount: 2));
    }

    [Fact]
    public void DeleteParticipant_ValidateDomain_WithLastChatAdmin_ShouldReturnLastAdminCannotRevoke()
    {
        var participant = new Participant { Id = Guid.CreateVersion7(), ChatId = Guid.CreateVersion7(), UserId = Guid.CreateVersion7() };

        Assert.Equal(
            [ParticipantCrudTexts.Messages.Validation.LastAdminCannotRevoke],
            RoleAssignmentCrudValidators.ValidateDomainDeleteParticipant(participant, isChatAdmin: true, chatAdminCount: 1));
        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.NotParticipant],
            RoleAssignmentCrudValidators.ValidateDomainDeleteParticipant(null, isChatAdmin: false, chatAdminCount: 1));
        Assert.Empty(RoleAssignmentCrudValidators.ValidateDomainDeleteParticipant(participant, isChatAdmin: false, chatAdminCount: 1));
    }

    [Fact]
    public void CreateChatAdmin_ValidateDomain_WithoutParticipant_ShouldReturnNotParticipant()
    {
        var participant = new Participant { Id = Guid.CreateVersion7(), ChatId = Guid.CreateVersion7(), UserId = Guid.CreateVersion7() };

        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.NotParticipant],
            RoleAssignmentCrudValidators.ValidateDomainCreateChatAdmin(null, isChatAdmin: false));
        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.AlreadyChatAdmin],
            RoleAssignmentCrudValidators.ValidateDomainCreateChatAdmin(participant, isChatAdmin: true));
        Assert.Empty(RoleAssignmentCrudValidators.ValidateDomainCreateChatAdmin(participant, isChatAdmin: false));
    }

    [Fact]
    public void DeleteChatAdmin_ValidateDomain_WithNotAdmin_ShouldReturnNotChatAdmin()
    {
        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.NotChatAdmin],
            RoleAssignmentCrudValidators.ValidateDomainDeleteChatAdmin(isChatAdmin: false, chatAdminCount: 2));
        Assert.Equal(
            [ParticipantCrudTexts.Messages.Validation.LastAdminCannotRevoke],
            RoleAssignmentCrudValidators.ValidateDomainDeleteChatAdmin(isChatAdmin: true, chatAdminCount: 1));
    }

    [Fact]
    public void DeleteByUser_ValidateDomain_WithActiveUser_ShouldReturnUserNotDeleted()
    {
        Assert.Equal(
            [RoleAssignmentCrudTexts.Messages.Validation.UserNotDeleted],
            RoleAssignmentCrudValidators.ValidateDomainDeleteByUser(CreateUser()));
        Assert.Empty(RoleAssignmentCrudValidators.ValidateDomainDeleteByUser(CreateUser(isDeleted: true)));
        Assert.Empty(RoleAssignmentCrudValidators.ValidateDomainDeleteByUser(null));
    }
}
