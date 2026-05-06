using System.Text;
using System.Text.Json;
using InertiaKit.Core;
using InertiaKit.Core.Abstractions;
using InertiaKit.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InertiaKit.AspNetCore.Internal;

internal sealed class InertiaResponseExecutor(ILogger<InertiaResponseExecutor> logger)
{
    private const string VersionCacheKey = "Inertia_ResolvedVersion";

    public static InertiaRequest GetOrParseRequest(HttpContext context)
    {
        if (context.Items.TryGetValue(typeof(InertiaRequest), out var requestObj)
            && requestObj is InertiaRequest request)
        {
            return request;
        }

        var parsed = ParseRequest(context);
        context.Items[typeof(InertiaRequest)] = parsed;
        return parsed;
    }

    public static string? ResolveAndCacheVersion(
        HttpContext context,
        HandleInertiaRequestsBase? handler,
        InertiaOptions options)
    {
        if (context.Items.TryGetValue(VersionCacheKey, out var cached))
            return cached as string;

        var version = handler?.Version(context) ?? options.VersionResolver();
        context.Items[VersionCacheKey] = version;
        return version;
    }

    public async Task ExecuteAsync(HttpContext context, InertiaResult result)
    {
        var inertiaRequest = GetOrParseRequest(context);
        var services = context.RequestServices;
        var options = services.GetRequiredService<IOptions<InertiaOptions>>().Value;
        var handler = services.GetService<HandleInertiaRequestsBase>();
        var serializer = services.GetRequiredService<IInertiaSerializer>();

        var shareBuilder = new InertiaShareBuilder();
        handler?.Share(shareBuilder, context);
        var sharedRaw = shareBuilder.Build();

        var mergedRaw = new Dictionary<string, object?>(sharedRaw, StringComparer.Ordinal);
        foreach (var (key, value) in result.Props)
            mergedRaw[key] = value;

        MergeSessionFlash(context, mergedRaw);
        MergeSessionErrors(context, inertiaRequest, mergedRaw, options);

        var resolver = services.GetService<PropResolver>()
                       ?? new PropResolver(services, services.GetService<ILogger<PropResolver>>());

        var componentMatches = !inertiaRequest.IsPartialReload
            || string.Equals(inertiaRequest.PartialComponent, result.Component, StringComparison.Ordinal);

        var resolved = resolver.Resolve(
            mergedRaw,
            isPartialReload: inertiaRequest.IsPartialReload && componentMatches,
            partialOnly: componentMatches ? inertiaRequest.PartialOnly : null,
            partialExcept: componentMatches ? inertiaRequest.PartialExcept : null,
            inertiaRequest.ClientOnceProps,
            inertiaRequest.ResetProps);

        var currentVersion = ResolveAndCacheVersion(context, handler, options) ?? string.Empty;
        var url = $"{context.Request.Path}{context.Request.QueryString}";
        var encryptHistory = ResolveEncryptHistory(context, result, options);
        var clearHistory = ResolveClearHistory(context, result);

        var pageObject = PageObjectBuilder.Build(
            result.Component,
            resolved,
            url,
            currentVersion,
            encryptHistory,
            clearHistory,
            result.PreserveFragment,
            result.ScrollRegions,
            result.RememberedState);

        if (!context.Response.Headers.ContainsKey(InertiaHeaders.Vary))
            context.Response.Headers.Append(InertiaHeaders.Vary, InertiaHeaders.VaryValue);

        context.Response.StatusCode = DetermineStatusCode(context, resolved);

        if (inertiaRequest.IsInertiaRequest)
        {
            context.Response.Headers[InertiaHeaders.Inertia] = "true";
            context.Response.ContentType = "application/json";

            var json = serializer.Serialize(pageObject);
            await context.Response.WriteAsync(json, Encoding.UTF8);
            return;
        }

        var pageJson = serializer.Serialize(pageObject);
        await RenderRootView(context, options, handler, pageJson, services);
    }

    private void MergeSessionFlash(HttpContext context, Dictionary<string, object?> mergedRaw)
    {
        try
        {
            if (!HasSession(context)) return;

            var raw = context.Session.GetString(InertiaMiddleware.SessionKeyFlash);
            if (raw is null) return;

            var flash = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(raw);
            if (flash is not null)
            {
                mergedRaw["flash"] = flash.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object?)kvp.Value);
            }

            context.Session.Remove(InertiaMiddleware.SessionKeyFlash);
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

            var raw = context.Session.GetString(InertiaMiddleware.SessionKeyErrors);
            if (raw is null) return;

            var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(raw);
            if (errors is null) return;

