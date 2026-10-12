using Datasource.Files.Ef.Contexts;
using Datasource.Messaging.Ef.Contexts;
using Datasource.Security.Ef.Contexts;
using Datasource.Socium.Ef.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repositories.Files.Ef.Api;
using Repositories.Messaging.Ef.Api;
using Repositories.Security.Ef.Api;
using Repositories.Socium.Ef.Api;
using Services.Files.Stored.Api;
using Services.Files.Stored.Ports;
using Services.Files.Middleware;
using Services.Messaging.Middleware;
using Services.Messaging.Tokens.Api;
using Services.Messaging.Tokens.Providers;
using Services.Realtime.Api;
using Services.Realtime.Middleware;
using Services.Security.Api;
using Services.Security.Passwords.Providers;
using Services.Security.Permissions.Providers;
using Services.Security.Ports;
using Services.Security.Tokens.Options;
using Services.Security.Tokens.Providers;
using Services.Security.Tokens.Validation;
using Services.Shared.Api;
using Services.Shared.Events;
using Services.Socium.Api;
using Services.Socium.Events;
using Services.Socium.Policies;
using Services.Socium.Ports;
using Services.Socium.Providers;
using SignalR.Realtime.Middleware;
using WebApi.Ports;

namespace WebApi.Middleware;

public static class ServiceRegistration
{
    public static IServiceCollection AddDependencyInjectionExt(this IServiceCollection services, IConfiguration configuration, bool captureMail = false)
    {
        // Репозитории, которые работают с DbContext, создаются в UoW - в DI не требуются.
        services.AddScoped<IUnitOfWorkSocium, UnitOfWorkSocium>();
        services.AddScoped<IUnitOfWorkSecurity, UnitOfWorkSecurity>();
        services.AddScoped<IUnitOfWorkMessaging, UnitOfWorkMessaging>();
        services.AddScoped<IUnitOfWorkFiles, UnitOfWorkFiles>();
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
        services.AddSingleton<IPermissionProvider, PermissionProvider>();
        services.AddSingleton<IEmailTokenHasher, EmailTokenHasher>();

        if (captureMail)
        {
            services.AddMessagingCapture(configuration);
        }
        else
        {
            services.AddMessaging(configuration);
        }

        services.AddFiles(configuration);
        services.AddSignalRRealtime();
        services.AddRealtime();

        services.AddScoped<IServiceEmailToken, ServiceEmailToken>();
        services.AddScoped<IEmailPort, EmailPort>();
        services.AddScoped<IServiceStoredFile, ServiceStoredFile>();
        services.AddScoped<IFileOwnerAccess, MessageFileOwnerAccess>();
        services.AddScoped<IFilePort, FilePort>();
        services.AddScoped<IChatMembership, ChatMembershipPort>();
        services.AddScoped<IChatNotifyPort, ChatNotifyPort>();

        services.AddScoped<IServiceUser, ServiceUser>();
        services.AddScoped<IServiceAccount, ServiceAccount>();
        services.AddScoped<IServiceAppRole, ServiceAppRole>();

        services.AddScoped<IUserPort, UserPort>();
        services.AddScoped<IUserRolePort, UserRolePort>();
        services.AddScoped<IRoleAssignmentProvider, RoleAssignmentProvider>();
        services.AddScoped<IRoomPolicy, RoomPolicy>();
        services.AddScoped<IChatPolicy, ChatPolicy>();
        services.AddScoped<IMessagePolicy, MessagePolicy>();
        services.AddScoped<IParticipantPolicy, ParticipantPolicy>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<IDomainEventHandler<RoomCreatedEvent>, AssignRoomAdminOnRoomCreatedHandler>();
        services.AddScoped<IDomainEventHandler<RoomCreatedEvent>, CreateDefaultChatOnRoomCreatedHandler>();
        services.AddScoped<IDomainEventHandler<RoomChangedEvent>, NotifyRoomChangedOnRoomChangedHandler>();
        services.AddScoped<IServiceRoom, ServiceRoom>();
        services.AddScoped<IServiceChat, ServiceChat>();
        services.AddScoped<IServiceMessage, ServiceMessage>();
        services.AddScoped<IServiceParticipant, ServiceParticipant>();

        return services;
    }

    /// <summary>
    /// Таблицы областей лежат в одной БД Socium, у каждой — свой контекст и своя таблица истории миграций.
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

        services.AddDbContextFactory<DbContextMessaging>(options =>
        {
            if (!options.IsConfigured)
            {
                var connectionString = configuration.GetConnectionString("Socium") ?? default!;

                options.EnableSensitiveDataLogging(false);
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Messaging"));
            }
        });

        services.AddScoped(provider =>
            provider.GetRequiredService<IDbContextFactory<DbContextMessaging>>().CreateDbContext());

        services.AddDbContextFactory<DbContextFiles>(options =>
        {
            if (!options.IsConfigured)
            {
                var connectionString = configuration.GetConnectionString("Socium") ?? default!;

                options.EnableSensitiveDataLogging(false);
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Files"));
            }
        });

        services.AddScoped(provider =>
            provider.GetRequiredService<IDbContextFactory<DbContextFiles>>().CreateDbContext());

        return services;
    }
}
