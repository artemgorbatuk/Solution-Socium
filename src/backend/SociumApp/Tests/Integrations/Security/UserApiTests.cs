using System.Net;
using System.Net.Http.Json;
using Services.Security.Models;
using Services.Security.Texts;
using Services.Shared.Enums;
using static Tests.Infrastructure.ApiAssert;

namespace Tests.Integrations.Security;

[Collection(SecurityApiCollection.Name)]
public sealed class UserApiTests(SecurityApiFixture fixture)
{
    private const string BaseUrl = "/api/user";
    private const string Password = SecurityApiFixture.DefaultPassword;

    private HttpClient Client => fixture.Client;

    private static string UniqueLogin() => $"user.{Guid.NewGuid():N}";

    [Fact]
    public async Task Create_GET_WithoutToken_ShouldReturn200WithEmptyForm()
    {
        using var anonymous = fixture.CreateAnonymousClient();

        var response = await anonymous.GetAsync($"{BaseUrl}/create", TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<UserCreatePageResponse>(response);
        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        Assert.Equal(string.Empty, body.Response!.Login);
        Assert.Equal(string.Empty, body.Response.Name);
    }

    [Fact]
    public async Task Create_POST_WithoutTokenAndValidUser_ShouldReturn200Saved()
    {
        using var anonymous = fixture.CreateAnonymousClient();

        var response = await anonymous.PostAsJsonAsync(BaseUrl, new UserCreateRequest { Email = "user@example.com", Login = UniqueLogin(), Name = "Иван", Password = Password }, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<UserCreatePageResponse>(response);
        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        Assert.Equal(UserCrudTexts.Messages.Success.CreateCompleted, body.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_POST_WithLoginDifferingOnlyInCase_ShouldReturn422()
    {
        var login = UniqueLogin();
        await fixture.CreateUserAsync(login, "Иван");

        var response = await Client.PostAsJsonAsync(BaseUrl, new UserCreateRequest { Email = "user@example.com", Login = login.ToUpperInvariant(), Name = "Иван", Password = Password }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginAlreadyExists, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithForbiddenLoginAndShortPassword_ShouldReturn422()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new UserCreateRequest { Email = "user@example.com", Login = "ivan petrov", Name = "Иван", Password = "short" }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginFormat, problem.Detail);
        Assert.Contains(UserCrudTexts.Messages.Validation.PasswordLength(8, 128), problem.Detail);
    }

    [Fact]
    public async Task ListAndInfo_GET_WithoutToken_ShouldReturn401()
    {
        using var anonymous = fixture.CreateAnonymousClient();

        var list = await anonymous.GetAsync(BaseUrl, TestContext.Current.CancellationToken);
        var info = await anonymous.GetAsync($"{BaseUrl}/info?id={fixture.DefaultUserId}", TestContext.Current.CancellationToken);

        await ReadProblemAsync(list, HttpStatusCode.Unauthorized);
        await ReadProblemAsync(info, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_GET_WithExistingUser_ShouldReturn200WithUser()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var body = await ReadSuccessAsync<UserListPageResponse>(await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        Assert.Contains(body.Response!.Rows, row => row.Id == id && row.Login == login && row.Name == "Иван");
    }

    [Fact]
    public async Task List_GET_WithIds_ShouldReturnOnlyTheseUsers()
    {
        var firstId = await fixture.CreateUserAsync(UniqueLogin(), "Иван");
        var secondId = await fixture.CreateUserAsync(UniqueLogin(), "Пётр");
        await fixture.CreateUserAsync(UniqueLogin(), "Сергей");

        var body = await ReadSuccessAsync<UserListPageResponse>(await Client.GetAsync($"{BaseUrl}?ids={firstId}&ids={secondId}", TestContext.Current.CancellationToken));

        Assert.Equal([firstId, secondId], body.Response!.Rows.Select(row => row.Id).Order());
    }

    [Fact]
    public async Task Info_GET_WithExistingUser_ShouldReturn200WithUser()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");

        var body = await ReadSuccessAsync<UserInfoPageResponse>(await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(login, body.Response.Login);
    }

    [Fact]
    public async Task Info_GET_WithEmptyId_ShouldReturn400()
    {
        var response = await Client.GetAsync($"{BaseUrl}/info?id={Guid.Empty}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(UserCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Info_GET_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/info?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(UserCrudTexts.Messages.Validation.UserNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Update_GETAndPUT_WithSelf_ShouldReturnFormAndChangeUser()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        using var self = fixture.CreateClientAs(id);
        var newLogin = UniqueLogin();

        var form = await ReadSuccessAsync<UserUpdatePageResponse>(await self.GetAsync($"{BaseUrl}/update?id={id}", TestContext.Current.CancellationToken));
        var response = await self.PutAsJsonAsync(BaseUrl, new UserUpdateRequest { Id = id, Login = newLogin, Name = "Пётр" }, TestContext.Current.CancellationToken);

        Assert.Equal(login, form.Response!.Login);
        Assert.Equal(MessageType.SAVED, (await ReadSuccessAsync<UserUpdatePageResponse>(response)).MessageInfo.MessageType);
        var info = await ReadSuccessAsync<UserInfoPageResponse>(await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken));
        Assert.Equal(newLogin, info.Response!.Login);
        Assert.Equal("Пётр", info.Response.Name);
    }

    [Fact]
    public async Task UpdateAndDelete_WithOtherUser_ShouldReturn403()
    {
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");

        var update = await Client.PutAsJsonAsync(BaseUrl, new UserUpdateRequest { Id = id, Login = UniqueLogin(), Name = "Пётр" }, TestContext.Current.CancellationToken);
        var delete = await Client.DeleteAsync($"{BaseUrl}?id={id}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(update, HttpStatusCode.Forbidden);
        Assert.Contains(UserCrudTexts.Messages.Validation.NotSelf, problem.Detail);
        await ReadProblemAsync(delete, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_PUT_WithLoginOfOtherUser_ShouldReturn422()
    {
        var otherLogin = UniqueLogin();
        await fixture.CreateUserAsync(otherLogin, "Пётр");
        var id = await fixture.CreateUserAsync(UniqueLogin(), "Иван");
        using var self = fixture.CreateClientAs(id);

        var response = await self.PutAsJsonAsync(BaseUrl, new UserUpdateRequest { Id = id, Login = otherLogin, Name = "Иван" }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginAlreadyExists, problem.Detail);
    }

    [Fact]
    public async Task Update_PUT_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.PutAsJsonAsync(BaseUrl, new UserUpdateRequest { Id = Guid.CreateVersion7(), Login = UniqueLogin(), Name = "Иван" }, TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_GETAndDELETE_WithSelf_ShouldConfirmHideUserAndEndSession()
    {
        var login = UniqueLogin();
        var id = await fixture.CreateUserAsync(login, "Иван");
        using var self = fixture.CreateClientAs(id);

        var confirm = await ReadSuccessAsync<UserDeletePageResponse>(await self.GetAsync($"{BaseUrl}/delete?id={id}", TestContext.Current.CancellationToken));
        var body = await ReadSuccessAsync<UserDeleteResponse>(await self.DeleteAsync($"{BaseUrl}?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(login, confirm.Response!.Login);
        Assert.True(body.Response!.IsDeleted);
        var info = await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, info.StatusCode);
        var profile = await self.GetAsync("/api/account/profile", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }

    [Fact]
    public async Task Delete_DELETE_WithEmptyId_ShouldReturn400()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}?id={Guid.Empty}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(UserCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }
}
