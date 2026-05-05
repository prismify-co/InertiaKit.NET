using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Inertia.NET.AspNetCore.Internal;
using Inertia.NET.Core;
using Inertia.NET.Core.Abstractions;
using Inertia.NET.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inertia.NET.AspNetCore;

public sealed class InertiaMiddleware(RequestDelegate next, ILogger<InertiaMiddleware> logger)
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

        var inertiaRequest = ParseRequest(context);
        context.Items[typeof(InertiaRequest)] = inertiaRequest;

        var options = context.RequestServices.GetRequiredService<IOptions<InertiaOptions>>().Value;
        var handler = context.RequestServices.GetService<HandleInertiaRequestsBase>();

        if (!inertiaRequest.IsInertiaRequest)
        {
            await next(context);
            // For non-Inertia (initial page load) requests, check whether the endpoint
            // stored an InertiaResult so we can render the HTML shell.
            if (context.Items.TryGetValue(typeof(InertiaResult), out var htmlResultObj)
                && htmlResultObj is InertiaResult htmlResult
                && !context.Response.HasStarted)
            {
                await ExecuteInertiaResult(context, htmlResult, inertiaRequest, options, handler);
            }
            return;
        }

        // [4] Asset version check — safe-read methods (GET/HEAD) only; never block mutations.
        // Resolve and cache the version once per request to avoid calling expensive
        // resolvers (e.g. file-hash) twice (once here, once when building the page object).
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            var currentVersion = ResolveAndCacheVersion(context, handler, options);
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

        await ExecuteInertiaResult(context, inertiaResult, inertiaRequest, options, handler);
    }

    // ── Version caching ───────────────────────────────────────────────────────

    private const string VersionCacheKey = "Inertia_ResolvedVersion";

    private static string? ResolveAndCacheVersion(
        HttpContext context, HandleInertiaRequestsBase? handler, InertiaOptions options)
    {
        if (context.Items.TryGetValue(VersionCacheKey, out var cached))
            return cached as string;

        var version = handler?.Version(context) ?? options.VersionResolver();
        context.Items[VersionCacheKey] = version;
        return version;
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

    // ── Inertia response execution ────────────────────────────────────────────

    private async Task ExecuteInertiaResult(
        HttpContext context,
        InertiaResult result,
        InertiaRequest inertiaRequest,
        InertiaOptions options,
        HandleInertiaRequestsBase? handler)
    {
        var services = context.RequestServices;
        var serializer = services.GetRequiredService<IInertiaSerializer>();

        // Shared props
        var shareBuilder = new InertiaShareBuilder();
        handler?.Share(shareBuilder, context);
        var sharedRaw = shareBuilder.Build();

        // Merge: shared → page (page wins)
        var mergedRaw = new Dictionary<string, object?>(sharedRaw, StringComparer.Ordinal);
        foreach (var (k, v) in result.Props)
            mergedRaw[k] = v;

        // Inject session flash props
        MergeSessionFlash(context, mergedRaw);

        // Inject session validation errors (respecting error bag scoping)
        MergeSessionErrors(context, inertiaRequest, mergedRaw, options);

        // PropResolver is registered as Scoped in AddInertia() — DI always provides it with
        // a proper ILogger. The fallback guards against misconfigured containers.
        var resolver = services.GetService<PropResolver>()
                       ?? new PropResolver(services, services.GetService<ILogger<PropResolver>>());

        // Component mismatch: if the client's X-Inertia-Partial-Component doesn't match
        // the component being rendered, ignore partial filters and return a full page object.
        var componentMatches = !inertiaRequest.IsPartialReload
            || string.Equals(inertiaRequest.PartialComponent, result.Component, StringComparison.Ordinal);

        var resolved = resolver.Resolve(
            mergedRaw,
            isPartialReload: inertiaRequest.IsPartialReload && componentMatches,
            partialOnly:  componentMatches ? inertiaRequest.PartialOnly  : null,
            partialExcept: componentMatches ? inertiaRequest.PartialExcept : null,
            inertiaRequest.ClientOnceProps,
            inertiaRequest.ResetProps);

        // Re-use the version resolved at the start of the request (avoids calling twice)
        var currentVersion = ResolveAndCacheVersion(context, handler, options) ?? string.Empty;
        var url = $"{context.Request.Path}{context.Request.QueryString}";

        var pageObject = PageObjectBuilder.Build(
            result.Component,
            resolved,
            url,
            currentVersion,
            result.EncryptHistory,
            result.ClearHistory,
            result.PreserveFragment,
            result.ScrollRegions,
            result.RememberedState);

        // Determine status code: 422 when errors present on a mutating request
        var statusCode = DetermineStatusCode(context, resolved);
        
        // Set status code only if response hasn't started yet
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
        }

        if (inertiaRequest.IsInertiaRequest)
        {
            // Set headers only if response hasn't started yet
            if (!context.Response.HasStarted)
            {
                context.Response.Headers[InertiaHeaders.Inertia] = "true";
                context.Response.ContentType = "application/json";
                
                var json = serializer.Serialize(pageObject);
                await context.Response.WriteAsync(json, Encoding.UTF8);
            }
        }
        else
        {
            // Set headers only if response hasn't started yet
            if (!context.Response.HasStarted)
            {
                // Explicitly set Vary here too in case the OnStarting callback was missed
                if (!context.Response.Headers.ContainsKey(InertiaHeaders.Vary))
                    context.Response.Headers.Append(InertiaHeaders.Vary, InertiaHeaders.VaryValue);

                var json = serializer.Serialize(pageObject);
                await RenderRootView(context, options, handler, json, services);
            }
        }
    }

    // ── Session helpers ───────────────────────────────────────────────────────

    private void MergeSessionFlash(HttpContext context, Dictionary<string, object?> mergedRaw)
    {
        try
        {
            if (!HasSession(context)) return;
            var raw = context.Session.GetString(SessionKeyFlash);
            if (raw is null) return;
            var flash = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(raw);
            if (flash is not null)
            {
                mergedRaw["flash"] = flash.ToDictionary(
                    kvp => kvp.Key, kvp => (object?)kvp.Value);
            }
            context.Session.Remove(SessionKeyFlash);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read Inertia flash data from session");
        }
    }

    private void MergeSessionErrors(
        HttpContext context,
        InertiaRequest inertiaRequest,
        Dictionary<string, object?> mergedRaw,
        InertiaOptions options)
    {
        try
        {
            if (!HasSession(context)) return;
            var raw = context.Session.GetString(SessionKeyErrors);
            if (raw is null) return;

            // Strongly typed: Dictionary<fieldName, string[]> prevents arbitrary injection
            var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(raw);
            if (errors is null) return;

            context.Session.Remove(SessionKeyErrors);

            // Apply ReturnAllErrors: collapse to first error per field if disabled
            object errorsValue = options.ReturnAllErrors
                ? errors
                : errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FirstOrDefault() ?? string.Empty);

            // Scope under error bag name only when the name passes the safety check.
            // Reject names like __proto__ or values with special characters.
            if (IsValidErrorBagName(inertiaRequest.ErrorBag))
                mergedRaw["errors"] = new Dictionary<string, object?> { [inertiaRequest.ErrorBag!] = errorsValue };
            else
                mergedRaw["errors"] = errorsValue;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read Inertia validation errors from session");
        }
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

    // ── Status code resolution ────────────────────────────────────────────────

    private static int DetermineStatusCode(HttpContext context, ResolvedProps resolved)
    {
        // Preserve any status code already set (e.g. by the action)
        if (context.Response.StatusCode is not (200 or 0))
            return context.Response.StatusCode;

        // 422 when validation errors present on a mutating request
        if (resolved.Resolved.ContainsKey("errors")
            && !HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method))
        {
            return StatusCodes.Status422UnprocessableEntity;
        }

        return StatusCodes.Status200OK;
    }

    // ── Root view rendering ───────────────────────────────────────────────────

    private static async Task RenderRootView(
        HttpContext context,
        InertiaOptions options,
        HandleInertiaRequestsBase? handler,
        string pageJson,
        IServiceProvider services)
    {
        context.Items["InertiaPage"] = pageJson;

        // SSR: call the Node.js gateway when configured and the route is not excluded
        string? ssrHtml = null;
        string[]? ssrHead = null;
        if (!string.IsNullOrWhiteSpace(options.SsrUrl) && !IsSsrExcluded(context, options))
        {
            var gateway = services.GetService<SsrGateway>();
            if (gateway is not null)
            {
                var ssrResult = await gateway.RenderAsync(pageJson, context.RequestAborted);
                ssrHtml = ssrResult?.Html;
                ssrHead = ssrResult?.Head;
            }
        }

        context.Items["InertiaPageSsrHtml"] = ssrHtml;
        context.Items["InertiaPageSsrHead"] = ssrHead;

        var mvcMarker = services.GetService<
            Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<
                Microsoft.AspNetCore.Mvc.ViewResult>>();

        if (mvcMarker is not null)
        {
            var viewName = handler?.RootView ?? options.RootView;
            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
                context,
                context.Features.Get<Microsoft.AspNetCore.Routing.IRoutingFeature>()?.RouteData
                    ?? new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            var viewResult = new Microsoft.AspNetCore.Mvc.ViewResult { ViewName = viewName };
            await viewResult.ExecuteResultAsync(actionContext);
            return;
        }

        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(BuildHtmlShell(pageJson, ssrHtml, ssrHead), Encoding.UTF8);
    }

    private static bool IsSsrExcluded(HttpContext context, InertiaOptions options)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        foreach (var prefix in options.SsrExcludedPrefixes)
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static string BuildHtmlShell(string pageJson, string? ssrHtml = null, string[]? ssrHead = null)
    {
        var headTags = ssrHead is { Length: > 0 }
            ? string.Join('\n', ssrHead)
            : string.Empty;

        var appContent = ssrHtml ?? string.Empty;

        return $"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8" />
        {headTags}
        </head>
        <body>
        <div id="app">{appContent}</div>
        <script type="application/json" id="app-data">{pageJson}</script>
        </body>
        </html>
        """;
    }

    // ── Request parsing ───────────────────────────────────────────────────────

    private static InertiaRequest ParseRequest(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (!headers.ContainsKey(InertiaHeaders.Inertia)) return InertiaRequest.NonInertia();

        return InertiaRequest.Parse(
            isInertia: true,
            version: headers[InertiaHeaders.Version].ToString() is { Length: > 0 } v ? v : null,
            partialComponent: headers[InertiaHeaders.PartialComponent].ToString() is { Length: > 0 } pc ? pc : null,
            partialData: headers[InertiaHeaders.PartialData].ToString() is { Length: > 0 } pd ? pd : null,
            partialExcept: headers[InertiaHeaders.PartialExcept].ToString() is { Length: > 0 } pe ? pe : null,
            errorBag: headers[InertiaHeaders.ErrorBag].ToString() is { Length: > 0 } eb ? eb : null,
            resetProps: headers[InertiaHeaders.Reset].ToString() is { Length: > 0 } rp ? rp : null,
            onceProps: headers[InertiaHeaders.OnceProps].ToString() is { Length: > 0 } op ? op : null,
            isPrefetch: string.Equals(
                headers[InertiaHeaders.Purpose].ToString(), "prefetch",
                StringComparison.OrdinalIgnoreCase));
    }

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
