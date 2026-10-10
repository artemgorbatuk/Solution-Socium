using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Services.Security.Tokens.Models;
using Services.Security.Tokens.Options;
using System.Security.Claims;
using System.Text;

namespace Services.Security.Tokens.Providers;

public interface IAccessTokenProvider
{
    IssuedAccessToken Create(Guid userId);
}

/// <summary>
/// JWT HS256; идентификатор пользователя — в claim <c>sub</c>.
/// </summary>
public class AccessTokenProvider : IAccessTokenProvider
{
    private readonly JwtOptions options;
    private readonly TimeProvider timeProvider;
    private readonly SigningCredentials signingCredentials;
    private readonly JsonWebTokenHandler handler = new();

    public AccessTokenProvider(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        this.options = options.Value;
        this.timeProvider = timeProvider;
        signingCredentials = new SigningCredentials(CreateSigningKey(this.options.SigningKey), SecurityAlgorithms.HmacSha256);
    }

    public static SymmetricSecurityKey CreateSigningKey(string signingKey)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }

    public IssuedAccessToken Create(Guid userId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = signingCredentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
        };

        return new IssuedAccessToken(handler.CreateToken(descriptor), expiresAt);
    }
}
