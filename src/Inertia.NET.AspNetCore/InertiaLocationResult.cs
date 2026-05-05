using Inertia.NET.AspNetCore.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inertia.NET.AspNetCore;

/// <summary>
/// Forces a full browser navigation to a URL by returning 409 Conflict with
/// <c>X-Inertia-Location</c>. Use for external redirects (OAuth, payment gateways)
/// or any URL outside the Inertia app that the client XHR cannot follow.
/// </summary>
/// <remarks>
/// Implements both <see cref="IActionResult"/> (MVC controllers) and
/// <see cref="IResult"/> (Minimal API / FastEndpoints) so it can be returned
/// directly from any handler style.
/// </remarks>
public sealed class InertiaLocationResult : IActionResult, IResult
{
    public string Url { get; }

    /// <param name="url">
    /// The target URL. Must be a relative path (e.g. <c>/dashboard</c>) or an
    /// absolute URL whose scheme is http or https. Arbitrary string values are
    /// rejected to prevent open-redirect attacks.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is not a safe redirect target.</exception>
    public InertiaLocationResult(string url)
    {
        if (!IsSafeRedirectUrl(url))
            throw new ArgumentException(
                $"Unsafe redirect URL '{url}'. Use a relative path or an absolute http/https URL.", nameof(url));

        Url = url;
    }

    // IActionResult — MVC controller return value
    public Task ExecuteResultAsync(ActionContext context) =>
        WriteResponseAsync(context.HttpContext);

    // IResult — Minimal API / FastEndpoints return value
    public Task ExecuteAsync(HttpContext httpContext) =>
        WriteResponseAsync(httpContext);

    private Task WriteResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.Headers[InertiaHeaders.Location] = Url;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns true for relative URLs ("/path") and absolute http/https URLs.
    /// Rejects javascript:, data:, and protocol-relative URLs.
    /// </summary>
    internal static bool IsSafeRedirectUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        // Relative path — safe
        if (url.StartsWith('/') && !url.StartsWith("//")) return true;

        // Absolute http / https — safe
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.Scheme is "http" or "https";

        return false;
    }
}
