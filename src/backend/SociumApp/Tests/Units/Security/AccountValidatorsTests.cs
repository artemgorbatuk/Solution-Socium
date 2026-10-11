using Datasource.Security.Ef.Models;
using Services.Security.Models;
using Services.Security.Texts;
using Services.Security.Validation;

namespace Tests.Units.Security;

public sealed class AccountValidatorsTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

    private static User CreateUser(bool isDeleted = false, bool isEmailConfirmed = true) => new()
    {
        Id = Guid.CreateVersion7(),
        Login = "ivan",
        Email = "ivan@example.com",
        Name = "Иван",
        PasswordHash = "hash",
        IsDeleted = isDeleted,
        IsEmailConfirmed = isEmailConfirmed,
    };

    private static RefreshToken CreateToken(DateTime expiresAt, User? user = null, DateTime? revokedAt = null)
    {
        user ??= CreateUser();
        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = "hash",
            IsPersistent = false,
            CreatedAt = Now.AddDays(-1),
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
            User = user,
        };
    }

    [Fact]
    public void LoginRequest_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        Assert.Equal([AccountTexts.Messages.Validation.RequestCannotBeNull], AccountValidators.ValidatePrimaryLoginRequest(null));
        Assert.Empty(AccountValidators.ValidatePrimaryLoginRequest(new AccountLoginRequest { Login = "", Password = "" }));
    }

    [Fact]
    public void Credentials_Validate_WithUnknownDeletedOrWrongPassword_ShouldReturnSameInvalidCredentials()
    {
        string[] expected = [AccountTexts.Messages.Validation.InvalidCredentials];

        Assert.Equal(expected, AccountValidators.ValidateCredentials(null, false));
        Assert.Equal(expected, AccountValidators.ValidateCredentials(CreateUser(isDeleted: true), true));
        Assert.Equal(expected, AccountValidators.ValidateCredentials(CreateUser(isEmailConfirmed: false), true));
        Assert.Equal(expected, AccountValidators.ValidateCredentials(CreateUser(), false));
        Assert.Empty(AccountValidators.ValidateCredentials(CreateUser(), true));
    }

    [Fact]
    public void RefreshToken_Validate_WithMissingExpiredOrDeletedUserToken_ShouldReturnRefreshTokenInvalid()
    {
        string[] expected = [AccountTexts.Messages.Validation.RefreshTokenInvalid];

        Assert.Equal(expected, AccountValidators.ValidateRefreshToken(null, Now));
        Assert.Equal(expected, AccountValidators.ValidateRefreshToken(CreateToken(Now), Now));
        Assert.Equal(expected, AccountValidators.ValidateRefreshToken(CreateToken(Now.AddDays(1), CreateUser(isDeleted: true)), Now));
        Assert.Empty(AccountValidators.ValidateRefreshToken(CreateToken(Now.AddSeconds(1)), Now));
    }

    [Fact]
    public void RefreshToken_IsReused_WithRevokedToken_ShouldReturnTrueOnlyForRevoked()
    {
        Assert.False(AccountValidators.IsReused(null));
        Assert.False(AccountValidators.IsReused(CreateToken(Now.AddDays(1))));
        Assert.True(AccountValidators.IsReused(CreateToken(Now.AddDays(1), revokedAt: Now.AddMinutes(-1))));
    }

    [Fact]
    public void ProfileRequests_Validate_WithNullOrEmptyName_ShouldReturnErrors()
    {
        Assert.Equal([AccountTexts.Messages.Validation.RequestCannotBeNull], AccountValidators.ValidatePrimaryProfilePageRequest(null));
        Assert.Equal([AccountTexts.Messages.Validation.RequestCannotBeNull], AccountValidators.ValidatePrimaryProfileUpdateRequest(null));
        Assert.Equal([UserCrudTexts.Messages.Validation.NameNotEmpty], AccountValidators.ValidateDomainProfileUpdateRequest(new AccountProfileUpdateRequest { Name = " " }));
        Assert.Empty(AccountValidators.ValidateDomainProfileUpdateRequest(new AccountProfileUpdateRequest { Name = "Иван" }));
    }

    [Fact]
    public void CurrentUser_Validate_WithMissingOrDeletedUser_ShouldReturnCurrentUserNotFound()
    {
        string[] expected = [AccountTexts.Messages.Validation.CurrentUserNotFound];

        Assert.Equal(expected, AccountValidators.ValidateCurrentUser(null));
        Assert.Equal(expected, AccountValidators.ValidateCurrentUser(CreateUser(isDeleted: true)));
        Assert.Empty(AccountValidators.ValidateCurrentUser(CreateUser()));
    }
}
