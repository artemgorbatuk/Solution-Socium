using Datasource.Socium.Ef.Models;
using Services.Socium.Models;
using Services.Socium.Texts;
using Services.Socium.Validation;

namespace Tests.Units.Socium;

public sealed class UserCrudValidatorsTests
{
    private const int LoginMinimumLength = 3;
    private const int LoginMaximumLength = 64;
    private const int NameMaximumLength = 128;

    private static readonly string LoginLength = UserCrudTexts.Messages.Validation.LoginLength(LoginMinimumLength, LoginMaximumLength);

    private static UserCreateRequest CreateRequest(string login = "ivan", string name = "Иван") => new() { Login = login, Name = name };

    [Fact]
    public void CreatePageRequest_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        var errors = UserCrudValidators.ValidatePrimaryCreatePageRequest(null);

        Assert.Equal([UserCrudTexts.Messages.Validation.RequestCannotBeNull], errors);
    }

    [Fact]
    public void CreateRequest_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        var errors = UserCrudValidators.ValidatePrimaryCreateRequest(null);

        Assert.Equal([UserCrudTexts.Messages.Validation.RequestCannotBeNull], errors);
    }

    [Fact]
    public void ListPageRequest_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        var errors = UserCrudValidators.ValidatePrimaryListPageRequest(null);

        Assert.Equal([UserCrudTexts.Messages.Validation.RequestCannotBeNull], errors);
    }

    [Fact]
    public void RequestsWithId_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        string[] expected = [UserCrudTexts.Messages.Validation.RequestCannotBeNull];

        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryUpdatePageRequest(null));
        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryUpdateRequest(null));
        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryDeletePageRequest(null));
        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryDeleteRequest(null));
        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryInfoPageRequest(null));
    }

    [Fact]
    public void RequestsWithId_ValidatePrimary_WithEmptyId_ShouldReturnIdCannotBeEmpty()
    {
        string[] expected = [UserCrudTexts.Messages.Validation.IdCannotBeEmpty];

        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryUpdatePageRequest(new UserUpdatePageRequest { Id = Guid.Empty }));
        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryUpdateRequest(new UserUpdateRequest { Id = Guid.Empty, Login = "ivan", Name = "Иван" }));
        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryDeletePageRequest(new UserDeletePageRequest { Id = Guid.Empty }));
        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryDeleteRequest(new UserDeleteRequest { Id = Guid.Empty }));
        Assert.Equal(expected, UserCrudValidators.ValidatePrimaryInfoPageRequest(new UserInfoPageRequest { Id = Guid.Empty }));
    }

    [Fact]
    public void AllRequests_ValidatePrimary_WithValidRequest_ShouldReturnNoErrors()
    {
        var id = Guid.CreateVersion7();

        Assert.Empty(UserCrudValidators.ValidatePrimaryCreatePageRequest(new UserCreatePageRequest()));
        Assert.Empty(UserCrudValidators.ValidatePrimaryCreateRequest(CreateRequest("", "")));
        Assert.Empty(UserCrudValidators.ValidatePrimaryListPageRequest(new UserListPageRequest()));
        Assert.Empty(UserCrudValidators.ValidatePrimaryUpdatePageRequest(new UserUpdatePageRequest { Id = id }));
        Assert.Empty(UserCrudValidators.ValidatePrimaryUpdateRequest(new UserUpdateRequest { Id = id, Login = "", Name = "" }));
        Assert.Empty(UserCrudValidators.ValidatePrimaryDeletePageRequest(new UserDeletePageRequest { Id = id }));
        Assert.Empty(UserCrudValidators.ValidatePrimaryDeleteRequest(new UserDeleteRequest { Id = id }));
        Assert.Empty(UserCrudValidators.ValidatePrimaryInfoPageRequest(new UserInfoPageRequest { Id = id }));
    }

    [Theory]
    [InlineData("ivan")]
    [InlineData("Ivan.Petrov")]
    [InlineData("ivan_petrov-2")]
    [InlineData("  ivan  ")]
    [InlineData("abc")]
    public void CreateRequest_ValidateDomain_WithValidLogin_ShouldReturnNoErrors(string login)
    {
        Assert.Empty(UserCrudValidators.ValidateDomainCreateRequest(CreateRequest(login)));
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithMaximumLogin_ShouldReturnNoErrors()
    {
        Assert.Empty(UserCrudValidators.ValidateDomainCreateRequest(CreateRequest(new string('a', LoginMaximumLength))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRequest_ValidateDomain_WithEmptyLogin_ShouldReturnLoginNotEmpty(string login)
    {
        var errors = UserCrudValidators.ValidateDomainCreateRequest(CreateRequest(login));

        Assert.Equal([UserCrudTexts.Messages.Validation.LoginNotEmpty], errors);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData(" ab ")]
    public void CreateRequest_ValidateDomain_WithShortLogin_ShouldReturnLoginLength(string login)
    {
        var errors = UserCrudValidators.ValidateDomainCreateRequest(CreateRequest(login));

        Assert.Equal([LoginLength], errors);
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithTooLongLogin_ShouldReturnLoginLength()
    {
        var errors = UserCrudValidators.ValidateDomainCreateRequest(CreateRequest(new string('a', LoginMaximumLength + 1)));

        Assert.Equal([LoginLength], errors);
    }

    [Theory]
    [InlineData("иван")]
    [InlineData("ivan petrov")]
    [InlineData("ivan@mail")]
    public void CreateRequest_ValidateDomain_WithForbiddenCharacters_ShouldReturnLoginFormat(string login)
    {
        var errors = UserCrudValidators.ValidateDomainCreateRequest(CreateRequest(login));

        Assert.Equal([UserCrudTexts.Messages.Validation.LoginFormat], errors);
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithShortLoginAndForbiddenCharacters_ShouldReturnLengthAndFormat()
    {
        var errors = UserCrudValidators.ValidateDomainCreateRequest(CreateRequest("я"));

        Assert.Equal([LoginLength, UserCrudTexts.Messages.Validation.LoginFormat], errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRequest_ValidateDomain_WithEmptyName_ShouldReturnNameNotEmpty(string name)
    {
        var errors = UserCrudValidators.ValidateDomainCreateRequest(CreateRequest(name: name));

        Assert.Equal([UserCrudTexts.Messages.Validation.NameNotEmpty], errors);
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithTooLongName_ShouldReturnNameMaximumLength()
    {
        var errors = UserCrudValidators.ValidateDomainCreateRequest(CreateRequest(name: new string('я', NameMaximumLength + 1)));

        Assert.Equal([UserCrudTexts.Messages.Validation.NameMaximumLength(NameMaximumLength)], errors);
    }

    [Fact]
    public void CreateRequest_ValidateDomain_WithEmptyLoginAndName_ShouldReturnBothErrors()
    {
        var errors = UserCrudValidators.ValidateDomainCreateRequest(CreateRequest("", ""));

        Assert.Equal([UserCrudTexts.Messages.Validation.LoginNotEmpty, UserCrudTexts.Messages.Validation.NameNotEmpty], errors);
    }

    [Fact]
    public void UpdateRequest_ValidateDomain_WithInvalidLoginAndEmptyName_ShouldReturnBothErrors()
    {
        var request = new UserUpdateRequest { Id = Guid.CreateVersion7(), Login = "ivan petrov", Name = " " };

        var errors = UserCrudValidators.ValidateDomainUpdateRequest(request);

        Assert.Equal([UserCrudTexts.Messages.Validation.LoginFormat, UserCrudTexts.Messages.Validation.NameNotEmpty], errors);
    }

    [Fact]
    public void User_ValidateAccessibility_WithMissingUser_ShouldReturnUserNotFoundById()
    {
        var errors = UserCrudValidators.ValidateAccessibilityUser(null);

        Assert.Equal([UserCrudTexts.Messages.Validation.UserNotFoundById], errors);
    }

    [Fact]
    public void User_ValidateAccessibility_WithDeletedUser_ShouldReturnUserNotFoundById()
    {
        var user = new User { Id = Guid.CreateVersion7(), Login = "ivan", Name = "Иван", IsDeleted = true };

        var errors = UserCrudValidators.ValidateAccessibilityUser(user);

        Assert.Equal([UserCrudTexts.Messages.Validation.UserNotFoundById], errors);
    }

    [Fact]
    public void User_ValidateAccessibility_WithActiveUser_ShouldReturnNoErrors()
    {
        var user = new User { Id = Guid.CreateVersion7(), Login = "ivan", Name = "Иван" };

        Assert.Empty(UserCrudValidators.ValidateAccessibilityUser(user));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void Login_ValidateDuplicates_WithDuplicateFlag_ShouldReturnLoginAlreadyExistsOnlyForDuplicate(bool hasDuplicate, int expectedCount)
    {
        var errors = UserCrudValidators.ValidateDuplicates(hasDuplicate).ToList();

        Assert.Equal(expectedCount, errors.Count);
        Assert.All(errors, error => Assert.Equal(UserCrudTexts.Messages.Validation.LoginAlreadyExists, error));
    }
}
