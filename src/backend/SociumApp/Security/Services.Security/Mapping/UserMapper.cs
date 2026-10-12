using Datasource.Security.Ef.Models;
using Repositories.Security.Ef.Options;
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
            IsDeleted = model.IsDeleted,
        };
    }

    public static UserQueryOptions ToQueryOptions(ICollection<Guid>? ids, bool includeDeleted)
    {
        return new UserQueryOptions
        {
            Ids = ids,
            IsDeleted = includeDeleted ? null : false,
        };
    }

    /// <summary>
    /// Выдать AppAdmin можно другому активному пользователю без этой роли;
    /// снять — с любого админа, пока он не последний.
    /// </summary>
    public static UserAdminListModel ToAdminListModel(
        User model,
        IReadOnlySet<Guid> appAdminUserIds,
        Guid currentUserId,
        bool canGrant,
        bool canRevoke)
    {
        var isAppAdmin = appAdminUserIds.Contains(model.Id);
        return new UserAdminListModel
        {
            Id = model.Id,
            Login = model.Login,
            Name = model.Name,
            Email = model.Email,
            IsEmailConfirmed = model.IsEmailConfirmed,
            IsDeleted = model.IsDeleted,
            IsAppAdmin = isAppAdmin,
            CanGrantAppAdmin = canGrant && !isAppAdmin && !model.IsDeleted && model.Id != currentUserId,
            CanRevokeAppAdmin = canRevoke && isAppAdmin && appAdminUserIds.Count > 1,
        };
    }

    public static UserAdminListPageResponse ToAdminListPageResponse(ICollection<UserAdminListModel> rows)
    {
        return new UserAdminListPageResponse
        {
            Rows = rows,
            RowExists = rows.Count > 0,
            RowCount = rows.Count,
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

    public static AccountProfileResponse ToProfileResponse(User model, bool canManageUsers)
    {
        return new AccountProfileResponse
        {
            Id = model.Id,
            Login = model.Login,
            Email = model.Email,
            Name = model.Name,
            CanManageUsers = canManageUsers,
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

    public static UserDeletedEvent ToDeletedEvent(Guid userId)
    {
        return new UserDeletedEvent
        {
            UserId = userId,
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
