using System.Net;
using System.Net.Http.Json;
using Services.Shared.Enums;
using Services.Socium.Models;
using Services.Socium.Texts;
using static Tests.Infrastructure.ApiAssert;

namespace Tests.Integrations.Socium;

[Collection(SociumApiCollection.Name)]
public sealed class UserApiTests(SociumApiFixture fixture)
{
    private const string BaseUrl = "/api/user";

    private HttpClient Client => fixture.Client;

    private static string UniqueLogin() => $"user.{Guid.NewGuid():N}";

    private async Task<Guid> CreateUserAsync(string login, string name = "Иван")
    {
        var created = await Client.PostAsJsonAsync(BaseUrl, new UserCreateRequest { Login = login, Name = name }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var list = await ReadSuccessAsync<UserListPageResponse>(await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken));
        return list.Response!.Rows.Single(row => row.Login == login).Id;
    }

    [Fact]
    public async Task Create_GET_WithNoParameters_ShouldReturn200WithEmptyForm()
    {
        var response = await Client.GetAsync($"{BaseUrl}/create", TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<UserCreatePageResponse>(response);
        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        Assert.Equal(string.Empty, body.Response!.Login);
        Assert.Equal(string.Empty, body.Response.Name);
    }

    [Fact]
    public async Task Create_POST_WithValidUser_ShouldReturn200Saved()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new UserCreateRequest { Login = UniqueLogin(), Name = "Иван" }, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<UserCreatePageResponse>(response);
        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        Assert.Equal(UserCrudTexts.Messages.Success.CreateCompleted, body.MessageInfo.MessageText);
    }

    [Fact]
    public async Task Create_POST_WithLoginDifferingOnlyInCase_ShouldReturn422()
    {
        var login = UniqueLogin();
        await CreateUserAsync(login);

        var response = await Client.PostAsJsonAsync(BaseUrl, new UserCreateRequest { Login = login.ToUpperInvariant(), Name = "Иван" }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginAlreadyExists, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithForbiddenLogin_ShouldReturn422()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new UserCreateRequest { Login = "ivan petrov", Name = "Иван" }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(UserCrudTexts.Messages.Validation.LoginFormat, problem.Detail);
    }

    [Fact]
    public async Task List_GET_WithExistingUser_ShouldReturn200WithUser()
    {
        var login = UniqueLogin();
        var id = await CreateUserAsync(login, "Иван");

        var body = await ReadSuccessAsync<UserListPageResponse>(await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        Assert.Contains(body.Response!.Rows, row => row.Id == id && row.Login == login && row.Name == "Иван");
    }

    [Fact]
    public async Task Info_GET_WithExistingUser_ShouldReturn200WithUser()
    {
        var login = UniqueLogin();
        var id = await CreateUserAsync(login);

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
    public async Task Update_GET_WithExistingUser_ShouldReturn200WithForm()
    {
        var login = UniqueLogin();
        var id = await CreateUserAsync(login, "Иван");

        var body = await ReadSuccessAsync<UserUpdatePageResponse>(await Client.GetAsync($"{BaseUrl}/update?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(login, body.Response.Login);
        Assert.Equal("Иван", body.Response.Name);
    }

    [Fact]
    public async Task Update_PUT_WithNewLoginAndName_ShouldReturn200AndChangeUser()
    {
        var id = await CreateUserAsync(UniqueLogin());
        var newLogin = UniqueLogin();

        var response = await Client.PutAsJsonAsync(BaseUrl, new UserUpdateRequest { Id = id, Login = newLogin, Name = "Пётр" }, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<UserUpdatePageResponse>(response);
        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        var info = await ReadSuccessAsync<UserInfoPageResponse>(await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken));
        Assert.Equal(newLogin, info.Response!.Login);
        Assert.Equal("Пётр", info.Response.Name);
    }

    [Fact]
    public async Task Update_PUT_WithLoginOfOtherUser_ShouldReturn422()
    {
        var otherLogin = UniqueLogin();
        await CreateUserAsync(otherLogin);
        var id = await CreateUserAsync(UniqueLogin());

        var response = await Client.PutAsJsonAsync(BaseUrl, new UserUpdateRequest { Id = id, Login = otherLogin, Name = "Иван" }, TestContext.Current.CancellationToken);

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
    public async Task Delete_GET_WithExistingUser_ShouldReturn200WithConfirmation()
    {
        var login = UniqueLogin();
        var id = await CreateUserAsync(login);

        var body = await ReadSuccessAsync<UserDeletePageResponse>(await Client.GetAsync($"{BaseUrl}/delete?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(login, body.Response.Login);
    }

    [Fact]
    public async Task Delete_DELETE_WithExistingUser_ShouldReturn200AndHideUser()
    {
        var id = await CreateUserAsync(UniqueLogin());

        var body = await ReadSuccessAsync<UserDeleteResponse>(await Client.DeleteAsync($"{BaseUrl}?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        Assert.True(body.Response!.IsDeleted);
        var info = await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, info.StatusCode);
    }

    [Fact]
    public async Task Delete_DELETE_WithEmptyId_ShouldReturn400()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}?id={Guid.Empty}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(UserCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }
}
