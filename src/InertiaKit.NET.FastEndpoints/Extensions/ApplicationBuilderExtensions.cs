using InertiaKit.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;

namespace InertiaKit.FastEndpoints.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Inertia middleware to the pipeline.
    /// </summary>
    /// <remarks>
    /// <strong>Pipeline order is critical.</strong> This call must appear:
    /// <list type="bullet">
    ///   <item>After <c>app.UseAuthentication()</c> and <c>app.UseAuthorization()</c>
    ///         so that shared props can read the authenticated user.</item>
    ///   <item>After <c>app.UseSession()</c> so that flash data and validation
    ///         errors survive the POST→redirect round-trip.</item>
    ///   <item><strong>Before</strong> <c>app.UseFastEndpoints()</c> so that
    ///         the Inertia middleware can apply request pre-checks (version/prefetch)
    ///         and normalize redirect behavior around FastEndpoints handlers.</item>
    /// </list>
    /// </remarks>
    public static IApplicationBuilder UseInertiaWithFastEndpoints(this IApplicationBuilder app) =>
        app.UseInertia();
}
