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

    /// <summary>
    /// Enables or disables encrypted history for every Inertia response produced by the endpoint.
    /// Explicit <see cref="InertiaResult.WithEncryptHistory(bool)"/> calls still win per response.
    /// </summary>
    public static TBuilder WithEncryptHistory<TBuilder>(this TBuilder builder, bool encrypt = true)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder => endpointBuilder.Metadata.Add(new EncryptHistoryAttribute(encrypt)));
        return builder;
    }

    /// <summary>
    /// Enables or disables history-key rotation for every Inertia response produced by the endpoint.
    /// Use this on logout or session-expired pages so previously encrypted history entries become unreadable.
    /// Explicit <see cref="InertiaResult.WithClearHistory(bool)"/> calls still win per response.
    /// </summary>
    public static TBuilder WithClearHistory<TBuilder>(this TBuilder builder, bool clear = true)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder => endpointBuilder.Metadata.Add(new ClearHistoryAttribute(clear)));
        return builder;
    }
}
