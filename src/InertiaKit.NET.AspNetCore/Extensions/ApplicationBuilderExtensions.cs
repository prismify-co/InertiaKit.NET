using Microsoft.AspNetCore.Builder;

namespace InertiaKit.AspNetCore.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Inertia middleware to the pipeline. Must be placed after
    /// authentication/session middleware and before routing.
    /// </summary>
    public static IApplicationBuilder UseInertia(this IApplicationBuilder app) =>
        app.UseMiddleware<InertiaMiddleware>();
}
