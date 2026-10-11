using Microsoft.Extensions.DependencyInjection;
using Services.Realtime.Api;

namespace Services.Realtime.Middleware;

public static class RealtimeServiceCollectionExtensions
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddScoped<IServiceChatNotify, ServiceChatNotify>();
        return services;
    }
}
