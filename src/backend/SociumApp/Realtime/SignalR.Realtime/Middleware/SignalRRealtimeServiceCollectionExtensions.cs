using Microsoft.Extensions.DependencyInjection;
using Services.Realtime.Api;
using SignalR.Realtime.Api;

namespace SignalR.Realtime.Middleware;

public static class SignalRRealtimeServiceCollectionExtensions
{
    public static IServiceCollection AddSignalRRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IChatConnection, ChatConnection>();
        return services;
    }
}
