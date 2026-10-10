using Microsoft.IdentityModel.JsonWebTokens;
using Services.Shared.Api;
using System.Security.Claims;

namespace WebApi.Middleware;

/// <summary>
/// <c>Id</c> из claim <c>sub</c> проверенного access-токена.
/// </summary>
public class CurrentUserFromClaims : ICurrentUser
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserFromClaims(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var id) && id != Guid.Empty ? id : null;
        }
    }
}
