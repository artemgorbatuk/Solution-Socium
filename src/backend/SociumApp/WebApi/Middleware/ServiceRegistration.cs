using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Repositories.Socium.Ef.Api;
using Services.Socium.Api;

namespace WebApi.Middleware;

public static class ServiceRegistration
{
    public static IServiceCollection AddDependencyInjectionExt(this IServiceCollection services)
    {
        // Репозитории, которые работают с DbContext, создаются в UoW - в DI не требуются.
        services.AddScoped<IUnitOfWorkSocium, UnitOfWorkSocium>();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserFromHeader>();

        services.AddScoped<IServiceRoom, ServiceRoom>();
        services.AddScoped<IServiceChat, ServiceChat>();
        services.AddScoped<IServiceMessage, ServiceMessage>();
        services.AddScoped<IServiceUser, ServiceUser>();
        services.AddScoped<IServiceParticipant, ServiceParticipant>();

        return services;
    }

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

        return services;
    }
}
