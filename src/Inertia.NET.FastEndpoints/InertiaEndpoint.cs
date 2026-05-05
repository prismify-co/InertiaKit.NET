using FastEndpoints;
using Inertia.NET.AspNetCore;
using Inertia.NET.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.NET.FastEndpoints;

/// <summary>
/// Base endpoint for rendering Inertia pages from FastEndpoints handlers.
/// Derive from this and call <see cref="RenderAsync"/> instead of <c>SendAsync</c>.
/// Handlers should return the result of RenderAsync().
/// </summary>
public abstract class InertiaEndpoint : EndpointWithoutRequest
{
    protected IInertiaService Inertia => HttpContext.RequestServices.GetRequiredService<IInertiaService>();

    /// <summary>
    /// Render an Inertia page with the given component and optional props.
    /// This should be returned from your HandleAsync method.
    /// </summary>
    protected IResult RenderAsync(string component) =>
        RenderAsync(component, new Dictionary<string, object?>());

    /// <summary>
    /// Render an Inertia page with the given component and props.
    /// This should be returned from your HandleAsync method.
    /// </summary>
    protected IResult RenderAsync(string component, object props) =>
        RenderAsync(component, props as IDictionary<string, object?> ?? new Dictionary<string, object?> { { "props", props } });

    /// <summary>
    /// Render an Inertia page with the given component and props dictionary.
    /// This should be returned from your HandleAsync method.
    /// </summary>
    protected IResult RenderAsync(string component, IDictionary<string, object?> props)
    {
        var result = Inertia.Render(component, props);
        HttpContext.SetInertiaResult(result);
        return result;
    }
}

/// <summary>
/// Base endpoint for rendering Inertia pages from FastEndpoints handlers with a typed request.
/// Handlers should return the result of RenderAsync().
/// </summary>
public abstract class InertiaEndpoint<TRequest> : Endpoint<TRequest>
    where TRequest : notnull, new()
{
    protected IInertiaService Inertia => HttpContext.RequestServices.GetRequiredService<IInertiaService>();

    /// <summary>
    /// Render an Inertia page with the given component and optional props.
    /// This should be returned from your HandleAsync method.
    /// </summary>
    protected IResult RenderAsync(string component) =>
        RenderAsync(component, new Dictionary<string, object?>());

    /// <summary>
    /// Render an Inertia page with the given component and props.
    /// This should be returned from your HandleAsync method.
    /// </summary>
    protected IResult RenderAsync(string component, object props) =>
        RenderAsync(component, props as IDictionary<string, object?> ?? new Dictionary<string, object?> { { "props", props } });

    /// <summary>
    /// Render an Inertia page with the given component and props dictionary.
    /// This should be returned from your HandleAsync method.
    /// </summary>
    protected IResult RenderAsync(string component, IDictionary<string, object?> props)
    {
        var result = Inertia.Render(component, props);
        HttpContext.SetInertiaResult(result);
        return result;
    }
}
