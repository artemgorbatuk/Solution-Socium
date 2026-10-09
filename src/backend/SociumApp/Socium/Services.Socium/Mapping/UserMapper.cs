using Datasource.Socium.Ef.Models;
using Services.Socium.Models;

namespace Services.Socium.Mapping;

public static class UserMapper
{
    public static void Apply(User model, string login, string name)
    {
        model.Login = login;
        model.Name = name;
    }

    public static UserCreatePageResponse ToCreatePageResponse(User model)
    {
        return new UserCreatePageResponse
        {
            Login = model.Login,
            Name = model.Name,
        };
    }

    public static UserUpdatePageResponse ToUpdatePageResponse(User model)
    {
        return new UserUpdatePageResponse
        {
            Id = model.Id,
            Login = model.Login,
            Name = model.Name,
        };
    }

    public static UserInfoPageResponse ToInfoPageResponse(User model)
    {
        return new UserInfoPageResponse
        {
            Id = model.Id,
            Login = model.Login,
            Name = model.Name,
        };
    }

    public static UserListModel ToListModel(User model)
    {
        return new UserListModel
        {
            Id = model.Id,
            Login = model.Login,
            Name = model.Name,
        };
    }

    public static UserDeletePageResponse ToDeletePageResponse(User model)
    {
        return new UserDeletePageResponse
        {
            Id = model.Id,
            Login = model.Login,
            Name = model.Name,
        };
    }
}
