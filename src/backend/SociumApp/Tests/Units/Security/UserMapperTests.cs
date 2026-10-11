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
    }

    [Fact]
    public void ProfileResponse_Map_WithExistingUser_ShouldCopyIdLoginAndNameWithoutPasswordHash()
    {
        var user = CreateUser();

        var response = UserMapper.ToProfileResponse(user);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Login, response.Login);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.Name, response.Name);
        Assert.DoesNotContain(typeof(Services.Security.Models.AccountProfileResponse).GetProperties(), property => property.Name == nameof(User.PasswordHash));
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
