using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Messaging.Options;
using Services.Messaging.Providers;

namespace Services.Messaging.Middleware;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));
        services.AddSingleton<IMessageChannel, SmtpEmailChannel>();
        services.AddSingleton<IMessageContext, MessageContext>();
        return services;
    }

    /// <summary>
    /// Тесты: SMTP не используется, письма копятся в <see cref="CapturingMessageChannel"/>.
    /// </summary>
    public static IServiceCollection AddMessagingCapture(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));
        services.AddSingleton<CapturingMessageChannel>();
        services.AddSingleton<IMessageChannel>(provider => provider.GetRequiredService<CapturingMessageChannel>());
        services.AddSingleton<IMessageContext, MessageContext>();
        return services;
    }
}
