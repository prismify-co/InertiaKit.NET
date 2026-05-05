using Inertia.NET.AspNetCore.Internal;
using Inertia.NET.Core.Abstractions;
using Inertia.NET.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Inertia.NET.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInertia(
        this IServiceCollection services,
        Action<InertiaOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<InertiaOptions>(_ => { });

        services.AddLogging();
        services.AddSingleton<IInertiaSerializer, SystemTextJsonInertiaSerializer>();
        services.AddScoped<IInertiaService, InertiaService>();
        services.AddScoped<InertiaResponseExecutor>();
        // Register PropResolver as scoped so DI injects a typed ILogger<PropResolver>
        services.AddScoped<PropResolver>();

        // SSR HttpClient — base address is resolved at first use from InertiaOptions
        services.AddHttpClient<SsrGateway>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<InertiaOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.SsrUrl))
                client.BaseAddress = new Uri(options.SsrUrl);
        });

        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="HandleInertiaRequestsBase"/> implementation
    /// for shared props and versioning.
    /// </summary>
    public static IServiceCollection AddInertiaHandler<T>(this IServiceCollection services)
        where T : HandleInertiaRequestsBase
    {
        services.AddScoped<HandleInertiaRequestsBase, T>();
        return services;
    }
}
