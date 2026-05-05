using InertiaKit.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.FastEndpoints.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Inertia services and configures FastEndpoints to work with Inertia.
    /// Call this alongside <c>services.AddFastEndpoints()</c>.
    /// </summary>
    public static IServiceCollection AddInertiaForFastEndpoints(
        this IServiceCollection services,
        Action<InertiaKit.AspNetCore.InertiaOptions>? configure = null)
    {
        services.AddInertia(configure);
        return services;
    }
}
