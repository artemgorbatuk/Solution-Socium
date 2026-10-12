namespace Services.Security.Models;

public class UserCreatePageRequest
{
}

/// <summary>
/// Регистрация: создать пользователя может любой, в том числе не вошедший.
/// </summary>
public class UserCreateRequest
{
    public required string Email { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
    public required string Password { get; set; }
}

public class UserCreatePageResponse
{
    public required string Email { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
}

public class UserUpdatePageRequest
{
    public required Guid Id { get; set; }
}

public class UserUpdatePageResponse
{
    public required Guid Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
}

public class UserUpdateRequest
{
    public required Guid Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
}

public class UserDeletePageRequest
{
    public required Guid Id { get; set; }
}

public class UserDeletePageResponse
{
    public required Guid Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
}

public class UserDeleteRequest
{
    public required Guid Id { get; set; }
}

public class UserDeleteResponse
{
    public required bool IsDeleted { get; set; }
}

public class UserInfoPageRequest
{
    public required Guid Id { get; set; }
}

public class UserInfoPageResponse
{
    public required Guid Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
}

public class UserListPageRequest
{
    /// <summary>Только пользователи с этими идентификаторами; <c>null</c> — все.</summary>
    public ICollection<Guid>? Ids { get; set; }
    /// <summary>Вместе с удалёнными — например, имя отправителя старого сообщения.</summary>
    public bool IncludeDeleted { get; set; }
}

public class UserListPageResponse
{
    public required bool RowExists { get; set; }
    public required int RowCount { get; set; }
    public required ICollection<UserListModel> Rows { get; set; } = [];
}

public class UserListModel
{
    public required Guid Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
    public required bool IsDeleted { get; set; }
}

/// <summary>Список всех пользователей для админа приложения (<c>UserRead</c>).</summary>
public class UserAdminListPageRequest
{
    /// <summary>Только пользователи с этими идентификаторами; <c>null</c> — все.</summary>
    public ICollection<Guid>? Ids { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class UserAdminListPageResponse
{
    public required bool RowExists { get; set; }
    public required int RowCount { get; set; }
    public required ICollection<UserAdminListModel> Rows { get; set; } = [];
}

public class UserAdminListModel
{
    public required Guid Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required bool IsEmailConfirmed { get; set; }
    public required bool IsDeleted { get; set; }
    public required bool IsAppAdmin { get; set; }
    public required bool CanGrantAppAdmin { get; set; }
    public required bool CanRevokeAppAdmin { get; set; }
}

public class UserSearchRequest
{
    public required string Login { get; set; }
}

public class UserSearchResponse
{
    public required ICollection<UserSearchModel> Rows { get; set; } = [];
}

public class UserSearchModel
{
    public required Guid Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
}
