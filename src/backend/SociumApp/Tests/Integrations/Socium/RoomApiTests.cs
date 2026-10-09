using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Services.Shared.Enums;
using Services.Socium.Models;
using Services.Socium.Texts;
using WebApi.Controllers.Shared;

namespace Tests.Integrations.Socium;

[Collection(RoomApiCollection.Name)]
public sealed class RoomApiTests(RoomApiFixture fixture)
{
    private const string BaseUrl = "/api/room";

    private HttpClient Client => fixture.Client;

    private static string UniqueName() => $"Room {Guid.NewGuid():N}";

    private static async Task<ApiSuccessResponse<T>> ReadSuccessAsync<T>(HttpResponseMessage response) where T : class
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<T>>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        return body;
    }

    private static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        return problem;
    }

    private async Task<Guid> CreateRoomAsync(string name)
    {
        var created = await Client.PostAsJsonAsync(BaseUrl, new RoomCreateRequest { Name = name }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var list = await ReadSuccessAsync<RoomListPageResponse>(await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken));
        return list.Response!.Rows.Single(row => row.Name == name).Id;
    }

    [Fact]
    public async Task Create_GET_WithNoParameters_ShouldReturn200WithEmptyForm()
    {
        var response = await Client.GetAsync($"{BaseUrl}/create", TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<RoomCreatePageResponse>(response);
        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        Assert.Equal(string.Empty, body.Response!.Name);
    }

    [Fact]
    public async Task Create_POST_WithValidName_ShouldReturn200Saved()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new RoomCreateRequest { Name = UniqueName() }, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<RoomCreatePageResponse>(response);
        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        Assert.Equal(RoomCrudTexts.Messages.Success.CreateCompleted, body.MessageInfo.MessageText);
        Assert.Equal(MessageType.GetAlertClass(MessageType.SAVED), body.MessageAlert);
    }

    [Fact]
    public async Task Create_POST_WithDuplicateName_ShouldReturn422()
    {
        var name = UniqueName();
        await CreateRoomAsync(name);

        var response = await Client.PostAsJsonAsync(BaseUrl, new RoomCreateRequest { Name = name }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(RoomCrudTexts.Messages.Validation.NameAlreadyExists, problem.Detail);
    }

    [Fact]
    public async Task Create_POST_WithTooLongName_ShouldReturn422()
    {
        var response = await Client.PostAsJsonAsync(BaseUrl, new RoomCreateRequest { Name = new string('a', 129) }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(RoomCrudTexts.Messages.Validation.NameMaximumLength(128), problem.Detail);
    }

    [Fact]
    public async Task List_GET_WithExistingRoom_ShouldReturn200WithRoom()
    {
        var name = UniqueName();
        var id = await CreateRoomAsync(name);

        var body = await ReadSuccessAsync<RoomListPageResponse>(await Client.GetAsync(BaseUrl, TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.LOADED, body.MessageInfo.MessageType);
        Assert.Contains(body.Response!.Rows, row => row.Id == id && row.Name == name);
    }

    [Fact]
    public async Task Info_GET_WithExistingRoom_ShouldReturn200WithRoom()
    {
        var name = UniqueName();
        var id = await CreateRoomAsync(name);

        var body = await ReadSuccessAsync<RoomInfoPageResponse>(await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(name, body.Response.Name);
    }

    [Fact]
    public async Task Info_GET_WithEmptyId_ShouldReturn400()
    {
        var response = await Client.GetAsync($"{BaseUrl}/info?id={Guid.Empty}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(RoomCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Info_GET_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/info?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(RoomCrudTexts.Messages.Validation.RoomNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Update_GET_WithExistingRoom_ShouldReturn200WithForm()
    {
        var name = UniqueName();
        var id = await CreateRoomAsync(name);

        var body = await ReadSuccessAsync<RoomUpdatePageResponse>(await Client.GetAsync($"{BaseUrl}/update?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(name, body.Response.Name);
    }

    [Fact]
    public async Task Update_GET_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/update?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(RoomCrudTexts.Messages.Validation.RoomNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Update_PUT_WithEmptyId_ShouldReturn400()
    {
        var response = await Client.PutAsJsonAsync(BaseUrl, new RoomUpdateRequest { Id = Guid.Empty, Name = UniqueName() }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(RoomCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Update_PUT_WithTooLongName_ShouldReturn422()
    {
        var id = await CreateRoomAsync(UniqueName());

        var response = await Client.PutAsJsonAsync(BaseUrl, new RoomUpdateRequest { Id = id, Name = new string('a', 129) }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(RoomCrudTexts.Messages.Validation.NameMaximumLength(128), problem.Detail);
    }

    [Fact]
    public async Task Update_PUT_WithDuplicateName_ShouldReturn422()
    {
        var otherName = UniqueName();
        await CreateRoomAsync(otherName);
        var id = await CreateRoomAsync(UniqueName());

        var response = await Client.PutAsJsonAsync(BaseUrl, new RoomUpdateRequest { Id = id, Name = otherName }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(RoomCrudTexts.Messages.Validation.NameAlreadyExists, problem.Detail);
    }

    [Fact]
    public async Task Update_PUT_WithNewName_ShouldReturn200AndRenameRoom()
    {
        var id = await CreateRoomAsync(UniqueName());
        var newName = UniqueName();

        var response = await Client.PutAsJsonAsync(BaseUrl, new RoomUpdateRequest { Id = id, Name = newName }, TestContext.Current.CancellationToken);

        var body = await ReadSuccessAsync<RoomUpdatePageResponse>(response);
        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        var info = await ReadSuccessAsync<RoomInfoPageResponse>(await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken));
        Assert.Equal(newName, info.Response!.Name);
    }

    [Fact]
    public async Task Update_PUT_WithEmptyName_ShouldReturn422()
    {
        var id = await CreateRoomAsync(UniqueName());

        var response = await Client.PutAsJsonAsync(BaseUrl, new RoomUpdateRequest { Id = id, Name = " " }, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Contains(RoomCrudTexts.Messages.Validation.NameNotEmpty, problem.Detail);
    }

    [Fact]
    public async Task Update_PUT_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.PutAsJsonAsync(BaseUrl, new RoomUpdateRequest { Id = Guid.CreateVersion7(), Name = UniqueName() }, TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_GET_WithExistingRoom_ShouldReturn200WithConfirmation()
    {
        var name = UniqueName();
        var id = await CreateRoomAsync(name);

        var body = await ReadSuccessAsync<RoomDeletePageResponse>(await Client.GetAsync($"{BaseUrl}/delete?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(id, body.Response!.Id);
        Assert.Equal(name, body.Response.Name);
    }

    [Fact]
    public async Task Delete_GET_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"{BaseUrl}/delete?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound);
        Assert.Contains(RoomCrudTexts.Messages.Validation.RoomNotFoundById, problem.Detail);
    }

    [Fact]
    public async Task Delete_DELETE_WithEmptyId_ShouldReturn400()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}?id={Guid.Empty}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(RoomCrudTexts.Messages.Validation.IdCannotBeEmpty, problem.Detail);
    }

    [Fact]
    public async Task Delete_DELETE_WithExistingRoom_ShouldReturn200AndRemoveRoom()
    {
        var id = await CreateRoomAsync(UniqueName());

        var body = await ReadSuccessAsync<RoomDeleteResponse>(await Client.DeleteAsync($"{BaseUrl}?id={id}", TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.SAVED, body.MessageInfo.MessageType);
        Assert.True(body.Response!.IsDeleted);
        var info = await Client.GetAsync($"{BaseUrl}/info?id={id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, info.StatusCode);
    }

    [Fact]
    public async Task Delete_DELETE_WithUnknownId_ShouldReturn404()
    {
        var response = await Client.DeleteAsync($"{BaseUrl}?id={Guid.CreateVersion7()}", TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }
}
