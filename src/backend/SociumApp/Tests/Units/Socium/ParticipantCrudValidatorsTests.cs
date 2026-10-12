using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
using Services.Socium.Ports;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Tests.Units.Socium;

public sealed class ParticipantCrudValidatorsTests
{
    private static readonly Guid ChatId = Guid.CreateVersion7();

    private static Participant CreateParticipant() => new()
    {
        Id = Guid.CreateVersion7(),
        ChatId = ChatId,
        UserId = Guid.CreateVersion7(),
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
    public void CurrentUser_Validate_WithMissingOrFoundUser_ShouldRejectOnlyMissing()
    {
        Assert.Equal([ParticipantCrudTexts.Messages.Validation.CurrentUserNotFound], ParticipantCrudValidators.ValidateCurrentUser(null));
        Assert.Empty(ParticipantCrudValidators.ValidateCurrentUser(new UserPortModel { Id = Guid.CreateVersion7(), Login = "ivan", Name = "Иван" }));
    }

    [Fact]
    public void Access_Validate_WithoutParticipant_ShouldReturnNotParticipant()
    {
        Assert.Equal([ParticipantCrudTexts.Messages.Validation.NotParticipant], ParticipantCrudValidators.ValidateAccessParticipant(null));
    }

    [Fact]
    public void Access_Validate_WithParticipant_ShouldAllow()
    {
        Assert.Empty(ParticipantCrudValidators.ValidateAccessParticipant(CreateParticipant()));
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithExistingParticipant_ShouldReturnAlreadyParticipant()
    {
        Assert.Equal([ParticipantCrudTexts.Messages.Validation.AlreadyParticipant], ParticipantCrudValidators.ValidateDomainCreateRequest(CreateParticipant()));
        Assert.Empty(ParticipantCrudValidators.ValidateDomainCreateRequest(null));
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithRevokeFromLastAdmin_ShouldReturnLastAdminCannotRevoke()
    {
        var errors = ParticipantCrudValidators.ValidateDomainUpdateRequest(
            new ParticipantUpdateRequest { Id = Guid.CreateVersion7(), IsAdmin = false },
            targetIsAdmin: true,
            adminCount: 1);

        Assert.Equal([ParticipantCrudTexts.Messages.Validation.LastAdminCannotRevoke], errors);
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithRevokeWhenTwoAdmins_ShouldReturnNoErrors()
    {
        var errors = ParticipantCrudValidators.ValidateDomainUpdateRequest(
            new ParticipantUpdateRequest { Id = Guid.CreateVersion7(), IsAdmin = false },
            targetIsAdmin: true,
            adminCount: 2);

        Assert.Empty(errors);
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithGrantOrRepeatedRole_ShouldReturnNoErrors()
    {
        var id = Guid.CreateVersion7();

        Assert.Empty(ParticipantCrudValidators.ValidateDomainUpdateRequest(
            new ParticipantUpdateRequest { Id = id, IsAdmin = true },
            targetIsAdmin: false,
            adminCount: 1));
        Assert.Empty(ParticipantCrudValidators.ValidateDomainUpdateRequest(
            new ParticipantUpdateRequest { Id = id, IsAdmin = true },
            targetIsAdmin: true,
            adminCount: 1));
        Assert.Empty(ParticipantCrudValidators.ValidateDomainUpdateRequest(
            new ParticipantUpdateRequest { Id = id, IsAdmin = false },
            targetIsAdmin: false,
            adminCount: 1));
    }

    [Fact]
    public void Delete_ValidateDomain_WithSoleParticipant_ShouldReturnSoleParticipantCannotLeave()
    {
        var errors = ParticipantCrudValidators.ValidateDomainDelete(
            currentIsAdmin: true,
            participantCount: 1,
            adminCount: 1);

        Assert.Equal([ParticipantCrudTexts.Messages.Validation.SoleParticipantCannotLeave], errors);
    }

    [Fact]
    public void Delete_ValidateDomain_WithLastAdminAndOthers_ShouldReturnLastAdminCannotLeave()
    {
        var errors = ParticipantCrudValidators.ValidateDomainDelete(
            currentIsAdmin: true,
            participantCount: 2,
            adminCount: 1);

        Assert.Equal([ParticipantCrudTexts.Messages.Validation.LastAdminCannotLeave], errors);
    }

    [Fact]
    public void Delete_ValidateDomain_WithOrdinaryParticipantOrOneOfAdmins_ShouldReturnNoErrors()
    {
        Assert.Empty(ParticipantCrudValidators.ValidateDomainDelete(
            currentIsAdmin: false,
            participantCount: 2,
            adminCount: 1));
        Assert.Empty(ParticipantCrudValidators.ValidateDomainDelete(
            currentIsAdmin: true,
            participantCount: 3,
            adminCount: 2));
    }
}
