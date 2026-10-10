using Repositories.Security.Ef.Api;
using Services.Security.Api;
using Services.Security.Models;
using Services.Security.Passwords.Providers;
using Services.Security.Texts;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace Tests.Integrations.Security;

[Collection(SecurityServiceCollection.Name)]
public sealed class ServiceUserTests(SecurityServiceFixture fixture)
{
    private const string Password = SecurityServiceFixture.DefaultPassword;

    private static string UniqueLogin() => $"user.{Guid.NewGuid():N}";

    private Task<ResponseInfo<T>> RunAsync<T>(Func<IServiceUser, Task<ResponseInfo<T>>> action) where T : class
        => fixture.RunAsync(action);

    private Task<ResponseInfo<T>> RunAsAsync<T>(Guid? userId, Func<IServiceUser, Task<ResponseInfo<T>>> action) where T : class
        => fixture.RunAsAsync(userId, action);

    private async Task DeleteUserAsync(Guid id)
    {
        var deleted = await RunAsAsync(id, service => service.DeleteAsync(new UserDeleteRequest { Id = id }));
        Assert.Equal(MessageType.SAVED, deleted.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Create_Load_WithoutCurrentUser_ShouldReturnLoadedWithEmptyForm()
    {
        var result = await RunAsAsync(null, service => service.DisplayCreatePageAsync(new UserCreatePageRequest()));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(string.Empty, result.Response!.Login);
        Assert.Equal(string.Empty, result.Response.Name);
    }

    [Fact]
    public async Task Create_Submit_WithoutCurrentUserAndPaddedMixedCaseLogin_ShouldSaveLowercaseLoginTrimmedNameAndGuidV7()
    {
        var login = UniqueLogin();

        var result = await RunAsAsync(null, service => service.CreateAsync(new UserCreateRequest { Login = $"  {login.ToUpperInvariant()} ", Name = "  Иван Петров  ", Password = Password }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.Equal(UserCrudTexts.Messages.Success.CreateCompleted, result.MessageInfo.MessageText);

        var list = await RunAsync(service => service.DisplayListPageAsync(new UserListPageRequest()));
        var row = Assert.Single(list.Response!.Rows, user => user.Login == login);
        Assert.Equal("Иван Петров", row.Name);
        Assert.Equal(7, row.Id.Version);
    }

    [Fact]
    public async Task Create_Submit_WithPassword_ShouldStoreVerifiableHashInsteadOfPassword()
    {
        var login = UniqueLogin();

        await fixture.CreateUserAsync(login, "Иван", " пароль с пробелами ");

        var user = await fixture.RunAsync((IUnitOfWorkSecurity unitOfWork) => unitOfWork.Users.GetSingleByLoginOrDefaultAsync(login));
        var hasher = new AspNetIdentityPasswordHasher();
        Assert.NotEqual(" пароль с пробелами ", user!.PasswordHash);
        Assert.True(hasher.Verify(user.PasswordHash, " пароль с пробелами "));
        Assert.False(hasher.Verify(user.PasswordHash, "пароль с пробелами"));
    }

    [Fact]
    public async Task Create_Submit_WithTooShortPassword_ShouldReturnInvalid()
    {
        var result = await RunAsAsync(null, service => service.CreateAsync(new UserCreateRequest { Login = UniqueLogin(), Name = "Иван", Password = "1234567" }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.PasswordLength(8, 128), result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithLoginDifferingOnlyInCase_ShouldReturnInvalid()
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsAsync(null, service => service.CreateAsync(new UserCreateRequest { Login = login.ToUpperInvariant(), Name = "Другой Иван", Password = Password }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginAlreadyExists, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithLoginOfDeletedUser_ShouldReturnInvalid()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        await DeleteUserAsync(id);

        var result = await RunAsAsync(null, service => service.CreateAsync(new UserCreateRequest { Login = login, Name = "Иван", Password = Password }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginAlreadyExists, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithForbiddenLoginEmptyNameAndEmptyPassword_ShouldReturnInvalidWithAllErrors()
    {
        var result = await RunAsAsync(null, service => service.CreateAsync(new UserCreateRequest { Login = "иван петров", Name = " ", Password = "" }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginFormat, result.MessageInfo.MessageText);
        Assert.Contains(UserCrudTexts.Messages.Validation.NameNotEmpty, result.MessageInfo.MessageText);
        Assert.Contains(UserCrudTexts.Messages.Validation.PasswordNotEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithNullRequest_ShouldReturnBadRequest()
    {
        var result = await RunAsAsync(null, service => service.CreateAsync(null!));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.RequestCannotBeNull, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task List_Load_WithActiveAndDeletedUsers_ShouldReturnOnlyActiveSortedByName()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await fixture.CreateUserAsync($"b.{suffix}", $"Б {suffix}");
        await fixture.CreateUserAsync($"a.{suffix}", $"А {suffix}");
        var deletedId = await fixture.CreateUserAsync($"c.{suffix}", $"В {suffix}");
        await DeleteUserAsync(deletedId);

        var result = await RunAsync(service => service.DisplayListPageAsync(new UserListPageRequest()));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.RowExists);
        Assert.Equal(result.Response.Rows.Count, result.Response.RowCount);
        var names = result.Response.Rows.Select(row => row.Name).Where(name => name.EndsWith(suffix)).ToList();
        Assert.Equal([$"А {suffix}", $"Б {suffix}"], names);
    }

    [Fact]
    public async Task List_Load_WithIds_ShouldReturnOnlyActiveUsersWithTheseIds()
    {
        var firstId = await fixture.CreateUserAsync(UniqueLogin(), "Иван");
        var secondId = await fixture.CreateUserAsync(UniqueLogin(), "Пётр");
        var deletedId = await fixture.CreateUserAsync(UniqueLogin(), "Удалённый");
        await DeleteUserAsync(deletedId);

        var result = await RunAsync(service => service.DisplayListPageAsync(new UserListPageRequest { Ids = [firstId, deletedId] }));

        var row = Assert.Single(result.Response!.Rows);
        Assert.Equal(firstId, row.Id);
        Assert.DoesNotContain(result.Response.Rows, user => user.Id == secondId);
    }

    [Fact]
    public async Task ListAndInfo_Load_WithoutCurrentUser_ShouldReturnUnauthorized()
    {
        var list = await RunAsAsync(null, service => service.DisplayListPageAsync(new UserListPageRequest()));
        var info = await RunAsAsync(null, service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = fixture.DefaultUserId }));

        Assert.Equal(MessageType.UNAUTHORIZED, list.MessageInfo.MessageType);
        Assert.Equal(MessageType.UNAUTHORIZED, info.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.CurrentUserNotFound, info.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Info_Load_WithOtherExistingUser_ShouldReturnUser()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsync(service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(login, result.Response.Login);
        Assert.Equal("Иван", result.Response.Name);
    }

    [Fact]
    public async Task Info_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.UserNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Info_Load_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Load_WithSelf_ShouldReturnUser()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsAsync(id, service => service.DisplayUpdatePageAsync(new UserUpdatePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(login, result.Response.Login);
        Assert.Equal("Иван", result.Response.Name);
    }

    [Fact]
    public async Task UpdateAndDelete_WithOtherUser_ShouldReturnForbidden()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");

        var updatePage = await RunAsync(service => service.DisplayUpdatePageAsync(new UserUpdatePageRequest { Id = id }));
        var update = await RunAsync(service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = UniqueLogin(), Name = "Пётр" }));
        var deletePage = await RunAsync(service => service.DisplayDeletePageAsync(new UserDeletePageRequest { Id = id }));
        var delete = await RunAsync(service => service.DeleteAsync(new UserDeleteRequest { Id = id }));

        Assert.Equal(MessageType.FORBIDDEN, updatePage.MessageInfo.MessageType);
        Assert.Equal(MessageType.FORBIDDEN, update.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.NotSelf, update.MessageInfo.MessageText);
        Assert.Equal(MessageType.FORBIDDEN, deletePage.MessageInfo.MessageType);
        Assert.Equal(MessageType.FORBIDDEN, delete.MessageInfo.MessageType);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = id }));
        Assert.Equal("Иван", info.Response!.Name);
    }

    [Fact]
    public async Task UpdateAndDelete_WithoutCurrentUser_ShouldReturnUnauthorized()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");

        var update = await RunAsAsync(null, service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = UniqueLogin(), Name = "Пётр" }));
        var delete = await RunAsAsync(null, service => service.DeleteAsync(new UserDeleteRequest { Id = id }));

        Assert.Equal(MessageType.UNAUTHORIZED, update.MessageInfo.MessageType);
        Assert.Equal(MessageType.UNAUTHORIZED, delete.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Update_Submit_WithSelfNewLoginAndName_ShouldSaveLowercaseLoginAndTrimmedName()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");
        var newLogin = UniqueLogin();

        var result = await RunAsAsync(id, service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = newLogin.ToUpperInvariant(), Name = " Пётр " }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = id }));
        Assert.Equal(newLogin, info.Response!.Login);
        Assert.Equal("Пётр", info.Response.Name);
    }

    [Fact]
    public async Task Update_Submit_WithOwnLoginInOtherCase_ShouldReturnSaved()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsAsync(id, service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = login.ToUpperInvariant(), Name = "Иван" }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Update_Submit_WithLoginOfOtherUser_ShouldReturnInvalidAndKeepLogin()
    {
        var otherLogin = UniqueLogin();
        await fixture.CreateUserAsync(otherLogin, "Пётр");
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsAsync(id, service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = otherLogin, Name = "Иван" }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginAlreadyExists, result.MessageInfo.MessageText);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = id }));
        Assert.Equal(login, info.Response!.Login);
    }

    [Fact]
    public async Task Update_Submit_WithTooShortLogin_ShouldReturnInvalid()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");

        var result = await RunAsAsync(id, service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = "ab", Name = "Иван" }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginLength(3, 64), result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.UpdateAsync(new UserUpdateRequest { Id = Guid.Empty, Login = UniqueLogin(), Name = "Иван" }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithUnknownIdAndEmptyName_ShouldReturnNotFoundBeforeInvalid()
    {
        var result = await RunAsync(service => service.UpdateAsync(new UserUpdateRequest { Id = Guid.CreateVersion7(), Login = "", Name = "" }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Load_WithSelf_ShouldReturnUser()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsAsync(id, service => service.DisplayDeletePageAsync(new UserDeletePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(login, result.Response.Login);
        Assert.Equal("Иван", result.Response.Name);
    }

    [Fact]
    public async Task Delete_Load_WithUnknownId_ShouldReturnNotFound()
    {
        var result = await RunAsync(service => service.DisplayDeletePageAsync(new UserDeletePageRequest { Id = Guid.CreateVersion7() }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.UserNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Delete_Submit_WithSelf_ShouldHideUserButKeepLoginTaken()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsAsync(id, service => service.DeleteAsync(new UserDeleteRequest { Id = id }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.IsDeleted);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = id }));
        Assert.Equal(MessageType.NOT_FOUND, info.MessageInfo.MessageType);
        var list = await RunAsync(service => service.DisplayListPageAsync(new UserListPageRequest()));
        Assert.DoesNotContain(list.Response!.Rows, row => row.Id == id);
    }

    [Fact]
    public async Task Delete_Submit_WithSelf_ShouldRevokeAllRefreshTokens()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        var first = await fixture.RunAsAsync(null, (IServiceAccount service) => service.LoginAsync(new AccountLoginRequest { Login = login, Password = Password }));
        var second = await fixture.RunAsAsync(null, (IServiceAccount service) => service.LoginAsync(new AccountLoginRequest { Login = login, Password = Password, RememberMe = true }));

        await DeleteUserAsync(id);

        var firstRefresh = await fixture.RunAsAsync(null, (IServiceAccount service) => service.RefreshAsync(new AccountRefreshRequest { RefreshToken = first.Response!.RefreshToken }));
        var secondRefresh = await fixture.RunAsAsync(null, (IServiceAccount service) => service.RefreshAsync(new AccountRefreshRequest { RefreshToken = second.Response!.RefreshToken }));
        Assert.Equal(MessageType.UNAUTHORIZED, firstRefresh.MessageInfo.MessageType);
        Assert.Equal(MessageType.UNAUTHORIZED, secondRefresh.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Submit_WithDeletedSelf_ShouldReturnUnauthorized()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");
        await DeleteUserAsync(id);

        var result = await RunAsAsync(id, service => service.DeleteAsync(new UserDeleteRequest { Id = id }));

        Assert.Equal(MessageType.UNAUTHORIZED, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Submit_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DeleteAsync(new UserDeleteRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }
}
