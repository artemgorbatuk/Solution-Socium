using Services.Socium.Models;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Tests.Units.Socium;

public sealed class RoomCrudValidatorsTests
{
    private const int NameMaximumLength = 128;

    [Fact]
    public void CreatePageRequest_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        var errors = RoomCrudValidators.ValidatePrimaryCreatePageRequest(null);

        Assert.Equal([RoomCrudTexts.Messages.Validation.RequestCannotBeNull], errors);
    }

    [Fact]
    public void CreateRequest_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        var errors = RoomCrudValidators.ValidatePrimaryCreateRequest(null);

        Assert.Equal([RoomCrudTexts.Messages.Validation.RequestCannotBeNull], errors);
    }

    [Fact]
    public void ListPageRequest_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        var errors = RoomCrudValidators.ValidatePrimaryListPageRequest(null);

        Assert.Equal([RoomCrudTexts.Messages.Validation.RequestCannotBeNull], errors);
    }

    [Fact]
    public void RequestsWithId_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        string[] expected = [RoomCrudTexts.Messages.Validation.RequestCannotBeNull];

        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryUpdatePageRequest(null));
        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryUpdateRequest(null));
        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryDeletePageRequest(null));
        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryDeleteRequest(null));
        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryInfoPageRequest(null));
    }

    [Fact]
    public void RequestsWithId_ValidatePrimary_WithEmptyId_ShouldReturnIdCannotBeEmpty()
    {
        string[] expected = [RoomCrudTexts.Messages.Validation.IdCannotBeEmpty];

        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryUpdatePageRequest(new RoomUpdatePageRequest { Id = Guid.Empty }));
        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryUpdateRequest(new RoomUpdateRequest { Id = Guid.Empty, Name = "Room" }));
        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryDeletePageRequest(new RoomDeletePageRequest { Id = Guid.Empty }));
        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryDeleteRequest(new RoomDeleteRequest { Id = Guid.Empty }));
        Assert.Equal(expected, RoomCrudValidators.ValidatePrimaryInfoPageRequest(new RoomInfoPageRequest { Id = Guid.Empty }));
    }

    [Fact]
    public void AllRequests_ValidatePrimary_WithValidRequest_ShouldReturnNoErrors()
    {
        var id = Guid.CreateVersion7();

        Assert.Empty(RoomCrudValidators.ValidatePrimaryCreatePageRequest(new RoomCreatePageRequest()));
        Assert.Empty(RoomCrudValidators.ValidatePrimaryCreateRequest(new RoomCreateRequest { Name = "" }));
        Assert.Empty(RoomCrudValidators.ValidatePrimaryListPageRequest(new RoomListPageRequest()));
        Assert.Empty(RoomCrudValidators.ValidatePrimaryUpdatePageRequest(new RoomUpdatePageRequest { Id = id }));
        Assert.Empty(RoomCrudValidators.ValidatePrimaryUpdateRequest(new RoomUpdateRequest { Id = id, Name = "" }));
        Assert.Empty(RoomCrudValidators.ValidatePrimaryDeletePageRequest(new RoomDeletePageRequest { Id = id }));
        Assert.Empty(RoomCrudValidators.ValidatePrimaryDeleteRequest(new RoomDeleteRequest { Id = id }));
        Assert.Empty(RoomCrudValidators.ValidatePrimaryInfoPageRequest(new RoomInfoPageRequest { Id = id }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRequest_ValidateDomain_WithEmptyName_ShouldReturnNameNotEmpty(string name)
    {
        var errors = RoomCrudValidators.ValidateDomainCreateRequest(new RoomCreateRequest { Name = name });

        Assert.Equal([RoomCrudTexts.Messages.Validation.NameNotEmpty], errors);
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithTooLongName_ShouldReturnNameMaximumLength()
    {
        var request = new RoomCreateRequest { Name = new string('a', NameMaximumLength + 1) };

        var errors = RoomCrudValidators.ValidateDomainCreateRequest(request);

        Assert.Equal([RoomCrudTexts.Messages.Validation.NameMaximumLength(NameMaximumLength)], errors);
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithMaximumNameAndSurroundingSpaces_ShouldReturnNoErrors()
    {
        var request = new RoomCreateRequest { Name = $"  {new string('a', NameMaximumLength)}  " };

        Assert.Empty(RoomCrudValidators.ValidateDomainCreateRequest(request));
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithEmptyName_ShouldReturnNameNotEmpty()
    {
        var request = new RoomUpdateRequest { Id = Guid.CreateVersion7(), Name = " " };

        var errors = RoomCrudValidators.ValidateDomainUpdateRequest(request);

        Assert.Equal([RoomCrudTexts.Messages.Validation.NameNotEmpty], errors);
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithTooLongName_ShouldReturnNameMaximumLength()
    {
        var request = new RoomUpdateRequest { Id = Guid.CreateVersion7(), Name = new string('a', NameMaximumLength + 1) };

        var errors = RoomCrudValidators.ValidateDomainUpdateRequest(request);

        Assert.Equal([RoomCrudTexts.Messages.Validation.NameMaximumLength(NameMaximumLength)], errors);
    }

    [Fact]
    public void Room_ValidateAccessibility_WithMissingRoom_ShouldReturnRoomNotFoundById()
    {
        var errors = RoomCrudValidators.ValidateAccessibilityRoom<object>(null);

        Assert.Equal([RoomCrudTexts.Messages.Validation.RoomNotFoundById], errors);
    }

    [Fact]
    public void Room_ValidateAccessibility_WithExistingRoom_ShouldReturnNoErrors()
    {
        Assert.Empty(RoomCrudValidators.ValidateAccessibilityRoom(new object()));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void Name_ValidateDuplicates_WithDuplicateFlag_ShouldReturnNameAlreadyExistsOnlyForDuplicate(bool hasDuplicate, int expectedCount)
    {
        var errors = RoomCrudValidators.ValidateDuplicates(hasDuplicate).ToList();

        Assert.Equal(expectedCount, errors.Count);
        Assert.All(errors, error => Assert.Equal(RoomCrudTexts.Messages.Validation.NameAlreadyExists, error));
    }
}
