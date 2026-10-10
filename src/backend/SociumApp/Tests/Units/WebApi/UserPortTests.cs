using Services.Security.Api;
using Services.Security.Models;
using Services.Shared.Enums;
using Services.Shared.Models;
using WebApi.Ports;

namespace Tests.Units.WebApi;

public sealed class UserPortTests
{
    private sealed class FakeServiceUser : IServiceUser
    {
        public ResponseInfo<UserInfoPageResponse> Info { get; set; } = ResponseInfo.NotFound<UserInfoPageResponse>(MessageType.NOT_FOUND);
        public ResponseInfo<UserListPageResponse> List { get; set; } = ResponseInfo.Success<UserListPageResponse>(MessageType.LOADED);
        public int ListCalls { get; private set; }
        public UserListPageRequest? LastListRequest { get; private set; }

        public Task<ResponseInfo<UserInfoPageResponse>> DisplayInfoPageAsync(UserInfoPageRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Info);

        public Task<ResponseInfo<UserListPageResponse>> DisplayListPageAsync(UserListPageRequest request, CancellationToken cancellationToken = default)
        {
            ListCalls++;
            LastListRequest = request;
            return Task.FromResult(List);
        }

        public Task<ResponseInfo<UserCreatePageResponse>> DisplayCreatePageAsync(UserCreatePageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ResponseInfo<UserCreatePageResponse>> CreateAsync(UserCreateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ResponseInfo<UserUpdatePageResponse>> DisplayUpdatePageAsync(UserUpdatePageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ResponseInfo<UserUpdatePageResponse>> UpdateAsync(UserUpdateRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ResponseInfo<UserDeletePageResponse>> DisplayDeletePageAsync(UserDeletePageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ResponseInfo<UserDeleteResponse>> DeleteAsync(UserDeleteRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task GetSingle_Load_WithFoundUser_ShouldMapUser()
    {
        var id = Guid.CreateVersion7();
        var service = new FakeServiceUser { Info = ResponseInfo.Success(new UserInfoPageResponse { Id = id, Login = "ivan", Name = "Иван" }) };

        var user = await new UserPort(service).GetSingleOrDefaultAsync(id, TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Equal(id, user.Id);
        Assert.Equal("ivan", user.Login);
        Assert.Equal("Иван", user.Name);
    }

    [Fact]
    public async Task GetSingle_Load_WithNotFoundOrUnauthorized_ShouldReturnNull()
    {
        var notFound = new FakeServiceUser { Info = ResponseInfo.NotFound<UserInfoPageResponse>(MessageType.NOT_FOUND) };
        var unauthorized = new FakeServiceUser { Info = ResponseInfo.Unauthorized<UserInfoPageResponse>(MessageType.UNAUTHORIZED) };

        Assert.Null(await new UserPort(notFound).GetSingleOrDefaultAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken));
        Assert.Null(await new UserPort(unauthorized).GetSingleOrDefaultAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetSingle_Load_WithServiceError_ShouldThrow()
    {
        var service = new FakeServiceUser { Info = ResponseInfo.Error<UserInfoPageResponse>(MessageType.ERROR, "сбой") };

        await Assert.ThrowsAsync<InvalidOperationException>(() => new UserPort(service).GetSingleOrDefaultAsync(Guid.CreateVersion7(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetList_Load_WithNoIds_ShouldNotCallService()
    {
        var service = new FakeServiceUser();

        var users = await new UserPort(service).GetListAsync([], TestContext.Current.CancellationToken);

        Assert.Empty(users);
        Assert.Equal(0, service.ListCalls);
    }

    [Fact]
    public async Task GetList_Load_WithDuplicateIds_ShouldRequestDistinctIdsAndMapByUserId()
    {
        var id = Guid.CreateVersion7();
        var service = new FakeServiceUser
        {
            List = ResponseInfo.Success(new UserListPageResponse
            {
                RowExists = true,
                RowCount = 1,
                Rows = [new UserListModel { Id = id, Login = "ivan", Name = "Иван" }],
            }),
        };

        var users = await new UserPort(service).GetListAsync([id, id], TestContext.Current.CancellationToken);

        Assert.Equal([id], service.LastListRequest!.Ids!);
        Assert.Equal("Иван", users[id].Name);
        Assert.Equal(MessageType.LOADED, service.List.MessageInfo.MessageType);
    }
}
