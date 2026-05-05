using Inertia.NET.Core.Props;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inertia.NET.AspNetCore;

/// <summary>
/// Controller-facing Inertia API. Inject via DI and call from action methods.
/// </summary>
public interface IInertiaService
{
    // ── Page rendering ────────────────────────────────────────────────────────

    InertiaResult Render(string component);
    InertiaResult Render(string component, object props);
    InertiaResult Render(string component, IDictionary<string, object?> props);

    // ── Navigation ────────────────────────────────────────────────────────────

    /// <summary>
    /// External redirect via 409 + X-Inertia-Location. Use when navigating outside
    /// the Inertia app (OAuth providers, payment gateways, etc.).
    /// </summary>
    InertiaLocationResult Location(string url);

    /// <summary>
    /// Redirect back to the HTTP Referer header, or to <paramref name="fallback"/>
    /// when no Referer is present. Returns 303 See Other.
    /// </summary>
    RedirectResult Back(HttpContext context, string fallback = "/");

    /// <summary>Explicit 303 See Other redirect to <paramref name="url"/>.</summary>
    RedirectResult SeeOther(string url);

    // ── Prop type factories ───────────────────────────────────────────────────

    OptionalProp Optional(Func<object?> factory);
    OptionalProp Optional(Func<IServiceProvider, object?> factory);

    AlwaysProp Always(object? value);
    AlwaysProp Always(Func<object?> factory);

    DeferredProp Defer(Func<object?> factory, string group = "default");

    OnceProp Once(object? value);
    OnceProp Once(Func<object?> factory);

    MergeProp Merge(object? value, MergeStrategy strategy = MergeStrategy.Append);
}
