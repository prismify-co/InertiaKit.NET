using FastEndpoints;
using InertiaKit.AspNetCore;
using InertiaKit.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.FastEndpoints;

/// <summary>
/// Base endpoint for rendering Inertia pages from FastEndpoints handlers.
/// Derive from this and call <see cref="RenderAsync"/> from <c>HandleAsync()</c>.
/// </summary>
public abstract class InertiaEndpoint : EndpointWithoutRequest
{
    protected IInertiaService Inertia => HttpContext.RequestServices.GetRequiredService<IInertiaService>();

    /// <summary>
    /// Render an Inertia page with the given component and optional props.
    /// </summary>
    protected Task RenderAsync(string component, CancellationToken ct = default) =>
        RenderAsync(component, new Dictionary<string, object?>(), ct);

    /// <summary>
    /// Render an Inertia page with the given component and props.
    /// </summary>
    protected async Task RenderAsync(string component, object props, CancellationToken ct = default) =>
        await Send.ResultAsync(Inertia.Render(component, props));

    /// <summary>
    /// Render an Inertia page with the given component and props dictionary.
    /// </summary>
    protected async Task RenderAsync(string component, IDictionary<string, object?> props, CancellationToken ct = default)
    {
        await Send.ResultAsync(Inertia.Render(component, props));
    }

    /// <summary>
    /// Applies encrypted-history metadata to the endpoint so every Inertia response
    /// from this route defaults to the requested setting.
    /// </summary>
    protected void WithEncryptHistory(bool encrypt = true) =>
        Description(builder => builder.WithEncryptHistory(encrypt));

    /// <summary>
    /// Applies clear-history metadata to the endpoint so logout or session-expired
    /// pages can rotate the client's history key automatically.
    /// </summary>
    protected void WithClearHistory(bool clear = true) =>
        Description(builder => builder.WithClearHistory(clear));

    /// <summary>
    /// Send a 303 redirect without falling back to FastEndpoints' auto-204 response.
    /// </summary>
    protected async Task SeeOtherAsync(string location, CancellationToken ct = default)
    {
        HttpContext.Response.StatusCode = StatusCodes.Status303SeeOther;
        HttpContext.Response.Headers.Location = location;
        await HttpContext.Response.StartAsync(ct);
    }
}

/// <summary>
/// Base endpoint for rendering Inertia pages from FastEndpoints handlers with a typed request.
/// Derive from this and call <see cref="RenderAsync"/> from <c>HandleAsync()</c>.
/// </summary>
public abstract class InertiaEndpoint<TRequest> : Endpoint<TRequest>
    where TRequest : notnull, new()
{
    protected IInertiaService Inertia => HttpContext.RequestServices.GetRequiredService<IInertiaService>();

    /// <summary>
    /// Render an Inertia page with the given component and optional props.
    /// </summary>
    protected Task RenderAsync(string component, CancellationToken ct = default) =>
        RenderAsync(component, new Dictionary<string, object?>(), ct);

    /// <summary>
    /// Render an Inertia page with the given component and props.
    /// </summary>
    protected async Task RenderAsync(string component, object props, CancellationToken ct = default) =>
        await Send.ResultAsync(Inertia.Render(component, props));

    /// <summary>
    /// Render an Inertia page with the given component and props dictionary.
    /// </summary>
    protected async Task RenderAsync(string component, IDictionary<string, object?> props, CancellationToken ct = default)
    {
        await Send.ResultAsync(Inertia.Render(component, props));
    }

    /// <summary>
    /// Applies encrypted-history metadata to the endpoint so every Inertia response
    /// from this route defaults to the requested setting.
    /// </summary>
    protected void WithEncryptHistory(bool encrypt = true) =>
        Description(builder => builder.WithEncryptHistory(encrypt));

    /// <summary>
    /// Applies clear-history metadata to the endpoint so logout or session-expired
    /// pages can rotate the client's history key automatically.
    /// </summary>
    protected void WithClearHistory(bool clear = true) =>
        Description(builder => builder.WithClearHistory(clear));

    /// <summary>
    /// Send a 303 redirect without falling back to FastEndpoints' auto-204 response.
    /// </summary>
    protected async Task SeeOtherAsync(string location, CancellationToken ct = default)
    {
        HttpContext.Response.StatusCode = StatusCodes.Status303SeeOther;
        HttpContext.Response.Headers.Location = location;
        await HttpContext.Response.StartAsync(ct);
    }
}
