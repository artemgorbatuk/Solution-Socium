using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Api;
using Services.Socium.Models;
using Services.Socium.Texts;

namespace Tests.Integrations.Socium;

[Collection(SociumServiceCollection.Name)]
public sealed class ServiceUserTests(SociumServiceFixture fixture)
{
    private static string UniqueLogin() => $"user.{Guid.NewGuid():N}";

    private Task<ResponseInfo<T>> RunAsync<T>(Func<IServiceUser, Task<ResponseInfo<T>>> action) where T : class
        => fixture.RunAsync(action);

    private async Task DeleteUserAsync(Guid id)
    {
        var deleted = await RunAsync(service => service.DeleteAsync(new UserDeleteRequest { Id = id }));
        Assert.Equal(MessageType.SAVED, deleted.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Create_Load_WithNoParameters_ShouldReturnLoadedWithEmptyForm()
    {
        var result = await RunAsync(service => service.DisplayCreatePageAsync(new UserCreatePageRequest()));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(string.Empty, result.Response!.Login);
        Assert.Equal(string.Empty, result.Response.Name);
    }

    [Fact]
    public async Task Create_Submit_WithPaddedMixedCaseLogin_ShouldSaveLowercaseLoginTrimmedNameAndGuidV7()
    {
        var login = UniqueLogin();

        var result = await RunAsync(service => service.CreateAsync(new UserCreateRequest { Login = $"  {login.ToUpperInvariant()} ", Name = "  Иван Петров  " }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.Equal(UserCrudTexts.Messages.Success.CreateCompleted, result.MessageInfo.MessageText);

        var list = await RunAsync(service => service.DisplayListPageAsync(new UserListPageRequest()));
        var row = Assert.Single(list.Response!.Rows, user => user.Login == login);
        Assert.Equal("Иван Петров", row.Name);
        Assert.Equal(7, row.Id.Version);
    }

    [Fact]
    public async Task Create_Submit_WithLoginDifferingOnlyInCase_ShouldReturnInvalid()
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsync(service => service.CreateAsync(new UserCreateRequest { Login = login.ToUpperInvariant(), Name = "Другой Иван" }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginAlreadyExists, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithLoginOfDeletedUser_ShouldReturnInvalid()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        await DeleteUserAsync(id);

        var result = await RunAsync(service => service.CreateAsync(new UserCreateRequest { Login = login, Name = "Иван" }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginAlreadyExists, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithForbiddenLoginAndEmptyName_ShouldReturnInvalidWithBothErrors()
    {
        var result = await RunAsync(service => service.CreateAsync(new UserCreateRequest { Login = "иван петров", Name = " " }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginFormat, result.MessageInfo.MessageText);
        Assert.Contains(UserCrudTexts.Messages.Validation.NameNotEmpty, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_Submit_WithNullRequest_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.CreateAsync(null!));

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
    public async Task Info_Load_WithExistingUser_ShouldReturnUser()
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
    public async Task Update_Load_WithExistingUser_ShouldReturnUser()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new UserUpdatePageRequest { Id = id }));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(id, result.Response!.Id);
        Assert.Equal(login, result.Response.Login);
        Assert.Equal("Иван", result.Response.Name);
    }

    [Fact]
    public async Task Update_Load_WithDeletedUser_ShouldReturnNotFound()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");
        await DeleteUserAsync(id);

        var result = await RunAsync(service => service.DisplayUpdatePageAsync(new UserUpdatePageRequest { Id = id }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.UserNotFoundById, result.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Update_Submit_WithNewLoginAndName_ShouldSaveLowercaseLoginAndTrimmedName()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");
        var newLogin = UniqueLogin();

        var result = await RunAsync(service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = newLogin.ToUpperInvariant(), Name = " Пётр " }));

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

        var result = await RunAsync(service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = login.ToUpperInvariant(), Name = "Иван" }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Update_Submit_WithLoginOfOtherUser_ShouldReturnInvalidAndKeepLogin()
    {
        var otherLogin = UniqueLogin();
        await fixture.CreateUserAsync(otherLogin, "Пётр");
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsync(service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = otherLogin, Name = "Иван" }));

        Assert.Equal(MessageType.INVALID, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginAlreadyExists, result.MessageInfo.MessageText);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = id }));
        Assert.Equal(login, info.Response!.Login);
    }

    [Fact]
    public async Task Update_Submit_WithTooShortLogin_ShouldReturnInvalid()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");

        var result = await RunAsync(service => service.UpdateAsync(new UserUpdateRequest { Id = id, Login = "ab", Name = "Иван" }));

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
    public async Task Delete_Load_WithExistingUser_ShouldReturnUser()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsync(service => service.DisplayDeletePageAsync(new UserDeletePageRequest { Id = id }));

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
    public async Task Delete_Submit_WithExistingUser_ShouldHideUserButKeepLoginTaken()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var result = await RunAsync(service => service.DeleteAsync(new UserDeleteRequest { Id = id }));

        Assert.Equal(MessageType.SAVED, result.MessageInfo.MessageType);
        Assert.True(result.Response!.IsDeleted);
        var info = await RunAsync(service => service.DisplayInfoPageAsync(new UserInfoPageRequest { Id = id }));
        Assert.Equal(MessageType.NOT_FOUND, info.MessageInfo.MessageType);
        var list = await RunAsync(service => service.DisplayListPageAsync(new UserListPageRequest()));
        Assert.DoesNotContain(list.Response!.Rows, row => row.Id == id);
    }

    [Fact]
    public async Task Delete_Submit_WithAlreadyDeletedUser_ShouldReturnNotFound()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");
        await DeleteUserAsync(id);

        var result = await RunAsync(service => service.DeleteAsync(new UserDeleteRequest { Id = id }));

        Assert.Equal(MessageType.NOT_FOUND, result.MessageInfo.MessageType);
    }

    [Fact]
    public async Task Delete_Submit_WithEmptyId_ShouldReturnBadRequest()
    {
        var result = await RunAsync(service => service.DeleteAsync(new UserDeleteRequest { Id = Guid.Empty }));

        Assert.Equal(MessageType.BAD_REQUEST, result.MessageInfo.MessageType);
        Assert.Contains(UserCrudTexts.Messages.Validation.IdCannotBeEmpty, result.MessageInfo.MessageText);
    }
}
