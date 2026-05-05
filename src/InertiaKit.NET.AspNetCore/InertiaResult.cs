using InertiaKit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.AspNetCore;

/// <summary>
/// Returned by <see cref="IInertiaService.Render"/> and stored in
/// <c>HttpContext.Items</c> for the middleware to process.
/// Implements both <see cref="IActionResult"/> (MVC) and <see cref="IResult"/> (MinimalAPI/FastEndpoints)
/// so it can be returned directly from any handler style.
/// </summary>
public sealed class InertiaResult(string component, IDictionary<string, object?> props)
    : IActionResult, IResult
{
    public string Component { get; } = component;
    public IDictionary<string, object?> Props { get; } = props;

    // History flags
    internal bool? EncryptHistory  { get; private set; }
    internal bool? ClearHistory    { get; private set; }
    internal bool? PreserveFragment{ get; private set; }

    // Scroll / remembered state
    internal IReadOnlyList<ScrollRegion>?          ScrollRegions   { get; private set; }
    internal IReadOnlyDictionary<string, object?>? RememberedState { get; private set; }

    // Flash data written to session before any redirect
    internal Dictionary<string, object?> Flash { get; } = [];

    public InertiaResult WithEncryptHistory(bool encrypt = true)   { EncryptHistory   = encrypt; return this; }
    public InertiaResult WithClearHistory(bool clear = true)       { ClearHistory     = clear;   return this; }
    public InertiaResult WithPreserveFragment()                    { PreserveFragment = true;    return this; }
    public InertiaResult WithFlash(string key, object? value)      { Flash[key]       = value;   return this; }

    public InertiaResult WithScrollRegions(IReadOnlyList<ScrollRegion> regions)
    {
        ScrollRegions = regions;
        return this;
    }

    public InertiaResult WithRememberedState(IReadOnlyDictionary<string, object?> state)
    {
        RememberedState = state;
        return this;
    }

    // MVC action invoker must never call this directly — the middleware owns execution.
    public Task ExecuteResultAsync(ActionContext context)
    {
        // Store for the middleware to pick up; do not throw so MVC pipelines survive.
        context.HttpContext.Items[typeof(InertiaResult)] = this;
        return Task.CompletedTask;
    }

    // IResult — Minimal API / FastEndpoints return value
    public Task ExecuteAsync(HttpContext httpContext) =>
        httpContext.RequestServices
            .GetRequiredService<Internal.InertiaResponseExecutor>()
            .ExecuteAsync(httpContext, this);
}
