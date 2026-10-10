using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Services.Security.Tokens.Models;
using Services.Security.Tokens.Options;
using System.Security.Cryptography;
using System.Text;

namespace Services.Security.Tokens.Providers;

public interface IRefreshTokenProvider
{
    IssuedRefreshToken Create();
    string Hash(string token);
}

/// <summary>
/// 64 случайных байта в Base64Url; в БД — SHA-256 от строки токена.
/// </summary>
public class RefreshTokenProvider : IRefreshTokenProvider
{
    private const int TokenBytes = 64;

    private readonly JwtOptions options;
    private readonly TimeProvider timeProvider;

    public RefreshTokenProvider(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public IssuedRefreshToken Create()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenBytes));
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddDays(options.RefreshTokenDays);
        return new IssuedRefreshToken(token, Hash(token), expiresAt);
    }

    public string Hash(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
