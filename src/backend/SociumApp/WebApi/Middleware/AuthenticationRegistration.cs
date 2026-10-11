using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Services.Security.Tokens.Options;
using Services.Security.Tokens.Providers;

namespace WebApi.Middleware;

public static class AuthenticationRegistration
{
    public const string AuthRateLimitPolicy = "auth";
    public const string MailRateLimitPolicy = "mail";
    public const string UnauthorizedDetail = "Требуется вход в систему.";

    /// <summary>
    /// JWT Bearer; по умолчанию любой endpoint требует входа — анонимные помечены <see cref="AllowAnonymousAttribute"/>.
    /// </summary>
    public static IServiceCollection AddAuthenticationExt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = AccessTokenProvider.CreateSigningKey(jwt.SigningKey),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                };
                bearer.Events = new JwtBearerEvents
                {
                    OnChallenge = WriteUnauthorizedProblemAsync,
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        services.Configure<AuthRateLimitOptions>(configuration.GetSection(AuthRateLimitOptions.SectionName));
        services.Configure<MailRateLimitOptions>(configuration.GetSection(MailRateLimitOptions.SectionName));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
            {
                var rateLimit = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimit.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimit.WindowSeconds),
                        QueueLimit = 0,
                    });
            });
            options.AddPolicy(MailRateLimitPolicy, httpContext =>
            {
                var rateLimit = httpContext.RequestServices.GetRequiredService<IOptions<MailRateLimitOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimit.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimit.WindowSeconds),
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// 401 без токена или с недействительным токеном — тоже Problem Details, как ответы сервисов.
    /// </summary>
    private static async Task WriteUnauthorizedProblemAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;

        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = UnauthorizedDetail,
            },
        });
    }
}
