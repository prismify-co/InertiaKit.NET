using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InertiaKit.AspNetCore.Internal;
using InertiaKit.Core;
using InertiaKit.Core.Abstractions;
using InertiaKit.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InertiaKit.AspNetCore;

public sealed class InertiaMiddleware(RequestDelegate next)
{
    // Namespaced session keys — the __ prefix and colon separator reduce collision
    // risk with user-managed session keys while remaining human-readable in debug tools.
    internal const string SessionKeyErrors = "__inertia:errors";
    internal const string SessionKeyFlash   = "__inertia:flash";

    // Shared options for session serialization: cycle-safe, no null bloat
    private static readonly JsonSerializerOptions SessionJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        // [1] Vary: X-Inertia must appear on every response (HTML and JSON alike)
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(InertiaHeaders.Vary))
                context.Response.Headers.Append(InertiaHeaders.Vary, InertiaHeaders.VaryValue);
            return Task.CompletedTask;
        });

        var inertiaRequest = InertiaResponseExecutor.GetOrParseRequest(context);

        var options = context.RequestServices.GetRequiredService<IOptions<InertiaOptions>>().Value;
        var handler = context.RequestServices.GetService<HandleInertiaRequestsBase>();
        var responseExecutor = context.RequestServices.GetRequiredService<InertiaResponseExecutor>();

        if (!inertiaRequest.IsInertiaRequest)
        {
            await next(context);
            // For non-Inertia (initial page load) requests, check whether the endpoint
            // stored an InertiaResult so we can render the HTML shell.
            if (context.Items.TryGetValue(typeof(InertiaResult), out var htmlResultObj)
                && htmlResultObj is InertiaResult htmlResult
                && !context.Response.HasStarted)
            {
                await responseExecutor.ExecuteAsync(context, htmlResult);
            }
            return;
        }

        // [4] Asset version check — safe-read methods (GET/HEAD) only; never block mutations.
        // Resolve and cache the version once per request to avoid calling expensive
        // resolvers (e.g. file-hash) twice (once here, once when building the page object).
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            var currentVersion = InertiaResponseExecutor.ResolveAndCacheVersion(context, handler, options);
            if (currentVersion is not null
                && inertiaRequest.Version is not null
                && !string.Equals(currentVersion, inertiaRequest.Version, StringComparison.Ordinal))
            {
                var self = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.Headers[InertiaHeaders.Location] = self;
                return;
            }
        }

        // [6] Prefetch: return 204 so the client knows we support it without running the full action
        if (inertiaRequest.IsPrefetch)
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        var originalMethod = context.Request.Method;

        await next(context);

        // ── Post-action: handle redirects ────────────────────────────────────

        if (IsRedirect(context.Response.StatusCode))
        {
            await HandleRedirectAsync(context, options, originalMethod);
            return;
        }

        // ── Post-action: render Inertia result ───────────────────────────────

        if (!context.Items.TryGetValue(typeof(InertiaResult), out var resultObj)
            || resultObj is not InertiaResult inertiaResult)
        {
            // If InertiaLocationResult was returned directly from a Minimal API or FastEndpoints
            // handler (via IResult.ExecuteAsync), it already set 409 + X-Inertia-Location.
            // Treat any pre-set X-Inertia-Location as a fully handled response.
            if (context.Response.Headers.ContainsKey(InertiaHeaders.Location)) return;

            // [O] Empty response — redirect back to a validated referer.
            // Use same-origin check to prevent open-redirect via a spoofed Referer header.
            // Only redirect if response hasn't already started (e.g., FastEndpoints auto-response).
            if (!context.Response.HasStarted)
            {
                var referer = context.Request.Headers.Referer.ToString();
                var target = Internal.InertiaService.IsSameOriginReferer(referer, context) ? referer : "/";
                context.Response.StatusCode = StatusCodes.Status303SeeOther;
                context.Response.Headers.Location = target;
            }
            return;
        }

        await responseExecutor.ExecuteAsync(context, inertiaResult);
    }

    // ── Redirect handling ─────────────────────────────────────────────────────

    private Task HandleRedirectAsync(HttpContext context, InertiaOptions options, string originalMethod)
    {
        var location = context.Response.Headers.Location.ToString();

        // Flash InertiaResult flash data to session before the redirect leaves the process,
        // respecting the configured MaxSessionPayloadBytes limit.
        if (context.Items.TryGetValue(typeof(InertiaResult), out var obj) && obj is InertiaResult pendingResult)
            WriteFlashToSession(context, pendingResult.Flash, options.MaxSessionPayloadBytes);

        // Fragment in Location → cannot be followed by XHR; send 409 + X-Inertia-Location.
        // Use URI parsing to distinguish real fragments from %23 in query strings.
        if (HasFragment(location))
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.Headers.Remove("Location");
            context.Response.Headers[InertiaHeaders.Location] = location;
            return Task.CompletedTask;
        }

        // Promote 302 → 303 for non-GET/HEAD origins so the browser re-requests as GET
        if (context.Response.StatusCode == StatusCodes.Status302Found
            && !HttpMethods.IsGet(originalMethod)
            && !HttpMethods.IsHead(originalMethod))
        {
            context.Response.StatusCode = StatusCodes.Status303SeeOther;
        }

        return Task.CompletedTask;
    }

    internal static void WriteFlashToSession(
        HttpContext context,
        IReadOnlyDictionary<string, object?> flash,
        int maxBytes = 64 * 1024)
    {
        if (flash.Count == 0) return;
        try
        {
            if (!HasSession(context)) return;
            // Use cycle-safe options so flash values with circular object graphs don't crash
            var json = JsonSerializer.Serialize(flash, SessionJsonOptions);
            if (ExceedsLimit(json, maxBytes)) return; // silently drop oversized payload
            context.Session.SetString(SessionKeyFlash, json);
        }
        catch { /* session unavailable */ }
    }

    internal static void WriteErrorsToSession(
        HttpContext context,
        Dictionary<string, string[]> errors,
        int maxBytes = 64 * 1024)
    {
        try
        {
            if (!HasSession(context)) return;
            var json = JsonSerializer.Serialize(errors, SessionJsonOptions);
            if (ExceedsLimit(json, maxBytes)) return; // silently drop oversized payload
            context.Session.SetString(SessionKeyErrors, json);
        }
        catch { /* session unavailable */ }
    }

    /// <summary>
    /// Fast byte-count check. Uses <c>string.Length * 4</c> as a cheap upper bound
    /// (UTF-8 is at most 4 bytes per char) and only calls the precise O(n)
    /// <see cref="Encoding.UTF8.GetByteCount(string)"/> when the estimate is within range.
    /// </summary>
    private static bool ExceedsLimit(string json, int maxBytes)
    {
        // Quick gate: if even the maximum possible byte count is within limit, skip exact check
        if (json.Length * 4 <= maxBytes) return false;
        return Encoding.UTF8.GetByteCount(json) > maxBytes;
    }

    private static bool HasSession(HttpContext context)
    {
        try { return context.Features.Get<ISessionFeature>() is not null && context.Session.IsAvailable; }
        catch { return false; }
    }

    // ── Request parsing ───────────────────────────────────────────────────────

    private static bool IsRedirect(int statusCode) =>
        statusCode is >= 300 and < 400;

    /// <summary>
    /// True only when the URL contains a real fragment (#section), not a URL-encoded %23.
    /// </summary>
    internal static bool HasFragment(string location)
    {
        if (string.IsNullOrEmpty(location)) return false;
        if (Uri.TryCreate(location, UriKind.RelativeOrAbsolute, out var uri))
        {
            // For absolute URIs Uri.Fragment is populated.
            if (uri.IsAbsoluteUri) return uri.Fragment.Length > 0;
            // For relative URIs check for literal '#' after stripping any %23 (encoded)
            // by looking at the raw string for an unencoded hash.
            var idx = location.IndexOf('#');
            return idx >= 0;
        }
        return location.Contains('#');
    }

    /// <summary>
    /// Returns true if the error bag name contains only safe identifier characters.
    /// Rejects prototype-pollution names and values with special characters.
    /// </summary>
    internal static bool IsValidErrorBagName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        // Bound the iteration — reject anything absurdly long before scanning characters
        if (name.Length > 128) return false;
        // Allow only alphanumeric, underscore, hyphen — reject __proto__, constructor, etc.
        foreach (var ch in name)
            if (!char.IsAsciiLetterOrDigit(ch) && ch != '_' && ch != '-')
                return false;
        return name is not ("__proto__" or "constructor" or "prototype");
    }
}
