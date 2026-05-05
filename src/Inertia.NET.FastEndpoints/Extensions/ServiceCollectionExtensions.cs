using Inertia.NET.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.NET.FastEndpoints.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Inertia services and configures FastEndpoints to work with Inertia.
    /// Call this alongside <c>services.AddFastEndpoints()</c>.
    /// </summary>
    public static IServiceCollection AddInertiaForFastEndpoints(
        this IServiceCollection services,
        Action<Inertia.NET.AspNetCore.InertiaOptions>? configure = null)
    {
        services.AddInertia(configure);
        return services;
    }
}
