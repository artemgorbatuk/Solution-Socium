using System.Text.Json.Serialization;

namespace Services.Security.Models;

public class AccountLoginRequest
{
    public required string Login { get; set; }
    public required string Password { get; set; }
    public bool RememberMe { get; set; }
}

/// <summary>
/// Ответ входа и обновления токенов. Refresh-токен клиенту в теле не отдаётся:
/// контроллер кладёт его в HttpOnly-cookie, поэтому свойства refresh-токена исключены из JSON.
/// Они без <c>required</c>: System.Text.Json не допускает обязательное свойство, которое не сериализуется.
/// </summary>
public class AccountTokenResponse
{
    public required string AccessToken { get; set; }
    public required DateTime AccessTokenExpiresAt { get; set; }
    public required AccountProfileResponse User { get; set; }

    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTime RefreshTokenExpiresAt { get; set; }

    [JsonIgnore]
    public bool IsPersistent { get; set; }
}

/// <summary>
/// Токен берётся из cookie — заполняет контроллер.
/// </summary>
public class AccountRefreshRequest
{
    public string? RefreshToken { get; set; }
}

/// <summary>
/// Токен берётся из cookie — заполняет контроллер.
/// </summary>
public class AccountLogoutRequest
{
    public string? RefreshToken { get; set; }
}

public class AccountLogoutResponse
{
    public required bool IsLoggedOut { get; set; }
}

public class AccountProfilePageRequest
{
}

public class AccountProfileResponse
{
    public required Guid Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
}

public class AccountProfileUpdateRequest
{
    public required string Name { get; set; }
}
