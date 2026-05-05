using FastEndpoints;
using Inertia.NET.AspNetCore;
using Inertia.NET.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.NET.FastEndpoints;

/// <summary>
/// Base endpoint for rendering Inertia pages from FastEndpoints handlers.
/// Derive from this and call <see cref="RenderAsync"/> instead of <c>SendAsync</c>.
/// </summary>
public abstract class InertiaEndpoint : EndpointWithoutRequest
{
    protected IInertiaService Inertia => HttpContext.RequestServices.GetRequiredService<IInertiaService>();

    protected async Task RenderAsync(string component, CancellationToken ct = default) =>
        await RenderAsync(component, new Dictionary<string, object?>(), ct);

    protected async Task RenderAsync(string component, object props, CancellationToken ct = default) =>
        await RenderCoreAsync(Inertia.Render(component, props), ct);

    protected async Task RenderAsync(string component, IDictionary<string, object?> props, CancellationToken ct = default) =>
        await RenderCoreAsync(Inertia.Render(component, props), ct);

    private async Task RenderCoreAsync(InertiaResult result, CancellationToken ct)
    {
        HttpContext.SetInertiaResult(result);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Base endpoint for rendering Inertia pages from FastEndpoints handlers with a typed request.
/// </summary>
public abstract class InertiaEndpoint<TRequest> : Endpoint<TRequest>
    where TRequest : notnull, new()
{
    protected IInertiaService Inertia => HttpContext.RequestServices.GetRequiredService<IInertiaService>();

    protected async Task RenderAsync(string component, CancellationToken ct = default) =>
        await RenderAsync(component, new Dictionary<string, object?>(), ct);

    protected async Task RenderAsync(string component, object props, CancellationToken ct = default) =>
        await RenderCoreAsync(Inertia.Render(component, props), ct);

    protected async Task RenderAsync(string component, IDictionary<string, object?> props, CancellationToken ct = default) =>
        await RenderCoreAsync(Inertia.Render(component, props), ct);

    private async Task RenderCoreAsync(InertiaResult result, CancellationToken ct)
    {
        HttpContext.SetInertiaResult(result);
        await Task.CompletedTask;
    }
}