            context.Session.Remove(InertiaMiddleware.SessionKeyErrors);

            object errorsValue = options.ReturnAllErrors
                ? errors
                : errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.FirstOrDefault() ?? string.Empty);

            if (InertiaMiddleware.IsValidErrorBagName(inertiaRequest.ErrorBag))
            {
                mergedRaw["errors"] = new Dictionary<string, object?>
                {
                    [inertiaRequest.ErrorBag!] = errorsValue,
                };
            }
            else
            {
                mergedRaw["errors"] = errorsValue;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read Inertia validation errors from session");
        }
    }

    private static int DetermineStatusCode(HttpContext context, ResolvedProps resolved)
    {
        if (context.Response.StatusCode is not (200 or 0))
            return context.Response.StatusCode;

        if (resolved.Resolved.ContainsKey("errors")
            && !HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method))
        {
            return StatusCodes.Status422UnprocessableEntity;
        }

        return StatusCodes.Status200OK;
    }

    private static async Task RenderRootView(
        HttpContext context,
        InertiaOptions options,
        HandleInertiaRequestsBase? handler,
        string pageJson,
        IServiceProvider services)
    {
        context.Items["InertiaPage"] = pageJson;

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

        var renderContext = new InertiaRenderContext
        {
            HttpContext = context,
            RootView = handler?.RootView ?? options.RootView,
            PageJson = pageJson,
            SsrHtml = ssrHtml,
            SsrHead = ssrHead,
        };

        foreach (var renderer in services.GetServices<IInertiaRenderer>()
                     .OrderByDescending(renderer => renderer.Priority))
        {
            if (!renderer.CanRender(renderContext))
                continue;

            await renderer.RenderAsync(renderContext);
            return;
        }

        throw new InvalidOperationException(
            "No IInertiaRenderer could render the initial HTML response. "
            + "Register a custom renderer or ensure the built-in renderers are available.");
    }

    private static bool? ResolveEncryptHistory(HttpContext context, InertiaResult result, InertiaOptions options)
    {
        if (result.EncryptHistory is not null)
            return result.EncryptHistory;

        var endpointSetting = context.GetEndpoint()?.Metadata
            .OfType<EncryptHistoryAttribute>()
            .LastOrDefault();
        if (endpointSetting is not null)
            return endpointSetting.Encrypt;

        return options.History.Encrypt ? true : null;
    }

    private static bool? ResolveClearHistory(HttpContext context, InertiaResult result)
    {
        if (result.ClearHistory is not null)
            return result.ClearHistory;

        var endpointSetting = context.GetEndpoint()?.Metadata
            .OfType<ClearHistoryAttribute>()
            .LastOrDefault();
        if (endpointSetting is not null)
            return endpointSetting.Clear;

        return null;
    }

    private static bool IsSsrExcluded(HttpContext context, InertiaOptions options)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        foreach (var prefix in options.SsrExcludedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static InertiaRequest ParseRequest(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (!headers.ContainsKey(InertiaHeaders.Inertia))
            return InertiaRequest.NonInertia();

        var exceptOnceProps = headers[InertiaHeaders.ExceptOnceProps].ToString() is { Length: > 0 } currentOnceProps
            ? currentOnceProps
            : headers[InertiaHeaders.LegacyOnceProps].ToString() is { Length: > 0 } legacyOnceProps
                ? legacyOnceProps
                : null;

        return InertiaRequest.Parse(
            isInertia: true,
            version: headers[InertiaHeaders.Version].ToString() is { Length: > 0 } version ? version : null,
            partialComponent: headers[InertiaHeaders.PartialComponent].ToString() is { Length: > 0 } partialComponent ? partialComponent : null,
            partialData: headers[InertiaHeaders.PartialData].ToString() is { Length: > 0 } partialData ? partialData : null,
            partialExcept: headers[InertiaHeaders.PartialExcept].ToString() is { Length: > 0 } partialExcept ? partialExcept : null,
            errorBag: headers[InertiaHeaders.ErrorBag].ToString() is { Length: > 0 } errorBag ? errorBag : null,
            resetProps: headers[InertiaHeaders.Reset].ToString() is { Length: > 0 } resetProps ? resetProps : null,
            onceProps: exceptOnceProps,
            isPrefetch: string.Equals(
                headers[InertiaHeaders.Purpose].ToString(),
                "prefetch",
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasSession(HttpContext context)
    {
        try
        {
            return context.Features.Get<ISessionFeature>() is not null && context.Session.IsAvailable;
        }
        catch
        {
            return false;
        }
    }
}
