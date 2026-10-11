using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Files.Options;
using Services.Files.Providers;

namespace Services.Files.Middleware;

public static class FilesServiceCollectionExtensions
{
    public static IServiceCollection AddFiles(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FilesOptions>(configuration.GetSection(FilesOptions.SectionName));
        services.AddSingleton<IFileStore, LocalFileStore>();
        return services;
    }
}
