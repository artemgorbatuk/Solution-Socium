using Datasource.Security.Ef.Models;
using Services.Security.Mapping;

namespace Tests.Units.Security;

public sealed class UserMapperTests
{
    private static User CreateUser() => new()
    {
        Id = Guid.CreateVersion7(),
        Login = "ivan",
        Email = "ivan@example.com",
        Name = "Иван",
        PasswordHash = "hash",
        IsEmailConfirmed = true,
    };

    [Fact]
    public void User_Apply_WithNewLoginAndName_ShouldSetBothAndKeepIdAndFlag()
    {
        var user = CreateUser();
        var id = user.Id;

        UserMapper.Apply(user, "petr", "petr@example.com", "Пётр");

        Assert.Equal("petr", user.Login);
        Assert.Equal("Пётр", user.Name);
        Assert.Equal(id, user.Id);
        Assert.False(user.IsDeleted);
    }

    [Fact]
    public void CreatePageResponse_Map_WithExistingUser_ShouldCopyLoginAndName()
    {
        var user = CreateUser();

        var response = UserMapper.ToCreatePageResponse(user);

        Assert.Equal(user.Login, response.Login);
        Assert.Equal(user.Name, response.Name);
    }

    [Fact]
    public void UpdatePageResponse_Map_WithExistingUser_ShouldCopyIdLoginAndName()
    {
        var user = CreateUser();

        var response = UserMapper.ToUpdatePageResponse(user);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Login, response.Login);
        Assert.Equal(user.Name, response.Name);
    }

    [Fact]
    public void InfoPageResponse_Map_WithExistingUser_ShouldCopyIdLoginAndName()
    {
        var user = CreateUser();

        var response = UserMapper.ToInfoPageResponse(user);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Login, response.Login);
        Assert.Equal(user.Name, response.Name);
    }

    [Fact]
    public void DeletePageResponse_Map_WithExistingUser_ShouldCopyIdLoginAndName()
    {
        var user = CreateUser();

        var response = UserMapper.ToDeletePageResponse(user);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Login, response.Login);
        Assert.Equal(user.Name, response.Name);
    }

    [Fact]
    public void ListModel_Map_WithExistingUser_ShouldCopyIdLoginAndName()
    {
        var user = CreateUser();

        var model = UserMapper.ToListModel(user);

        Assert.Equal(user.Id, model.Id);
        Assert.Equal(user.Login, model.Login);
        Assert.Equal(user.Name, model.Name);
        Assert.False(model.IsDeleted);
    }

    [Fact]
    public void ProfileResponse_Map_WithExistingUser_ShouldCopyIdLoginAndNameWithoutPasswordHash()
    {
        var user = CreateUser();

        var response = UserMapper.ToProfileResponse(user, canManageUsers: true);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Login, response.Login);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.Name, response.Name);
        Assert.True(response.CanManageUsers);
        Assert.DoesNotContain(typeof(Services.Security.Models.AccountProfileResponse).GetProperties(), property => property.Name == nameof(User.PasswordHash));
    }

    [Fact]
    public void QueryOptions_Map_WithIncludeDeleted_ShouldNotFilterByDeleted()
    {
        Assert.Null(UserMapper.ToQueryOptions(null, includeDeleted: true).IsDeleted);
        Assert.False(UserMapper.ToQueryOptions(null, includeDeleted: false).IsDeleted);
    }

    [Fact]
    public void AdminListModel_Map_WithOtherActiveUser_ShouldAllowGrantOnly()
    {
        var user = CreateUser();
        var currentUserId = Guid.CreateVersion7();
        HashSet<Guid> appAdmins = [currentUserId];

        var model = UserMapper.ToAdminListModel(user, appAdmins, currentUserId, canGrant: true, canRevoke: true);

        Assert.False(model.IsAppAdmin);
        Assert.True(model.CanGrantAppAdmin);
        Assert.False(model.CanRevokeAppAdmin);
        Assert.Equal(user.Email, model.Email);
        Assert.True(model.IsEmailConfirmed);
    }

    [Fact]
    public void AdminListModel_Map_WithSelfAsLastAdmin_ShouldForbidGrantAndRevoke()
    {
        var user = CreateUser();
        HashSet<Guid> appAdmins = [user.Id];

        var model = UserMapper.ToAdminListModel(user, appAdmins, user.Id, canGrant: true, canRevoke: true);

        Assert.True(model.IsAppAdmin);
        Assert.False(model.CanGrantAppAdmin);
        Assert.False(model.CanRevokeAppAdmin);
    }

    [Fact]
    public void AdminListModel_Map_WithSelfAndAnotherAdmin_ShouldAllowRevokeSelf()
    {
        var user = CreateUser();
        HashSet<Guid> appAdmins = [user.Id, Guid.CreateVersion7()];

        var model = UserMapper.ToAdminListModel(user, appAdmins, user.Id, canGrant: true, canRevoke: true);

        Assert.True(model.CanRevokeAppAdmin);
    }

    [Fact]
    public void AdminListModel_Map_WithDeletedUser_ShouldForbidGrant()
    {
        var user = CreateUser();
        user.IsDeleted = true;

        var model = UserMapper.ToAdminListModel(user, new HashSet<Guid>(), Guid.CreateVersion7(), canGrant: true, canRevoke: true);

        Assert.True(model.IsDeleted);
        Assert.False(model.CanGrantAppAdmin);
    }

    [Fact]
    public void DeletedEvent_Map_WithUserId_ShouldCopyUserId()
    {
        var userId = Guid.CreateVersion7();

        Assert.Equal(userId, UserMapper.ToDeletedEvent(userId).UserId);
    }

    [Fact]
    public void ChangedEvent_Map_WithUserIdAndName_ShouldCopyFields()
    {
        var userId = Guid.CreateVersion7();

        var domainEvent = UserMapper.ToChangedEvent(userId, "Иван Петров");

        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal("Иван Петров", domainEvent.Name);
    }

    [Fact]
    public void NotifyUserChanged_Map_WithChangedEvent_ShouldCopyFields()
    {
        var userId = Guid.CreateVersion7();
        var domainEvent = UserMapper.ToChangedEvent(userId, "Иван Петров");

        var notify = UserMapper.ToNotifyUserChanged(domainEvent);

        Assert.Equal(userId, notify.UserId);
        Assert.Equal("Иван Петров", notify.Name);
    }

    [Fact]
    public void RefreshToken_Revoke_WithReplacement_ShouldSetRevokedAtAndReplacedBy()
    {
        var token = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = Guid.CreateVersion7(),
            TokenHash = "hash",
            IsPersistent = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(14),
        };
        var revokedAt = DateTime.UtcNow;
        var replacedBy = Guid.CreateVersion7();

        RefreshTokenMapper.Revoke(token, revokedAt, replacedBy);

        Assert.Equal(revokedAt, token.RevokedAt);
        Assert.Equal(replacedBy, token.ReplacedByTokenId);
    }
}
