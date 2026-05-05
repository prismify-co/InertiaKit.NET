using System.Reflection;
using InertiaKit.Core.Props;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InertiaKit.AspNetCore.Internal;

internal sealed class InertiaService : IInertiaService
{
    // ── Rendering ─────────────────────────────────────────────────────────────

    public InertiaResult Render(string component) =>
        new(component, new Dictionary<string, object?>());

    public InertiaResult Render(string component, object props) =>
        new(component, PropsFromObject(props));

    public InertiaResult Render(string component, IDictionary<string, object?> props) =>
        new(component, props);

    // ── Navigation ────────────────────────────────────────────────────────────

    public InertiaLocationResult Location(string url) => new(url);

    /// <summary>
    /// Redirect back to the HTTP Referer, but ONLY when the Referer is same-origin.
    /// A cross-origin Referer is silently replaced by <paramref name="fallback"/> to
    /// prevent open-redirect attacks via a spoofed Referer header.
    /// </summary>
    public RedirectResult Back(HttpContext context, string fallback = "/")
    {
        var referer = context.Request.Headers.Referer.ToString();
        var target = IsSameOriginReferer(referer, context) ? referer : fallback;
        // The middleware 302→303 promotion converts this to a 303 See Other for non-GET origins.
        return new RedirectResult(target, permanent: false, preserveMethod: false);
    }

    public RedirectResult SeeOther(string url) =>
        new(url, permanent: false, preserveMethod: false);

    // ── Prop factories ────────────────────────────────────────────────────────

    public OptionalProp Optional(Func<object?> factory) => OptionalProp.From(factory);
    public OptionalProp Optional(Func<IServiceProvider, object?> factory) => new(factory);

    public AlwaysProp Always(object? value) => AlwaysProp.From(value);
    public AlwaysProp Always(Func<object?> factory) => AlwaysProp.From(factory);

    public DeferredProp Defer(Func<object?> factory, string group = "default") =>
        DeferredProp.From(factory, group);

    public OnceProp Once(object? value) => OnceProp.From(value);
    public OnceProp Once(Func<object?> factory) => OnceProp.From(factory);

    public MergeProp Merge(object? value, MergeStrategy strategy = MergeStrategy.Append) =>
        new(value, strategy);

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when the Referer URL is a relative path or an absolute URL
    /// whose origin matches the current request's scheme + host.
    /// </summary>
    /// <remarks>
    /// Ports are normalised before comparison: a Referer of
    /// <c>https://host:443/path</c> is treated as same-origin as a request to
    /// <c>https://host</c> because 443 is the default port for https.
    /// </remarks>
    internal static bool IsSameOriginReferer(string referer, HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(referer)) return false;

        // Relative paths (but not protocol-relative //) are always safe
        if (referer.StartsWith('/') && !referer.StartsWith("//")) return true;

        if (!Uri.TryCreate(referer, UriKind.Absolute, out var uri)) return false;

        var req = context.Request;
        if (!uri.Scheme.Equals(req.Scheme, StringComparison.OrdinalIgnoreCase)) return false;

        // Normalise ports: strip scheme-default ports so that
        // "host:443" (https) and "host" compare equal.
        var defaultPort = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
        var refererHost = uri.Host;
        var refererPort = uri.Port == defaultPort ? -1 : uri.Port;

        var requestHost = req.Host.Host;
        var requestPort = req.Host.Port.HasValue && req.Host.Port.Value != defaultPort
            ? req.Host.Port.Value
            : -1;

        return refererHost.Equals(requestHost, StringComparison.OrdinalIgnoreCase)
            && refererPort == requestPort;
    }

    private static IDictionary<string, object?> PropsFromObject(object props)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var prop in props.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            dict[prop.Name] = prop.GetValue(props);
        return dict;
    }
}
