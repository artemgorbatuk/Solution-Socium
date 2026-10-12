using System.Net;
using System.Net.Http.Json;
using Services.Security.Models;
using Services.Security.Permissions.Catalogs;
using Services.Security.Texts;
using Services.Shared.Enums;
using Services.Socium.Models;
using Services.Socium.Texts;
using static Tests.Infrastructure.ApiAssert;

namespace Tests.Integrations.Socium;

/// <summary>HTTP-контракт доступов пользователей (task-0029): коды ответов и 403 для не-админа.</summary>
[Collection(SociumApiCollection.Name)]
public sealed class UserAccessApiTests(SociumApiFixture fixture)
{
    private const string UserAdminUrl = "/api/user/admin";
    private const string AppRoleUrl = "/api/appRole";
    private const string RoleAssignmentUrl = "/api/roleAssignment";

    private HttpClient Client => fixture.Client;

    private Task<Guid> CreateUserAsync() => fixture.CreateUserAsync($"access.{Guid.NewGuid():N}", "Пользователь доступа");

    [Fact]
    public async Task AdminList_GET_WithAppAdmin_ShouldReturnUsers()
    {
        var userId = await CreateUserAsync();

        var result = await ReadSuccessAsync<UserAdminListPageResponse>(
            await Client.GetAsync(UserAdminUrl, TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Contains(result.Response!.Rows, row => row.Id == userId && !row.IsAppAdmin);
    }

    [Fact]
    public async Task AdminList_GET_WithOrdinaryUser_ShouldReturnForbidden()
    {
        using var client = fixture.CreateClientAs(await CreateUserAsync());

        var response = await client.GetAsync(UserAdminUrl, TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.Forbidden);
        Assert.Contains(UserCrudTexts.Messages.Validation.AccessDenied, problem.Detail);
    }

    [Fact]
    public async Task AppRole_POSTDELETE_WithOtherUser_ShouldGrantAndRevoke()
    {
        var userId = await CreateUserAsync();

        var created = await ReadSuccessAsync<AppRoleCreateResponse>(await Client.PostAsJsonAsync(
            AppRoleUrl,
            new AppRoleCreateRequest { UserId = userId, RoleName = RoleNames.AppAdmin },
            TestContext.Current.CancellationToken));
        var deleted = await ReadSuccessAsync<AppRoleDeleteResponse>(await Client.DeleteAsync(
            $"{AppRoleUrl}?userId={userId}&roleName={RoleNames.AppAdmin}",
            TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.SAVED, created.MessageInfo.MessageType);
        Assert.Equal(MessageType.SAVED, deleted.MessageInfo.MessageType);
    }

    [Fact]
    public async Task AppRole_POST_WithOrdinaryUser_ShouldReturnForbidden()
    {
        var targetId = await CreateUserAsync();
        using var client = fixture.CreateClientAs(await CreateUserAsync());

        var response = await client.PostAsJsonAsync(
            AppRoleUrl,
            new AppRoleCreateRequest { UserId = targetId, RoleName = RoleNames.AppAdmin },
            TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RoleAssignment_GET_WithAppAdmin_ShouldReturnTree()
    {
        var userId = await CreateUserAsync();

        var result = await ReadSuccessAsync<RoleAssignmentListPageResponse>(
            await Client.GetAsync($"{RoleAssignmentUrl}?userId={userId}", TestContext.Current.CancellationToken));

        Assert.Equal(MessageType.LOADED, result.MessageInfo.MessageType);
        Assert.Equal(userId, result.Response!.UserId);
        Assert.True(result.Response.CanManage);
    }

    [Fact]
    public async Task RoleAssignment_GET_WithOrdinaryUser_ShouldReturnForbidden()
    {
        var userId = await CreateUserAsync();
        using var client = fixture.CreateClientAs(userId);

        var response = await client.GetAsync($"{RoleAssignmentUrl}?userId={userId}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.Forbidden);
        Assert.Contains(RoleAssignmentCrudTexts.Messages.Validation.AccessDenied, problem.Detail);
    }

    [Fact]
    public async Task RoleAssignment_POST_WithAppRole_ShouldReturnBadRequest()
    {
        var userId = await CreateUserAsync();

        var response = await Client.PostAsJsonAsync(
            RoleAssignmentUrl,
            new RoleAssignmentCreateRequest { UserId = userId, RoleName = RoleNames.AppAdmin },
            TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains(RoleAssignmentCrudTexts.Messages.Validation.UnknownRole, problem.Detail);
    }
}
