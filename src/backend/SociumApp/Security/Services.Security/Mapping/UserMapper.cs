using Datasource.Security.Ef.Models;
using Services.Security.Events;
using Services.Security.Models;
using Services.Security.Ports;

namespace Services.Security.Mapping;

public static class UserMapper
{
    public static void Apply(User model, string login, string email, string name)
    {
        model.Login = login;
        model.Email = email;
        model.Name = name;
    }

    public static UserCreatePageResponse ToCreatePageResponse(User model)
    {
        return new UserCreatePageResponse
        {
            Email = model.Email,
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

    public static UserSearchModel ToSearchModel(User model)
    {
        return new UserSearchModel
        {
            Id = model.Id,
            Login = model.Login,
            Name = model.Name,
        };
    }

    public static UserSearchResponse ToSearchResponse(ICollection<UserSearchModel> rows)
    {
        return new UserSearchResponse
        {
            Rows = rows,
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

    public static AccountProfileResponse ToProfileResponse(User model)
    {
        return new AccountProfileResponse
        {
            Id = model.Id,
            Login = model.Login,
            Email = model.Email,
            Name = model.Name,
        };
    }

    public static UserChangedEvent ToChangedEvent(Guid userId, string name)
    {
        return new UserChangedEvent
        {
            UserId = userId,
            Name = name,
        };
    }

    public static UserNotifyChangedModel ToNotifyUserChanged(UserChangedEvent domainEvent)
    {
        return new UserNotifyChangedModel
        {
            UserId = domainEvent.UserId,
            Name = domainEvent.Name,
        };
    }
}
