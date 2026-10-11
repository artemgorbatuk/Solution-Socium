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
}
