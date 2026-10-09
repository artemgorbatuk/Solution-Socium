using Datasource.Socium.Ef.Models;
using Services.Socium.Mapping;

namespace Tests.Units.Socium;

public sealed class UserMapperTests
{
    private static User CreateUser() => new()
    {
        Id = Guid.CreateVersion7(),
        Login = "ivan",
        Name = "Иван",
    };

    [Fact]
    public void User_Apply_WithNewLoginAndName_ShouldSetBothAndKeepIdAndFlag()
    {
        var user = CreateUser();
        var id = user.Id;

        UserMapper.Apply(user, "petr", "Пётр");

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
}
