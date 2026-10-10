using Services.Security.Models;

namespace WebApi.Controllers.Shared;

/// <summary>
/// Refresh-токен живёт только в HttpOnly-cookie, видимой лишь для <c>/api/account</c>.
/// Без «Запомнить меня» cookie сессионная — пропадает при закрытии браузера.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "socium_refresh";
    public const string Path = "/api/account";

    public static void Append(HttpResponse response, AccountTokenResponse tokens)
    {
        var options = CreateOptions();
        options.Expires = tokens.IsPersistent ? tokens.RefreshTokenExpiresAt : null;
        response.Cookies.Append(Name, tokens.RefreshToken, options);
    }

    public static string? Read(HttpRequest request)
    {
        return request.Cookies[Name];
    }

    public static void Delete(HttpResponse response)
    {
        response.Cookies.Delete(Name, CreateOptions());
    }

    private static CookieOptions CreateOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            IsEssential = true,
        };
    }
}
