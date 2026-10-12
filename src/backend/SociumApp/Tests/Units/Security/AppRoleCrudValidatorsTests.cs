using Services.Security.Models;
using Services.Security.Permissions.Catalogs;
using Services.Security.Texts;
using Services.Security.Validation;

namespace Tests.Units.Security;

public sealed class AppRoleCrudValidatorsTests
{
    [Fact]
    public void Requests_ValidatePrimary_WithNullRequest_ShouldReturnRequestCannotBeNull()
    {
        string[] expected = [AppRoleCrudTexts.Messages.Validation.RequestCannotBeNull];

        Assert.Equal(expected, AppRoleCrudValidators.ValidatePrimaryCreateRequest(null));
        Assert.Equal(expected, AppRoleCrudValidators.ValidatePrimaryDeleteRequest(null));
    }

    [Theory]
    [InlineData(RoleNames.RoomAdmin)]
    [InlineData(RoleNames.ChatAdmin)]
    [InlineData("Unknown.Role")]
    [InlineData("")]
    public void CreateRequest_ValidatePrimary_WithNotAppRole_ShouldReturnUnknownRole(string roleName)
    {
        var errors = AppRoleCrudValidators.ValidatePrimaryCreateRequest(new AppRoleCreateRequest
        {
            UserId = Guid.CreateVersion7(),
            RoleName = roleName,
        });

        Assert.Equal([AppRoleCrudTexts.Messages.Validation.UnknownRole], errors);
    }

    [Fact]
    public void DeleteRequest_ValidatePrimary_WithEmptyUserId_ShouldReturnUserIdCannotBeEmpty()
    {
        var errors = AppRoleCrudValidators.ValidatePrimaryDeleteRequest(new AppRoleDeleteRequest
        {
            UserId = Guid.Empty,
            RoleName = RoleNames.AppAdmin,
        });

        Assert.Equal([AppRoleCrudTexts.Messages.Validation.UserIdCannotBeEmpty], errors);
    }

    [Fact]
    public void Create_ValidateAccess_WithSelf_ShouldReturnCannotGrantSelf()
    {
        var userId = Guid.CreateVersion7();

        Assert.Equal(
            [AppRoleCrudTexts.Messages.Validation.CannotGrantSelf],
            AppRoleCrudValidators.ValidateAccessCreate(allowed: true, userId, userId));
        Assert.Equal(
            [AppRoleCrudTexts.Messages.Validation.AccessDenied],
            AppRoleCrudValidators.ValidateAccessCreate(allowed: false, userId, Guid.CreateVersion7()));
        Assert.Empty(AppRoleCrudValidators.ValidateAccessCreate(allowed: true, userId, Guid.CreateVersion7()));
    }

    [Fact]
    public void Delete_ValidateDomain_WithLastAppAdmin_ShouldReturnLastAppAdminCannotRevoke()
    {
        Assert.Equal(
            [AppRoleCrudTexts.Messages.Validation.LastAppAdminCannotRevoke],
            AppRoleCrudValidators.ValidateDomainDelete(isAssigned: true, appAdminCount: 1));
        Assert.Equal(
            [AppRoleCrudTexts.Messages.Validation.NotAssigned],
            AppRoleCrudValidators.ValidateDomainDelete(isAssigned: false, appAdminCount: 1));
        Assert.Empty(AppRoleCrudValidators.ValidateDomainDelete(isAssigned: true, appAdminCount: 2));
    }

    [Fact]
    public void Create_ValidateDomain_WithAssigned_ShouldReturnAlreadyAssigned()
    {
        Assert.Equal([AppRoleCrudTexts.Messages.Validation.AlreadyAssigned], AppRoleCrudValidators.ValidateDomainCreate(isAssigned: true));
        Assert.Empty(AppRoleCrudValidators.ValidateDomainCreate(isAssigned: false));
    }

    [Fact]
    public void LastAppAdmin_ValidateDomain_WithNotAdmin_ShouldPass()
    {
        Assert.Empty(AppRoleCrudValidators.ValidateDomainLastAppAdmin(isAppAdmin: false, appAdminCount: 1));
    }

    [Fact]
    public void UserDelete_ValidateDomain_WithLastAdminPlaces_ShouldListPlaces()
    {
        var errors = UserCrudValidators.ValidateDomainDelete(["приложение", "комната «A»"]).ToList();

        var error = Assert.Single(errors);
        Assert.StartsWith(UserCrudTexts.Messages.Validation.LastAdminCannotDelete, error);
        Assert.Contains("приложение; комната «A»", error);
        Assert.Empty(UserCrudValidators.ValidateDomainDelete([]));
    }
}
