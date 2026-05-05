using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.AspNetCore.Extensions;

public static class EndpointExtensions
{
    /// <summary>
    /// Maps a Minimal API GET endpoint that renders an Inertia page.
    /// The <paramref name="propsFactory"/> receives a typed dictionary, which gives
    /// full control over prop types (including <see cref="IInertiaService"/> wrappers).
    /// </summary>
    public static IEndpointConventionBuilder MapInertia(
        this IEndpointRouteBuilder app,
        string pattern,
        string component,
        Func<HttpContext, IDictionary<string, object?>>? propsFactory = null)
    {
        return app.MapGet(pattern, async (HttpContext ctx) =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            var props = propsFactory?.Invoke(ctx) ?? new Dictionary<string, object?>();
            ctx.SetInertiaResult(inertia.Render(component, props));
            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Maps a Minimal API GET endpoint that renders an Inertia page using an
    /// anonymous object or POCO for props — mirrors <see cref="IInertiaService.Render(string,object)"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// app.MapInertia("/users", "Users/Index",
    ///     ctx => new { users = db.Users.ToList(), total = db.Users.Count() });
    /// </code>
    /// </example>
    public static IEndpointConventionBuilder MapInertia(
        this IEndpointRouteBuilder app,
        string pattern,
        string component,
        Func<HttpContext, object> propsFactory)
    {
        return app.MapGet(pattern, async (HttpContext ctx) =>
        {
            var inertia = ctx.RequestServices.GetRequiredService<IInertiaService>();
            ctx.SetInertiaResult(inertia.Render(component, propsFactory(ctx)));
            await Task.CompletedTask;
        });
    }
}
