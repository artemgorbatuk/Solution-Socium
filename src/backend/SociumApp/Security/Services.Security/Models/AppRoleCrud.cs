namespace Services.Security.Models;

/// <summary>Выдача роли приложения другому пользователю (<c>AppAdminGrant</c>).</summary>
public class AppRoleCreateRequest
{
    public required Guid UserId { get; set; }
    public required string RoleName { get; set; }
}

public class AppRoleCreateResponse
{
    public required Guid UserId { get; set; }
    public required string RoleName { get; set; }
}

/// <summary>Снятие роли приложения (<c>AppAdminRevoke</c>); с себя — можно, если не последний.</summary>
public class AppRoleDeleteRequest
{
    public required Guid UserId { get; set; }
    public required string RoleName { get; set; }
}

public class AppRoleDeleteResponse
{
    public required bool IsDeleted { get; set; }
}
