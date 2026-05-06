using Microsoft.AspNetCore.Antiforgery;
using InertiaKit.AspNetCore.Internal;
using InertiaKit.Core.Abstractions;
using InertiaKit.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace InertiaKit.AspNetCore.Extensions;

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
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IInertiaRenderer, DefaultHtmlShellInertiaRenderer>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IInertiaRenderer, AssetShellInertiaRenderer>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IInertiaRenderer, MvcViewInertiaRenderer>());
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
    /// Registers ASP.NET Core antiforgery services with Inertia-friendly defaults.
    /// This configures the request header name expected by the Inertia browser client
    /// while leaving the normal ASP.NET Core antiforgery cookie in place.
    /// </summary>
    public static IServiceCollection AddInertiaAntiforgery(
        this IServiceCollection services,
        Action<InertiaAntiforgeryOptions>? configure = null)
    {
        services.AddOptions<InertiaAntiforgeryOptions>();
        if (configure is not null)
            services.Configure(configure);

        var inertiaOptions = new InertiaAntiforgeryOptions();
        configure?.Invoke(inertiaOptions);

        services.AddAntiforgery(options =>
        {
            options.HeaderName = inertiaOptions.HeaderName;
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

    /// <summary>
    /// Registers a custom <see cref="IInertiaRenderer"/> used for the initial HTML shell.
    /// Custom renderers take precedence over the built-in MVC and default HTML-shell renderers.
    /// </summary>
    public static IServiceCollection AddInertiaRenderer<T>(this IServiceCollection services)
        where T : class, IInertiaRenderer
    {
        services.AddScoped<IInertiaRenderer, T>();
        return services;
    }
}
