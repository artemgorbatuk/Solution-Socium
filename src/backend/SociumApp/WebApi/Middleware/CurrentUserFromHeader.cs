using Services.Socium.Api;

namespace WebApi.Middleware;

/// <summary>
/// Временная реализация до настоящего входа: <c>Id</c> берётся из заголовка без проверки подлинности.
/// </summary>
public class CurrentUserFromHeader : ICurrentUser
{
    public const string HeaderName = "X-User-Id";

    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserFromHeader(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();
            return Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
        }
    }
}
