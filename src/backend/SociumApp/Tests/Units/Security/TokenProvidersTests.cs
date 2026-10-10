using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Services.Security.Tokens.Options;
using Services.Security.Tokens.Providers;
using Services.Security.Tokens.Texts;
using Services.Security.Tokens.Validation;
using Tests.Infrastructure;

namespace Tests.Units.Security;

public sealed class TokenProvidersTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static JwtOptions CreateOptions(string? signingKey = null) => new()
    {
        SigningKey = signingKey ?? TestJwt.CreateSigningKey(),
    };

    [Fact]
    public async Task AccessToken_Create_WithUserId_ShouldIssueSignedTokenWithSubIssuerAudienceAndLifetime()
    {
        var options = CreateOptions();
        var provider = new AccessTokenProvider(Options.Create(options), new FixedTimeProvider(Now));
        var userId = Guid.CreateVersion7();

        var issued = provider.Create(userId);

        Assert.Equal(Now.UtcDateTime.AddMinutes(15), issued.ExpiresAt);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Token, new TokenValidationParameters
        {
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = AccessTokenProvider.CreateSigningKey(options.SigningKey),
            ValidateLifetime = false,
        });
        Assert.True(result.IsValid);
        var token = (JsonWebToken)result.SecurityToken;
        Assert.Equal(userId.ToString(), token.Subject);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Alg);
        Assert.False(string.IsNullOrEmpty(token.Id));
    }

    [Fact]
    public async Task AccessToken_Validate_WithOtherSigningKey_ShouldBeInvalid()
    {
        var provider = new AccessTokenProvider(Options.Create(CreateOptions()), new FixedTimeProvider(Now));
        var issued = provider.Create(Guid.CreateVersion7());

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = AccessTokenProvider.CreateSigningKey(TestJwt.CreateSigningKey()),
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RefreshToken_Create_WithOptions_ShouldReturnUniqueTokenItsHashAndExpiry()
    {
        var provider = new RefreshTokenProvider(Options.Create(CreateOptions()), new FixedTimeProvider(Now));

        var first = provider.Create();
        var second = provider.Create();

        Assert.NotEqual(first.Token, second.Token);
        Assert.Equal(86, first.Token.Length);
        Assert.Equal(provider.Hash(first.Token), first.TokenHash);
        Assert.NotEqual(first.Token, first.TokenHash);
        Assert.Equal(44, first.TokenHash.Length);
        Assert.Equal(Now.UtcDateTime.AddDays(14), first.ExpiresAt);
    }

    [Fact]
    public void JwtOptions_Validate_WithValidOptions_ShouldSucceed()
    {
        var result = new JwtOptionsValidator().Validate(null, CreateOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void JwtOptions_Validate_WithoutSigningKey_ShouldFailWithSigningKeyRequired(string signingKey)
    {
        var result = new JwtOptionsValidator().Validate(null, CreateOptions(signingKey));

        Assert.True(result.Failed);
        Assert.Contains(TokenTexts.Messages.Validation.SigningKeyRequired, result.Failures!);
    }

    [Fact]
    public void JwtOptions_Validate_WithShortKeyAndNonPositiveLifetimes_ShouldFailWithAllErrors()
    {
        var options = CreateOptions(new string('k', 31));
        options.AccessTokenMinutes = 0;
        options.RefreshTokenDays = -1;
        options.Issuer = "";

        var result = new JwtOptionsValidator().Validate(null, options);

        Assert.Equal(
            [
                TokenTexts.Messages.Validation.IssuerRequired,
                TokenTexts.Messages.Validation.SigningKeyTooShort,
                TokenTexts.Messages.Validation.AccessTokenMinutesInvalid,
                TokenTexts.Messages.Validation.RefreshTokenDaysInvalid,
            ],
            result.Failures!);
    }
}
