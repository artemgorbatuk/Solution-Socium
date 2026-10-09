using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Tests.Units.Socium;

public sealed class ParticipantCrudValidatorsTests
{
    private static readonly Guid ChatId = Guid.CreateVersion7();

    private static Participant CreateParticipant(bool isAdmin) => new()
    {
        Id = Guid.CreateVersion7(),
        ChatId = ChatId,
        UserId = Guid.CreateVersion7(),
        IsAdmin = isAdmin,
    };

    [Fact]
    public void AllRequests_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        string[] expected = [ParticipantCrudTexts.Messages.Validation.RequestCannotBeNull];

        Assert.Equal(expected, ParticipantCrudValidators.ValidatePrimaryCreateRequest(null));
        Assert.Equal(expected, ParticipantCrudValidators.ValidatePrimaryUpdateRequest(null));
        Assert.Equal(expected, ParticipantCrudValidators.ValidatePrimaryDeletePageRequest(null));
        Assert.Equal(expected, ParticipantCrudValidators.ValidatePrimaryDeleteRequest(null));
        Assert.Equal(expected, ParticipantCrudValidators.ValidatePrimaryListPageRequest(null));
    }

    [Fact]
    public void RequestsWithChatId_ValidatePrimary_WithEmptyChatId_ShouldReturnChatIdCannotBeEmpty()
    {
        string[] expected = [ParticipantCrudTexts.Messages.Validation.ChatIdCannotBeEmpty];

        Assert.Equal(expected, ParticipantCrudValidators.ValidatePrimaryCreateRequest(new ParticipantCreateRequest { ChatId = Guid.Empty }));
        Assert.Equal(expected, ParticipantCrudValidators.ValidatePrimaryDeletePageRequest(new ParticipantDeletePageRequest { ChatId = Guid.Empty }));
        Assert.Equal(expected, ParticipantCrudValidators.ValidatePrimaryDeleteRequest(new ParticipantDeleteRequest { ChatId = Guid.Empty }));
        Assert.Equal(expected, ParticipantCrudValidators.ValidatePrimaryListPageRequest(new ParticipantListPageRequest { ChatId = Guid.Empty }));
    }

    [Fact]
    public void UpdateRequest_ValidatePrimary_WithEmptyId_ShouldReturnIdCannotBeEmpty()
    {
        var errors = ParticipantCrudValidators.ValidatePrimaryUpdateRequest(new ParticipantUpdateRequest { Id = Guid.Empty, IsAdmin = true });

        Assert.Equal([ParticipantCrudTexts.Messages.Validation.IdCannotBeEmpty], errors);
    }

    [Fact]
    public void FilledRequests_ValidatePrimary_WithIds_ShouldReturnNoErrors()
    {
        Assert.Empty(ParticipantCrudValidators.ValidatePrimaryCreateRequest(new ParticipantCreateRequest { ChatId = ChatId }));
        Assert.Empty(ParticipantCrudValidators.ValidatePrimaryUpdateRequest(new ParticipantUpdateRequest { Id = Guid.CreateVersion7(), IsAdmin = false }));
        Assert.Empty(ParticipantCrudValidators.ValidatePrimaryDeletePageRequest(new ParticipantDeletePageRequest { ChatId = ChatId }));
        Assert.Empty(ParticipantCrudValidators.ValidatePrimaryDeleteRequest(new ParticipantDeleteRequest { ChatId = ChatId }));
        Assert.Empty(ParticipantCrudValidators.ValidatePrimaryListPageRequest(new ParticipantListPageRequest { ChatId = ChatId }));
    }

    [Fact]
    public void CurrentUser_Validate_WithMissingOrDeletedUser_ShouldReturnCurrentUserNotFound()
    {
        string[] expected = [ParticipantCrudTexts.Messages.Validation.CurrentUserNotFound];
        var deleted = new User { Id = Guid.CreateVersion7(), Login = "ivan", Name = "Иван", IsDeleted = true };

        Assert.Equal(expected, ParticipantCrudValidators.ValidateCurrentUser(null));
        Assert.Equal(expected, ParticipantCrudValidators.ValidateCurrentUser(deleted));
    }

    [Fact]
    public void CurrentUser_Validate_WithActiveUser_ShouldReturnNoErrors()
    {
        var user = new User { Id = Guid.CreateVersion7(), Login = "ivan", Name = "Иван" };

        Assert.Empty(ParticipantCrudValidators.ValidateCurrentUser(user));
    }

    [Fact]
    public void Access_Validate_WithoutParticipant_ShouldReturnNotParticipantAndNotAdmin()
    {
        Assert.Equal([ParticipantCrudTexts.Messages.Validation.NotParticipant], ParticipantCrudValidators.ValidateAccessParticipant(null));
        Assert.Equal([ParticipantCrudTexts.Messages.Validation.NotAdmin], ParticipantCrudValidators.ValidateAccessAdmin(null));
    }

    [Fact]
    public void Access_Validate_WithOrdinaryParticipant_ShouldAllowParticipantAndRejectAdmin()
    {
        var participant = CreateParticipant(isAdmin: false);

        Assert.Empty(ParticipantCrudValidators.ValidateAccessParticipant(participant));
        Assert.Equal([ParticipantCrudTexts.Messages.Validation.NotAdmin], ParticipantCrudValidators.ValidateAccessAdmin(participant));
    }

    [Fact]
    public void Access_Validate_WithAdmin_ShouldAllowAdmin()
    {
        Assert.Empty(ParticipantCrudValidators.ValidateAccessAdmin(CreateParticipant(isAdmin: true)));
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithExistingParticipant_ShouldReturnAlreadyParticipant()
    {
        Assert.Equal([ParticipantCrudTexts.Messages.Validation.AlreadyParticipant], ParticipantCrudValidators.ValidateDomainCreateRequest(CreateParticipant(isAdmin: false)));
        Assert.Empty(ParticipantCrudValidators.ValidateDomainCreateRequest(null));
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithRevokeFromLastAdmin_ShouldReturnLastAdminCannotRevoke()
    {
        var admin = CreateParticipant(isAdmin: true);
        Participant[] participants = [admin, CreateParticipant(isAdmin: false)];

        var errors = ParticipantCrudValidators.ValidateDomainUpdateRequest(new ParticipantUpdateRequest { Id = admin.Id, IsAdmin = false }, admin, participants);

        Assert.Equal([ParticipantCrudTexts.Messages.Validation.LastAdminCannotRevoke], errors);
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithRevokeWhenTwoAdmins_ShouldReturnNoErrors()
    {
        var admin = CreateParticipant(isAdmin: true);
        Participant[] participants = [admin, CreateParticipant(isAdmin: true)];

        var errors = ParticipantCrudValidators.ValidateDomainUpdateRequest(new ParticipantUpdateRequest { Id = admin.Id, IsAdmin = false }, admin, participants);

        Assert.Empty(errors);
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithGrantOrRepeatedRole_ShouldReturnNoErrors()
    {
        var admin = CreateParticipant(isAdmin: true);
        var member = CreateParticipant(isAdmin: false);
        Participant[] participants = [admin, member];

        Assert.Empty(ParticipantCrudValidators.ValidateDomainUpdateRequest(new ParticipantUpdateRequest { Id = member.Id, IsAdmin = true }, member, participants));
        Assert.Empty(ParticipantCrudValidators.ValidateDomainUpdateRequest(new ParticipantUpdateRequest { Id = admin.Id, IsAdmin = true }, admin, participants));
        Assert.Empty(ParticipantCrudValidators.ValidateDomainUpdateRequest(new ParticipantUpdateRequest { Id = member.Id, IsAdmin = false }, member, participants));
    }

    [Fact]
    public void Delete_ValidateDomain_WithSoleParticipant_ShouldReturnSoleParticipantCannotLeave()
    {
        var admin = CreateParticipant(isAdmin: true);

        var errors = ParticipantCrudValidators.ValidateDomainDelete(admin, [admin]);

        Assert.Equal([ParticipantCrudTexts.Messages.Validation.SoleParticipantCannotLeave], errors);
    }

    [Fact]
    public void Delete_ValidateDomain_WithLastAdminAndOthers_ShouldReturnLastAdminCannotLeave()
    {
        var admin = CreateParticipant(isAdmin: true);

        var errors = ParticipantCrudValidators.ValidateDomainDelete(admin, [admin, CreateParticipant(isAdmin: false)]);

        Assert.Equal([ParticipantCrudTexts.Messages.Validation.LastAdminCannotLeave], errors);
    }

    [Fact]
    public void Delete_ValidateDomain_WithOrdinaryParticipantOrOneOfAdmins_ShouldReturnNoErrors()
    {
        var admin = CreateParticipant(isAdmin: true);
        var otherAdmin = CreateParticipant(isAdmin: true);
        var member = CreateParticipant(isAdmin: false);

        Assert.Empty(ParticipantCrudValidators.ValidateDomainDelete(member, [admin, member]));
        Assert.Empty(ParticipantCrudValidators.ValidateDomainDelete(admin, [admin, otherAdmin, member]));
    }
}
