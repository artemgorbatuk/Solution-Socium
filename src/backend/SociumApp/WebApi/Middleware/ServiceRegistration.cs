using Datasource.Security.Ef.Contexts;
using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repositories.Security.Ef.Api;
using Repositories.Socium.Ef.Api;
using Services.Security.Api;
using Services.Security.Passwords.Providers;
using Services.Security.Tokens.Options;
using Services.Security.Tokens.Providers;
using Services.Security.Tokens.Validation;
using Services.Shared.Api;
using Services.Socium.Api;
using Services.Socium.Ports;
using WebApi.Ports;

namespace WebApi.Middleware;

public static class ServiceRegistration
{
    public static IServiceCollection AddDependencyInjectionExt(this IServiceCollection services, IConfiguration configuration)
    {
        // Репозитории, которые работают с DbContext, создаются в UoW - в DI не требуются.
        services.AddScoped<IUnitOfWorkSocium, UnitOfWorkSocium>();
        services.AddScoped<IUnitOfWorkSecurity, UnitOfWorkSecurity>();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserFromClaims>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IAccessTokenProvider, AccessTokenProvider>();
        services.AddSingleton<IRefreshTokenProvider, RefreshTokenProvider>();
        services.AddSingleton<IPasswordHasher, AspNetIdentityPasswordHasher>();

        services.AddScoped<IServiceUser, ServiceUser>();
        services.AddScoped<IServiceAccount, ServiceAccount>();

        services.AddScoped<IUserPort, UserPort>();
        services.AddScoped<IServiceRoom, ServiceRoom>();
        services.AddScoped<IServiceChat, ServiceChat>();
        services.AddScoped<IServiceMessage, ServiceMessage>();
        services.AddScoped<IServiceParticipant, ServiceParticipant>();

        return services;
    }

    /// <summary>
    /// Таблицы обеих областей лежат в одной БД Socium, но у каждой области свой контекст и своя таблица истории миграций.
    /// </summary>
    public static IServiceCollection AddDbContextExt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<DbContextSocium>(options =>
        {
            if (!options.IsConfigured)
            {
                var connectionString = configuration.GetConnectionString("Socium") ?? default!;

                options.EnableSensitiveDataLogging(false);
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Socium"));
            }
        });

        services.AddScoped(provider =>
            provider.GetRequiredService<IDbContextFactory<DbContextSocium>>().CreateDbContext());

        services.AddDbContextFactory<DbContextSecurity>(options =>
        {
            if (!options.IsConfigured)
            {
                var connectionString = configuration.GetConnectionString("Socium") ?? default!;

                options.EnableSensitiveDataLogging(false);
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Security"));
            }
        });

        services.AddScoped(provider =>
            provider.GetRequiredService<IDbContextFactory<DbContextSecurity>>().CreateDbContext());

        return services;
    }
}
